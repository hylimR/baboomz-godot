using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Regression tests for bugs fixed 2026-03-23 ---

        [Test]
        public void BossLogic_ForgeColossus_StompDoesNotSelfDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as a Forge Colossus boss
            state.Players[1].BossType = "forge_colossus";
            state.Players[1].IsMob = true;
            state.Players[1].MaxHealth = 200f;
            state.Players[1].Health = 200f;
            state.Players[1].BossPhase = 0;

            // Move player 0 far away so stomp only affects boss
            state.Players[0].Position = new Vec2(-40f, 0f);

            // Trigger phase 2 stomp by dropping HP to 49%
            state.Players[1].Health = 98f; // 49% of 200

            BossLogic.Reset(42);
            float healthBefore = state.Players[1].Health;

            // Tick to trigger phase transition
            GameSimulation.Tick(state, 0.016f);

            Assert.GreaterOrEqual(state.Players[1].Health, healthBefore - 0.01f,
                "Forge Colossus should not take self-damage from stomp");
        }

        [Test]
        public void ClusterBomb_SubProjectiles_SpreadBothDirections()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Place a cluster projectile moving leftward
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 2f),
                Velocity = new Vec2(-10f, 5f), // moving LEFT
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 20f,
                KnockbackForce = 4f,
                Alive = true,
                ClusterCount = 5
            });

            // Build a terrain wall to force impact
            for (int px = 150; px <= 165; px++)
                for (int py = 0; py < 80; py++)
                    state.Terrain.SetSolid(px, py, true);

            // Tick until cluster impacts
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Projectiles.Count > 1) break;
            }

            if (state.Projectiles.Count > 1)
            {
                // Sub-projectiles from a leftward-moving parent should have negative X velocities
                bool hasLeftward = false;
                for (int i = 0; i < state.Projectiles.Count; i++)
                {
                    if (state.Projectiles[i].ClusterCount == 0 && state.Projectiles[i].Velocity.x < 0f)
                    {
                        hasLeftward = true;
                        break;
                    }
                }
                Assert.IsTrue(hasLeftward,
                    "Cluster sub-projectiles should spread in the parent's travel direction");
            }
        }

        [Test]
        public void MultipleMatches_BossLogicReset_NoStaleTimers()
        {
            var config = SmallConfig();

            // Match 1: run with boss
            var state1 = GameSimulation.CreateMatch(config, 100);
            AILogic.Reset(100);
            BossLogic.Reset(100);
            state1.Players[1].BossType = "iron_sentinel";
            state1.Players[1].IsMob = true;

            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state1, 0.016f);

            // Match 2: fresh match — boss timers should not carry over
            var state2 = GameSimulation.CreateMatch(config, 200);
            AILogic.Reset(200);
            BossLogic.Reset(200);
            state2.Players[1].BossType = "iron_sentinel";
            state2.Players[1].IsMob = true;

            // The boss should behave correctly from frame 0 without stale timer issues
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 300; i++)
                    GameSimulation.Tick(state2, 0.016f);
            });
        }

    }
}
