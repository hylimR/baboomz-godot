using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class TerrainStateTests
    {
        [Test]
        public void NewTerrain_IsEmpty()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            Assert.IsFalse(t.IsSolid(50, 25));
        }

        [Test]
        public void SetSolid_MakesPixelSolid()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 20, true);
            Assert.IsTrue(t.IsSolid(10, 20));
        }

        [Test]
        public void SetSolid_False_ClearsPixel()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 20, true);
            t.SetSolid(10, 20, false);
            Assert.IsFalse(t.IsSolid(10, 20));
        }

        [Test]
        public void OutOfBounds_ReturnsFalse()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            Assert.IsFalse(t.IsSolid(-1, 0));
            Assert.IsFalse(t.IsSolid(100, 0));
            Assert.IsFalse(t.IsSolid(0, -1));
            Assert.IsFalse(t.IsSolid(0, 50));
        }

        [Test]
        public void IsSurface_SolidWithAirAbove()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 5, true);
            // Air above — surface
            Assert.IsTrue(t.IsSurface(10, 5));
        }

        [Test]
        public void IsSurface_SolidWithSolidAbove_NotSurface()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 5, true);
            t.SetSolid(10, 6, true);
            Assert.IsFalse(t.IsSurface(10, 5));
        }

        [Test]
        public void Indestructible_SurvivesClearCircle()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.FillRect(0, 0, 100, 50);
            t.SetIndestructible(50, 25, true);

            t.ClearCircleDestructible(50, 25, 5);

            // Indestructible pixel survives
            Assert.IsTrue(t.IsSolid(50, 25));
            Assert.IsTrue(t.IsIndestructible(50, 25));

            // Nearby destructible pixels are cleared
            Assert.IsFalse(t.IsSolid(50, 22));
        }

        [Test]
        public void ClearCircle_Force_DestroysIndestructible()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.FillRect(0, 0, 100, 50);
            t.SetIndestructible(50, 25, true);

            t.ClearCircle(50, 25, 5);

            Assert.IsFalse(t.IsSolid(50, 25));
        }

        [Test]
        public void WorldToPixel_Roundtrip()
        {
            var t = new TerrainState(160, 80, 16f, -5f, -2f);
            int px = t.WorldToPixelX(0f);
            int py = t.WorldToPixelY(0f);
            float wx = t.PixelToWorldX(px);
            float wy = t.PixelToWorldY(py);
            Assert.AreEqual(0f, wx, 0.1f);
            Assert.AreEqual(0f, wy, 0.1f);
        }

        [Test]
        public void FillRectIndestructible_SetsCorrectFlags()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.FillRectIndestructible(10, 10, 5, 5);

            Assert.IsTrue(t.IsSolid(12, 12));
            Assert.IsTrue(t.IsIndestructible(12, 12));
            Assert.IsFalse(t.IsIndestructible(9, 9));
        }
    }
}
