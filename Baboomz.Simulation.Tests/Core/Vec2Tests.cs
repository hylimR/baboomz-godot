using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class Vec2Tests
    {
        [Test]
        public void Addition()
        {
            var a = new Vec2(1f, 2f);
            var b = new Vec2(3f, 4f);
            var c = a + b;
            Assert.AreEqual(4f, c.x, 0.001f);
            Assert.AreEqual(6f, c.y, 0.001f);
        }

        [Test]
        public void Subtraction()
        {
            var r = new Vec2(5f, 3f) - new Vec2(2f, 1f);
            Assert.AreEqual(3f, r.x, 0.001f);
            Assert.AreEqual(2f, r.y, 0.001f);
        }

        [Test]
        public void ScalarMultiply()
        {
            var v = new Vec2(2f, 3f) * 2f;
            Assert.AreEqual(4f, v.x, 0.001f);
            Assert.AreEqual(6f, v.y, 0.001f);
        }

        [Test]
        public void Magnitude()
        {
            var v = new Vec2(3f, 4f);
            Assert.AreEqual(5f, v.Magnitude, 0.001f);
        }

        [Test]
        public void Normalized()
        {
            var v = new Vec2(0f, 5f).Normalized;
            Assert.AreEqual(0f, v.x, 0.001f);
            Assert.AreEqual(1f, v.y, 0.001f);
        }

        [Test]
        public void Distance()
        {
            float d = Vec2.Distance(new Vec2(0f, 0f), new Vec2(3f, 4f));
            Assert.AreEqual(5f, d, 0.001f);
        }

        [Test]
        public void ZeroVector_Normalized_ReturnsZero()
        {
            var v = Vec2.Zero.Normalized;
            Assert.AreEqual(0f, v.Magnitude, 0.001f);
        }
    }
}
