using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Headhunter mode logic: token drops on death, token collection,
    /// player respawn with reduced HP, and first-to-N-tokens win condition.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitHeadhunter(GameState state)
        {
            int playerCount = state.Players.Length;
            state.Headhunter = new HeadhunterState
            {
                TokensCollected = new int[playerCount],
                RespawnTimers = new float[playerCount],
                Tokens = new TokenPickup[64], // max tokens on map
                TokenCount = 0
            };
        }

        static void UpdateHeadhunter(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.Headhunter) return;

            ref HeadhunterState hh = ref state.Headhunter;
            var config = state.Config;

            // Respawn dead players
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (!p.IsDead) continue;

                if (hh.RespawnTimers[i] <= 0f)
                    hh.RespawnTimers[i] = config.HeadhunterRespawnDelay;

                hh.RespawnTimers[i] -= dt;
                if (hh.RespawnTimers[i] <= 0f)
                    RespawnHeadhunter(state, i);
            }

            // Deactivate tokens submerged by rising water
            float waterY = MathF.Max(config.DeathBoundaryY, state.WaterLevel);
            for (int t = 0; t < hh.TokenCount; t++)
            {
                if (hh.Tokens[t].Active && hh.Tokens[t].Position.y < waterY)
                    hh.Tokens[t].Active = false;
            }

            // Token collection
            float collectR = config.HeadhunterTokenCollectRadius;
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.IsDead) continue;

                for (int t = 0; t < hh.TokenCount; t++)
                {
                    if (!hh.Tokens[t].Active) continue;

                    float dist = Vec2.Distance(p.Position, hh.Tokens[t].Position);
                    if (dist > collectR) continue;

                    hh.TokensCollected[i]++;
                    hh.Tokens[t].Active = false;

                    state.TokenCollectEvents.Add(new TokenCollectEvent
                    {
                        PlayerIndex = i,
                        Position = hh.Tokens[t].Position
                    });
                }
            }

            // Compact inactive tokens
            CompactTokens(ref hh);

            CheckHeadhunterEnd(state);
        }

        /// <summary>
        /// Spawn tokens at a player's death position. Called from death-handling code.
        /// </summary>
        public static void SpawnHeadhunterTokens(GameState state, int deadPlayerIndex)
        {
            if (state.Config.MatchType != MatchType.Headhunter) return;

            ref HeadhunterState hh = ref state.Headhunter;
            Vec2 deathPos = state.Players[deadPlayerIndex].Position;
            int count = state.Config.HeadhunterTokensOnDeath;
            var rng = new Random(state.Seed + (int)(state.Time * 1000) + deadPlayerIndex);

            for (int i = 0; i < count; i++)
            {
                if (hh.TokenCount >= hh.Tokens.Length) break;

                // Scatter tokens slightly around death position
                float offsetX = (float)(rng.NextDouble() * 2.0 - 1.0) * 1.5f;
                float offsetY = (float)(rng.NextDouble()) * 1f;

                hh.Tokens[hh.TokenCount] = new TokenPickup
                {
                    Position = new Vec2(deathPos.x + offsetX, deathPos.y + offsetY),
                    Active = true
                };
                hh.TokenCount++;
            }
        }

        static void RespawnHeadhunter(GameState state, int playerIndex)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            var config = state.Config;

            // Respawn at own spawn side
            float spawnX = playerIndex == 0 ? config.Player1SpawnX : config.Player2SpawnX;
            float spawnY = GamePhysics.FindGroundY(state.Terrain, spawnX, config.SpawnProbeY, 0.5f);

            p.IsDead = false;
            p.Health = config.DefaultMaxHealth * config.HeadhunterRespawnHealthFraction;
            p.Energy = config.DefaultMaxEnergy;
            p.Position = new Vec2(spawnX, spawnY + 0.5f);
            p.Velocity = Vec2.Zero;
            p.FreezeTimer = 0f;
            p.RetreatTimer = 0f;
            p.ShootCooldownRemaining = 0f;
            p.IsSwimming = false;
            p.SwimTimer = 0f;

            // Restore weapon ammo from config to prevent progressive weapon starvation
            RestoreWeaponAmmo(ref p, config);
            ResetSkillCooldowns(ref p);
        }

        static void CompactTokens(ref HeadhunterState hh)
        {
            int write = 0;
            for (int read = 0; read < hh.TokenCount; read++)
            {
                if (hh.Tokens[read].Active)
                {
                    if (write != read)
                        hh.Tokens[write] = hh.Tokens[read];
                    write++;
                }
            }
            hh.TokenCount = write;
        }

        static void CheckHeadhunterEnd(GameState state)
        {
            ref HeadhunterState hh = ref state.Headhunter;
            int target = state.Config.HeadhunterTokensToWin;

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (hh.TokensCollected[i] >= target)
                {
                    state.Phase = MatchPhase.Ended;
                    state.WinnerIndex = i;
                    return;
                }
            }
        }
    }
}
