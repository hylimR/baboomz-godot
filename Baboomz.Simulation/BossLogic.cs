using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Boss-specific AI behaviors. Each boss has a state machine driven by
    /// HP phase thresholds defined in the level JSON.
    ///
    /// Called from AILogic when MobType == "boss" or BossType is set.
    /// All logic is pure C# — no Unity dependency.
    ///
    /// Boss behaviors are split across partial class files:
    ///   BossLogic.cs            — router, state, helpers
    ///   BossIronSentinel.cs     — Iron Sentinel turret boss
    ///   BossSandWyrm.cs         — Sand Wyrm burrowing boss
    ///   BossGlacialCannon.cs    — Glacial Cannon ice boss
    ///   BossForgeColossus.cs    — Forge Colossus mech boss
    ///   BossBaronCogsworth.cs   — Baron Cogsworth final boss
    /// </summary>
    public static partial class BossLogic
    {
        // Per-boss timers (indexed by player index, dynamically sized)
        internal static float[] attackTimer = new float[16];
        internal static float[] specialTimer = new float[16];
        internal static float[] stateTimer = new float[16];
        public static int[] subState = new int[16]; // boss-specific sub-state
        internal static Random rng = new Random(42);

        public static void Reset(int seed, int playerCount = 16)
        {
            rng = new Random(seed);
            int size = Math.Max(playerCount, 16);
            attackTimer = new float[size];
            specialTimer = new float[size];
            stateTimer = new float[size];
            subState = new int[size];
        }

        /// <summary>
        /// Route to the correct boss behavior based on BossType.
        /// </summary>
        public static void Update(GameState state, int index, float dt)
        {
            ref PlayerState boss = ref state.Players[index];
            if (boss.IsDead) return;

            switch (boss.BossType)
            {
                case "iron_sentinel":
                    UpdateIronSentinel(state, index, dt);
                    break;
                case "sand_wyrm":
                    UpdateSandWyrm(state, index, dt);
                    break;
                case "glacial_cannon":
                    UpdateGlacialCannon(state, index, dt);
                    break;
                case "forge_colossus":
                    UpdateForgeColossus(state, index, dt);
                    break;
                case "baron_cogsworth":
                    UpdateBaronCogsworth(state, index, dt);
                    break;
            }
        }

        // ── Shared helpers ───────────────────────────────────────────────────

        internal static int FindTarget(GameState state, int selfIndex)
        {
            int selfTeam = state.Players[selfIndex].TeamIndex;
            // Prefer human player
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == selfIndex || state.Players[i].IsDead) continue;
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[i].TeamIndex == selfTeam) continue;
                if (!state.Players[i].IsAI) return i;
            }
            // Fallback: any alive
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == selfIndex || state.Players[i].IsDead) continue;
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[i].TeamIndex == selfTeam) continue;
                return i;
            }
            return -1;
        }

        internal static void SpawnFrostZones(GameState state, Vec2 nearPos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float offsetX = (float)(rng.NextDouble() * 20.0 - 10.0);
                float offsetY = (float)(rng.NextDouble() * 4.0 - 2.0);
                state.Mines.Add(new MineState
                {
                    Position = new Vec2(nearPos.x + offsetX, nearPos.y + offsetY),
                    TriggerRadius = 2f,
                    ExplosionRadius = 2.5f,
                    Damage = 20f,
                    Active = true,
                    Lifetime = 15f
                });
            }
        }
    }
}
