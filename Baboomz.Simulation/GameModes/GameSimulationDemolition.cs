using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Demolition mode logic: crystal placement, explosion damage to crystals,
    /// player respawn with limited lives, and win condition checks.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitDemolition(GameState state, Random rng)
        {
            var config = state.Config;
            float p1Crystal = config.Player1SpawnX - config.DemolitionCrystalOffset;
            float p2Crystal = config.Player2SpawnX + config.DemolitionCrystalOffset;

            float y1 = GamePhysics.FindGroundY(state.Terrain, p1Crystal, config.SpawnProbeY, 0.5f);
            float y2 = GamePhysics.FindGroundY(state.Terrain, p2Crystal, config.SpawnProbeY, 0.5f);

            state.Demolition = new DemolitionState
            {
                Crystals = new[]
                {
                    new CrystalState
                    {
                        HP = config.DemolitionCrystalHP,
                        MaxHP = config.DemolitionCrystalHP,
                        Position = new Vec2(p1Crystal, y1 + config.DemolitionCrystalHeight * 0.5f),
                        TeamIndex = 0
                    },
                    new CrystalState
                    {
                        HP = config.DemolitionCrystalHP,
                        MaxHP = config.DemolitionCrystalHP,
                        Position = new Vec2(p2Crystal, y2 + config.DemolitionCrystalHeight * 0.5f),
                        TeamIndex = 1
                    }
                },
                LivesRemaining = new int[state.Players.Length],
                RespawnTimers = new float[state.Players.Length],
                RespawnDelay = config.DemolitionRespawnDelay
            };

            for (int i = 0; i < state.Players.Length; i++)
                state.Demolition.LivesRemaining[i] = config.DemolitionLivesPerPlayer;
        }

        static void UpdateDemolition(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.Demolition) return;

            ref DemolitionState demo = ref state.Demolition;

            // Respawn dead players with remaining lives
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (!p.IsDead) continue;
                if (demo.LivesRemaining[i] <= 0) continue;

                if (demo.RespawnTimers[i] <= 0f)
                {
                    // Start respawn countdown on death
                    demo.RespawnTimers[i] = demo.RespawnDelay;
                }

                demo.RespawnTimers[i] -= dt;
                if (demo.RespawnTimers[i] <= 0f)
                {
                    demo.LivesRemaining[i]--;
                    RespawnPlayer(state, i);
                }
            }

            // Check crystal destruction win condition
            CheckDemolitionEnd(state);
        }

        static void RespawnPlayer(GameState state, int playerIndex)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            var config = state.Config;

            // Respawn at own crystal position (use TeamIndex, not playerIndex, for 2v2 correctness)
            int crystalIdx = p.TeamIndex == 0 ? 0 : 1;
            Vec2 crystalPos = state.Demolition.Crystals[crystalIdx].Position;
            float spawnY = GamePhysics.FindGroundY(state.Terrain, crystalPos.x, config.SpawnProbeY, 0.5f);

            p.IsDead = false;
            p.Health = config.DefaultMaxHealth;
            p.Energy = config.DefaultMaxEnergy;
            p.Position = new Vec2(crystalPos.x, spawnY + 0.5f);
            p.Velocity = Vec2.Zero;
            p.FreezeTimer = 0f;
            p.RetreatTimer = 0f;
            p.ShootCooldownRemaining = 0f;

            // Restore weapon ammo from config to prevent progressive weapon starvation
            RestoreWeaponAmmo(ref p, config);
        }

        /// <summary>
        /// Apply explosion damage to crystals. Called from CombatResolver.
        /// Crystals take damage from explosions but NOT from fire zones.
        /// </summary>
        public static void ApplyCrystalExplosionDamage(GameState state, Vec2 pos, float radius, float maxDamage)
        {
            if (state.Config.MatchType != MatchType.Demolition) return;

            var config = state.Config;
            float halfW = config.DemolitionCrystalWidth * 0.5f;
            float halfH = config.DemolitionCrystalHeight * 0.5f;

            for (int c = 0; c < state.Demolition.Crystals.Length; c++)
            {
                ref CrystalState crystal = ref state.Demolition.Crystals[c];
                if (crystal.HP <= 0f) continue;

                // AABB-circle overlap: find closest point on crystal rect to explosion center
                float closestX = Math.Clamp(pos.x, crystal.Position.x - halfW, crystal.Position.x + halfW);
                float closestY = Math.Clamp(pos.y, crystal.Position.y - halfH, crystal.Position.y + halfH);
                float dist = Vec2.Distance(pos, new Vec2(closestX, closestY));

                if (dist > radius) continue;

                float dmgRatio = radius > 0f ? 1f - Math.Clamp(dist / radius, 0f, 1f) : 1f;
                float damage = maxDamage * dmgRatio;

                crystal.HP -= damage;
                if (crystal.HP < 0f) crystal.HP = 0f;

                state.CrystalDamageEvents.Add(new CrystalDamageEvent
                {
                    CrystalIndex = c,
                    Amount = damage,
                    Position = crystal.Position
                });
            }
        }

        static void CheckDemolitionEnd(GameState state)
        {
            ref DemolitionState demo = ref state.Demolition;

            // Crystal destroyed = opponent wins
            for (int c = 0; c < demo.Crystals.Length; c++)
            {
                if (demo.Crystals[c].HP <= 0f)
                {
                    // Crystal c belongs to team c; the other team wins
                    int winner = c == 0 ? 1 : 0;
                    state.Phase = MatchPhase.Ended;
                    state.WinnerIndex = winner;
                    return;
                }
            }

            // All lives exhausted for both sides: crystal HP tiebreaker
            bool allExhausted = true;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (!state.Players[i].IsDead || demo.LivesRemaining[i] > 0)
                {
                    allExhausted = false;
                    break;
                }
            }

            if (allExhausted)
            {
                float hp0 = demo.Crystals[0].HP;
                float hp1 = demo.Crystals[1].HP;

                state.Phase = MatchPhase.Ended;
                if (hp0 > hp1)
                    state.WinnerIndex = 0;
                else if (hp1 > hp0)
                    state.WinnerIndex = 1;
                else
                    state.WinnerIndex = -1; // draw
            }
        }
    }
}
