using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class GamePhysicsTests
    {
        [Test]
        public void ApplyGravity_DecreasesVelocityY()
        {
            Vec2 v = new Vec2(5f, 10f);
            GamePhysics.ApplyGravity(ref v, 1f, 9.81f);
            Assert.AreEqual(10f - 9.81f, v.y, 0.01f);
            Assert.AreEqual(5f, v.x, 0.01f); // X unchanged
        }

        [Test]
        public void ApplyWind_AffectsVelocityX()
        {
            Vec2 v = new Vec2(0f, 0f);
            GamePhysics.ApplyWind(ref v, 2f, 1f);
            Assert.AreEqual(2f, v.x, 0.01f);
        }

        [Test]
        public void IsGrounded_OnSolidTerrain_ReturnsTrue()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill a strip at pixel y=0..10
            for (int px = 0; px < 100; px++)
                for (int py = 0; py <= 10; py++)
                    t.SetSolid(px, py, true);

            float worldX = t.PixelToWorldX(50);

            // Position well above surface — not grounded
            float highY = t.PixelToWorldY(15);
            Assert.IsFalse(GamePhysics.IsGrounded(t, new Vec2(worldX, highY)),
                "Should not be grounded well above surface");

            // Position at surface level (feet touching solid)
            float surfaceY = t.PixelToWorldY(11) + 0.05f;
            Assert.IsTrue(GamePhysics.IsGrounded(t, new Vec2(worldX, surfaceY)),
                "Should be grounded when feet touch surface");
        }

        [Test]
        public void FindGroundY_FindsSurface()
        {
            var t = new TerrainState(160, 80, 16f, -5f, -2f);
            // Fill solid from y=0 to y=39 (world y = -2 to about +0.4)
            for (int px = 0; px < 160; px++)
                for (int py = 0; py < 40; py++)
                    t.SetSolid(px, py, true);

            float groundY = GamePhysics.FindGroundY(t, 0f, 10f);
            float expectedSurface = t.PixelToWorldY(41); // just above pixel 40
            Assert.AreEqual(expectedSurface, groundY, 0.5f);
        }

        [Test]
        public void RaycastTerrain_HitsSolidPixel()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill a horizontal strip
            for (int px = 0; px < 100; px++)
                t.SetSolid(px, 20, true);

            Vec2 from = new Vec2(0f, 5f); // above
            Vec2 to = new Vec2(0f, 0f);   // below

            bool hit = GamePhysics.RaycastTerrain(t, from, to, out Vec2 hitPoint);
            Assert.IsTrue(hit);
        }

        [Test]
        public void RaycastTerrain_MissesClearTerrain()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Empty terrain

            Vec2 from = new Vec2(0f, 5f);
            Vec2 to = new Vec2(0f, 0f);

            bool hit = GamePhysics.RaycastTerrain(t, from, to, out _);
            Assert.IsFalse(hit);
        }

        [Test]
        public void RaycastTerrain_HitsDiagonalCornerPixel()
        {
            // Regression test for #229: Bresenham diagonal skip
            // Place a single solid pixel on the diagonal path such that
            // it occupies an intermediate position the old code would skip.
            var t = new TerrainState(100, 50, 10f, -5f, 0f);

            // Ray goes diagonally from (0,0) to (9,9) in pixel space.
            // Place a solid pixel at an intermediate position (5+1, 5) = (6, 5)
            // that would be skipped when both x and y advance simultaneously.
            // Using pixel coords: set solid at the intermediate pixel the
            // diagonal step from (5,5) to (6,6) would skip.
            int cornerPx = 56; // pixel for an x just past the midpoint
            int cornerPy = 25; // pixel for the same y row
            t.SetSolid(cornerPx, cornerPy, true);

            // Convert those pixel coords to world coords for the ray endpoints
            float startX = t.PixelToWorldX(50);
            float startY = t.PixelToWorldY(20);
            float endX = t.PixelToWorldX(60);
            float endY = t.PixelToWorldY(30);

            Vec2 from = new Vec2(startX, startY);
            Vec2 to = new Vec2(endX, endY);

            bool hit = GamePhysics.RaycastTerrain(t, from, to, out Vec2 hitPoint);
            Assert.IsTrue(hit, "Raycast should detect corner pixel on diagonal step");
        }

        [Test]
        public void ClampToMapBounds_ClampsX()
        {
            Vec2 pos = new Vec2(999f, 5f);
            GamePhysics.ClampToMapBounds(ref pos, -50f, 50f, -30f);
            Assert.AreEqual(50f, pos.x, 0.01f);
        }

        [Test]
        public void ResolveTerrainPenetration_PushesUp()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill pixels 0-30
            for (int px = 0; px < 100; px++)
                for (int py = 0; py < 30; py++)
                    t.SetSolid(px, py, true);

            // Position inside terrain
            Vec2 pos = new Vec2(0f, 2.5f); // inside solid zone
            GamePhysics.ResolveTerrainPenetration(t, ref pos);

            // Should be pushed above solid
            int px2 = t.WorldToPixelX(pos.x);
            int py2 = t.WorldToPixelY(pos.y);
            Assert.IsFalse(t.IsSolid(px2, py2), "Position should be in air after resolution");
        }

        [Test]
        public void ResolveTerrainPenetration_DeepPenetration_FallbackScansFullHeight()
        {
            var t = new TerrainState(100, 80, 10f, -5f, 0f);
            // Fill solid from 0 to 45 — that's 4.5 world units, exceeds maxPush=2
            for (int px = 0; px < 100; px++)
                for (int py = 0; py < 45; py++)
                    t.SetSolid(px, py, true);

            // Place player deep inside terrain at y=0.5 (pixel 5, needs 40 pixels up)
            Vec2 pos = new Vec2(0f, 0.5f);
            GamePhysics.ResolveTerrainPenetration(t, ref pos);

            int py2 = t.WorldToPixelY(pos.y);
            Assert.IsFalse(t.IsSolid(t.WorldToPixelX(pos.x), py2),
                "Deep penetration should resolve via fallback scan");
            Assert.GreaterOrEqual(pos.y, 4.5f,
                "Should be pushed above the solid zone");
        }

        [Test]
        public void ResolveTerrainPenetration_FullySolid_PlacesAboveTerrain()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill entire terrain solid
            for (int px = 0; px < 100; px++)
                for (int py = 0; py < 50; py++)
                    t.SetSolid(px, py, true);

            Vec2 pos = new Vec2(0f, 1.0f);
            GamePhysics.ResolveTerrainPenetration(t, ref pos);

            // IsSolid returns false for out-of-bounds, so scan finds air at Height
            float terrainTop = t.PixelToWorldY(t.Height);
            Assert.AreEqual(terrainTop, pos.y, 0.01f,
                "Fully solid terrain should place player above terrain");
        }
    }
}
