using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Survival mode: endless waves of mobs with escalating difficulty.
    /// Player survives as long as possible, earning a score.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitSurvival(GameState state)
        {
            state.Survival = new SurvivalState
            {
                WaveNumber = 0,
                Score = 0,
                BreakTimer = state.Config.SurvivalBreakDuration,
                WaveActive = false,
                MobsAliveCount = 0,
                MobsSpawnedTotal = 0
            };
        }

        public static void UpdateSurvival(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.Survival) return;

            ref var surv = ref state.Survival;

            // Check if player is dead → end match
            if (state.Players[0].IsDead)
            {
                state.Phase = MatchPhase.Ended;
                state.WinnerIndex = -1;
                return;
            }

            if (surv.WaveActive)
            {
                // Count alive mobs
                int alive = 0;
                for (int i = 1; i < state.Players.Length; i++)
                {
                    if (!state.Players[i].IsDead) alive++;
                }
                surv.MobsAliveCount = alive;

                if (alive == 0)
                {
                    // Wave cleared — award score (1.5x for modifier waves)
                    int waveScore = state.Config.SurvivalScorePerWave * surv.WaveNumber;
                    if (surv.ActiveModifier != SurvivalModifier.None)
                        waveScore = (int)(waveScore * 1.5f);
                    surv.Score += waveScore;

                    // No-damage bonus: check if player at full HP
                    if (state.Players[0].Health >= state.Players[0].MaxHealth)
                        surv.Score += state.Config.SurvivalScoreNoDamageBonus;

                    RevertModifier(state);

                    surv.WaveActive = false;
                    surv.BreakTimer = state.Config.SurvivalBreakDuration;

                    // Between-wave recovery
                    ref PlayerState p = ref state.Players[0];
                    p.Health = MathF.Min(p.MaxHealth, p.Health + state.Config.SurvivalHealthRegen);
                    p.Energy = p.MaxEnergy;
                }
            }
            else
            {
                // Break between waves
                surv.BreakTimer -= dt;
                if (surv.BreakTimer <= 0f)
                {
                    surv.WaveNumber++;
                    SpawnSurvivalWave(state);
                }
            }
        }

        public static void SpawnSurvivalWave(GameState state)
        {
            var config = state.Config;
            ref var surv = ref state.Survival;
            int wave = surv.WaveNumber;
            var rng = new Random(state.Seed + wave * 7);

            bool isBossWave = config.SurvivalBossInterval > 0
                              && wave % config.SurvivalBossInterval == 0;

            int mobCount = isBossWave ? 1 : GetSurvivalMobCount(wave, config);
            float speedMult = GetSurvivalSpeedMult(wave);
            float hpMult = GetSurvivalHPMult(wave);

            // Build new Players array: player[0] + new mobs
            var newPlayers = new PlayerState[1 + mobCount];
            newPlayers[0] = state.Players[0];

            float halfMap = config.MapWidth / 2f;

            for (int i = 0; i < mobCount; i++)
            {
                float x = PickSpawnX(rng, halfMap, state.Players[0].Position.x);
                float y = GamePhysics.FindGroundY(state.Terrain, x, config.SpawnProbeY);

                if (isBossWave)
                    newPlayers[1 + i] = CreateSurvivalBoss(config, x, y, wave, hpMult, rng);
                else
                    newPlayers[1 + i] = CreateSurvivalMob(config, x, y, wave, speedMult, hpMult, rng);
            }

            state.Players = newPlayers;

            // Save player 0's weapon tracking before reinit (InitWeaponTracking replaces all arrays)
            var savedHits = state.WeaponHits?[0];
            var savedKills = state.WeaponKills?[0];
            var savedDamage = state.WeaponDamage?[0];
            var savedUsed = state.WeaponsUsed?[0];
            var savedSkills = state.SkillsActivated?[0];

            state.InitWeaponTracking(state.Players.Length);

            if (savedHits != null) state.WeaponHits[0] = savedHits;
            if (savedKills != null) state.WeaponKills[0] = savedKills;
            if (savedDamage != null) state.WeaponDamage[0] = savedDamage;
            if (savedUsed != null) state.WeaponsUsed[0] = savedUsed;
            if (savedSkills != null) state.SkillsActivated[0] = savedSkills;

            AILogic.Reset(state.Seed + wave, state.Players.Length);
            BossLogic.Reset(state.Seed + wave, state.Players.Length);

            surv.MobsSpawnedTotal += mobCount;
            surv.WaveActive = true;

            RollAndApplyModifier(state, rng);
        }

        static float PickSpawnX(Random rng, float halfMap, float playerX)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float x = (float)(rng.NextDouble() * halfMap * 2f - halfMap);
                if (MathF.Abs(x - playerX) >= 15f) return x;
            }
            // Fallback: opposite side of map
            return playerX > 0f ? -halfMap + 5f : halfMap - 5f;
        }

        public static int GetSurvivalMobCount(int wave, GameConfig config)
        {
            int baseMobs = config.SurvivalWaveMobBase;
            // Gradually increase: +1 mob every 4 waves, capped at baseMobs+5
            int extra = wave / 4;
            return Math.Min(baseMobs + extra, baseMobs + 5);
        }

        public static float GetSurvivalSpeedMult(int wave)
        {
            if (wave <= 4) return 1.0f;
            if (wave <= 9) return 1.1f;
            if (wave <= 14) return 1.2f;
            if (wave <= 19) return 1.3f;
            if (wave <= 24) return 1.5f;
            // Beyond wave 25: +0.2 per 5-wave loop
            int loopsBeyond = (wave - 25) / 5;
            return 1.5f + loopsBeyond * 0.2f;
        }

        public static float GetSurvivalHPMult(int wave)
        {
            if (wave <= 4) return 1.0f;
            if (wave <= 9) return 1.2f;
            if (wave <= 14) return 1.4f;
            if (wave <= 19) return 1.6f;
            if (wave <= 24) return 2.0f;
            // Beyond wave 25: +0.5 per 5-wave loop
            int loopsBeyond = (wave - 25) / 5;
            return 2.0f + loopsBeyond * 0.5f;
        }

        static void RollAndApplyModifier(GameState state, Random rng)
        {
            ref var surv = ref state.Survival;
            surv.ActiveModifier = SurvivalModifier.None;

            if (surv.WaveNumber < 5) return;
            if (rng.NextDouble() >= 0.3) return;

            var options = (SurvivalModifier[])Enum.GetValues(typeof(SurvivalModifier));
            // Skip None (index 0)
            surv.ActiveModifier = options[rng.Next(1, options.Length)];

            surv.SavedGravity = state.Config.Gravity;
            surv.SavedWindForce = state.WindForce;
            surv.SavedWindAngle = state.WindAngle;

            switch (surv.ActiveModifier)
            {
                case SurvivalModifier.LowGravity:
                    state.Config.Gravity *= 0.5f;
                    break;
                case SurvivalModifier.HeavyWind:
                    state.WindForce = state.Config.MaxWindStrength * 2f;
                    state.WindAngle = rng.Next(2) == 0 ? 0f : 180f;
                    break;
                case SurvivalModifier.GlassCannon:
                    surv.SavedDamageMultiplier = state.Players[0].DamageMultiplier;
                    surv.SavedArmorMultiplier = state.Players[0].ArmorMultiplier;
                    state.Players[0].DamageMultiplier *= 2f;
                    state.Players[0].ArmorMultiplier *= 0.5f;
                    break;
                case SurvivalModifier.ArmoredHorde:
                    for (int i = 1; i < state.Players.Length; i++)
                        state.Players[i].ArmorMultiplier *= 2f;
                    break;
                case SurvivalModifier.SpeedBlitz:
                    for (int i = 1; i < state.Players.Length; i++)
                        state.Players[i].MoveSpeed *= 1.8f;
                    break;
                case SurvivalModifier.RegenWave:
                    for (int i = 1; i < state.Players.Length; i++)
                        state.Players[i].HealthRegen = 3f;
                    break;
            }
        }

        static void RevertModifier(GameState state)
        {
            ref var surv = ref state.Survival;
            if (surv.ActiveModifier == SurvivalModifier.None) return;

            switch (surv.ActiveModifier)
            {
                case SurvivalModifier.LowGravity:
                    state.Config.Gravity = surv.SavedGravity;
                    break;
                case SurvivalModifier.HeavyWind:
                    state.WindForce = surv.SavedWindForce;
                    state.WindAngle = surv.SavedWindAngle;
                    break;
                case SurvivalModifier.GlassCannon:
                    state.Players[0].DamageMultiplier = surv.SavedDamageMultiplier;
                    state.Players[0].ArmorMultiplier = surv.SavedArmorMultiplier;
                    break;
                // ArmoredHorde, SpeedBlitz, RegenWave: mobs die, no revert needed
            }

            surv.ActiveModifier = SurvivalModifier.None;
        }

        public static void ScoreSurvivalKill(GameState state, int killedIndex)
        {
            if (state.Config.MatchType != MatchType.Survival) return;
            if (killedIndex < 0 || killedIndex >= state.Players.Length) return;
            if (!state.Players[killedIndex].IsMob) return;

            bool isBoss = !string.IsNullOrEmpty(state.Players[killedIndex].BossType);
            state.Survival.Score += isBoss
                ? state.Config.SurvivalScorePerBossKill
                : state.Config.SurvivalScorePerKill;
        }
    }
}
