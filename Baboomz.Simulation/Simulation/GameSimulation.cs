using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Core game loop: creates matches, ticks simulation, processes input.
    /// Delegates combat to CombatResolver, projectiles to ProjectileSimulation.
    /// Player logic (input, movement, creation) in GameSimulationPlayer.cs.
    /// </summary>
    public static partial class GameSimulation
    {
        public static GameState CreateMatch(GameConfig config, int seed,
            int playerSkill0 = -1, int playerSkill1 = -1)
        {
            var state = new GameState
            {
                Phase = MatchPhase.Waiting,
                Config = config,
                WinnerIndex = -1,
                NextProjectileId = 1
            };

            state.Biome = TerrainBiome.GetRandom(seed);
            config.TerrainMinHeight = state.Biome.MinHeight;
            config.TerrainMaxHeight = state.Biome.MaxHeight;
            config.TerrainHillFrequency = state.Biome.HillFrequency;
            BiomeModifiers.Apply(config, state.Biome);

            state.Terrain = TerrainGenerator.Generate(config, seed, state.Biome);
            TerrainFeatures.StampFeatures(state.Terrain, config, seed);

            float p1Y = GamePhysics.FindGroundY(state.Terrain, config.Player1SpawnX, config.SpawnProbeY);

            if (config.MatchType == MatchType.Survival || config.MatchType == MatchType.Campaign)
            {
                // Survival / Campaign: single player, mobs spawned externally
                state.Players = new[]
                {
                    CreatePlayer(config, config.Player1SpawnX, p1Y, config.Player1Name, isAI: false,
                        playerSkill0, playerSkill1)
                };
            }
            else
            {
                float p2Y = GamePhysics.FindGroundY(state.Terrain, config.Player2SpawnX, config.SpawnProbeY);
                var aiLoadout = AILogic.PickLoadout(config, seed);
                // Set AI weapon loadout if not already configured externally
                if (config.AIWeaponLoadout == null)
                    config.AIWeaponLoadout = AILogic.PickWeaponLoadout(config, seed);
                state.Players = new[]
                {
                    CreatePlayer(config, config.Player1SpawnX, p1Y, config.Player1Name, isAI: false,
                        playerSkill0, playerSkill1),
                    CreatePlayer(config, config.Player2SpawnX, p2Y, config.Player2Name, isAI: true,
                        aiLoadout[0], aiLoadout[1])
                };
            }

            state.Seed = seed;

            // Reset AI/Boss RNG after Players array is created so arrays are correctly sized
            AILogic.Reset(seed, state.Players.Length);
            BossLogic.Reset(seed, state.Players.Length);

            var rng = new Random(seed);

            // Assign random cosmetic hats
            int hatCount = 5; // HatType values 1-5 (skip None)
            for (int i = 0; i < state.Players.Length; i++)
                state.Players[i].Hat = (HatType)(1 + rng.Next(hatCount));

            UpdateWind(state, rng);
            state.NextWindChangeTime = config.WindChangeInterval;

            state.WaterLevel = config.DeathBoundaryY;
            state.SuddenDeathActive = false;

            SpawnMines(state, rng);
            SpawnBarrels(state, rng);
            SpawnBiomeHazards(state, rng);

            // Randomize AI starting weapon (not applicable for survival — only 1 player)
            if (state.Players.Length > 1)
            {
                var validSlots = new System.Collections.Generic.List<int>();
                for (int i = 0; i < state.Players[1].WeaponSlots.Length; i++)
                {
                    if (state.Players[1].WeaponSlots[i].WeaponId != null
                        && !state.Players[1].WeaponSlots[i].IsAirstrike)
                        validSlots.Add(i);
                }
                if (validSlots.Count > 1)
                    state.Players[1].ActiveWeaponSlot = validSlots[rng.Next(validSlots.Count)];
            }

            // Initialize KOTH if applicable
            if (config.MatchType == MatchType.KingOfTheHill)
                InitKoth(state, rng);

            // Initialize Survival if applicable
            if (config.MatchType == MatchType.Survival)
                InitSurvival(state);

            // Initialize Demolition if applicable
            if (config.MatchType == MatchType.Demolition)
                InitDemolition(state, rng);

            // Initialize Target Practice if applicable
            if (config.MatchType == MatchType.TargetPractice)
            {
                // Solo mode — disable AI opponent
                state.Players[1].IsDead = true;
                state.Players[1].Health = 0f;
                TargetPractice.Init(state, rng);
            }

            // Initialize Arms Race if applicable
            if (config.MatchType == MatchType.ArmsRace)
                InitArmsRace(state);

            // Initialize Roulette if applicable
            if (config.MatchType == MatchType.Roulette)
                InitRoulette(state);

            // Initialize Payload if applicable
            if (config.MatchType == MatchType.Payload)
                InitPayload(state);

            // Initialize Capture the Flag if applicable
            if (config.MatchType == MatchType.CaptureTheFlag)
                InitCtf(state, rng);

            // Initialize Headhunter if applicable
            if (config.MatchType == MatchType.Headhunter)
                InitHeadhunter(state);

            // Initialize Territories if applicable
            if (config.MatchType == MatchType.Territories)
                InitTerritories(state, rng);

            state.InitWeaponTracking(state.Players.Length);

            state.Phase = MatchPhase.Playing;
            return state;
        }

        public static void Tick(GameState state, float dt)
        {
            state.ExplosionEvents.Clear();
            state.DamageEvents.Clear();
            state.SplashEvents.Clear();
            state.SkillEvents.Clear();
            state.EmoteEvents.Clear();
            state.HitscanEvents.Clear();
            state.EnergyDrainEvents.Clear();
            state.AchievementEvents.Clear();
            state.ComboEvents.Clear();
            state.CrystalDamageEvents.Clear();
            state.FlagEvents.Clear();
            state.TokenCollectEvents.Clear();
            state.BarrelDetonationsThisTick = 0;

            if (state.Phase != MatchPhase.Playing) return;

            // Record frame for replay (before any processing)
            ReplaySystem.RecordFrame(state, dt);

            state.Time += dt;

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (!state.Players[i].IsAI && !state.Players[i].IsDead)
                    ProcessInput(state, i, dt);
            }
            AILogic.Update(state, dt);
            SkillSystem.Update(state, dt);
            TutorialSystem.Update(state, dt);

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (!state.Players[i].IsDead)
                    UpdatePlayer(state, i, dt);
            }

            ProjectileSimulation.Update(state, dt);
            CheckMines(state, dt);
            CheckBarrels(state);
            UpdateCrates(state, dt);
            UpdateFireZones(state, dt);
            CheckBiomeHazards(state, dt);

            if (state.Time >= state.NextWindChangeTime)
            {
                UpdateWind(state, new Random(state.Seed + (int)(state.Time * 100)));
                state.NextWindChangeTime = state.Time + state.Config.WindChangeInterval;
            }

            UpdateArmsRace(state, dt);
            UpdateKoth(state, dt);
            UpdateSurvival(state, dt);
            UpdateDemolition(state, dt);
            UpdatePayload(state, dt);
            UpdateCtf(state, dt);
            UpdateHeadhunter(state, dt);
            UpdateTerritories(state, dt);
            TargetPractice.Update(state, dt);
            TargetPractice.ResetStreakOnMiss(state);
            UpdateSuddenDeath(state, dt);
            CheckDeathBoundary(state, dt);
            CheckMatchEnd(state);
            AchievementTracker.Update(state);
            CombatResolver.DecayCombo(state);
        }

        // Fire() in GameSimulationFiring.cs (partial class)
        // FireHitscan in GameSimulationHitscan.cs (partial class)
        // Environment logic (mines, barrels, sudden death, wind, match end) in GameSimulationEnvironment.cs
    }
}
