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
                    // Wave cleared — award score
                    surv.Score += state.Config.SurvivalScorePerWave * surv.WaveNumber;

                    // No-damage bonus: check if player at full HP
                    if (state.Players[0].Health >= state.Players[0].MaxHealth)
                        surv.Score += state.Config.SurvivalScoreNoDamageBonus;

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

            // Re-initialize weapon tracking arrays to match new player count.
            // Preserves player[0]'s existing tracking data by re-using InitWeaponTracking
            // which creates fresh arrays — player[0]'s per-weapon stats are on PlayerState
            // (ShotsFired, DirectHits, TotalDamageDealt) which were preserved via struct copy.
            state.InitWeaponTracking(state.Players.Length);

            AILogic.Reset(state.Seed + wave, state.Players.Length);
            BossLogic.Reset(state.Seed + wave, state.Players.Length);

            surv.MobsSpawnedTotal += mobCount;
            surv.WaveActive = true;
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
