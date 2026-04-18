using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    // --- Wall-collision regression tests (#373) ---
    [TestFixture]
    public class WallCollisionTests
    {
        static GameConfig FlatConfig()
        {
            return new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = -1f,
                TerrainHillFrequency = 0.05f,
                TerrainFloorDepth = -10f,
                Player1SpawnX = -10f,
                Player2SpawnX = 10f,
                SpawnProbeY = 20f,
                DeathBoundaryY = -25f,
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f
            };
        }

        [Test]
        public void Player_BlockedByWall_AtFootLevel()
        {
            // Regression (#373): foot-level wall was not blocked (only chest sampled).
            var state = GameSimulation.CreateMatch(FlatConfig(), 1);
            ref PlayerState p = ref state.Players[0];
            float startX = p.Position.x;
            float startY = p.Position.y;

            var t = state.Terrain;
            int wallPx = t.WorldToPixelX(startX + 1.5f);
            int footPy = t.WorldToPixelY(startY + 0.1f);
            for (int dy = 0; dy <= 2; dy++)
            {
                int row = footPy - dy;
                if (row >= 0 && row < t.Height)
                    t.SetSolid(wallPx, row, true);
            }

            p.Velocity = new Vec2(5f, 0f);
            p.IsGrounded = true;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(p.Position.x, startX + 2.0f,
                "Player should be blocked by foot-level solid wall");
        }

        [Test]
        public void Player_BlockedByWall_AtHeadLevel()
        {
            // Regression (#373): head-level wall was not blocked (only chest sampled).
            var state = GameSimulation.CreateMatch(FlatConfig(), 2);
            ref PlayerState p = ref state.Players[0];
            float startX = p.Position.x;
            float startY = p.Position.y;

            var t = state.Terrain;
            int wallPx = t.WorldToPixelX(startX + 1.5f);
            int headPy = t.WorldToPixelY(startY + 1.4f);
            for (int dy = 0; dy <= 2; dy++)
            {
                int row = headPy + dy;
                if (row < t.Height)
                    t.SetSolid(wallPx, row, true);
            }

            p.Velocity = new Vec2(5f, 0f);
            p.IsGrounded = true;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(p.Position.x, startX + 2.0f,
                "Player should be blocked by head-level solid wall");
        }
    }
}
