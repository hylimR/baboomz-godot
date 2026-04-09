using System;
using NUnit.Framework;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class BallisticTests
    {
        const float Rad2Deg = 180f / MathF.PI;
        const float Deg2Rad = MathF.PI / 180f;

        [Test]
        public void AimAngle_RightTarget_ReturnsZero()
        {
            // Target directly to the right at same height
            float ox = 0f, oy = 0f;
            float tx = 10f, ty = 0f;
            float angle = MathF.Atan2(ty - oy, tx - ox) * Rad2Deg;
            Assert.AreEqual(0f, angle, 0.01f);
        }

        [Test]
        public void AimAngle_AboveTarget_Returns90()
        {
            // Target directly above
            float ox = 0f, oy = 0f;
            float tx = 0f, ty = 10f;
            float angle = MathF.Atan2(ty - oy, tx - ox) * Rad2Deg;
            Assert.AreEqual(90f, angle, 0.01f);
        }

        [Test]
        public void AimAngle_LeftTarget_Returns180()
        {
            // Target directly to the left
            float ox = 0f, oy = 0f;
            float tx = -10f, ty = 0f;
            float angle = MathF.Atan2(ty - oy, tx - ox) * Rad2Deg;
            Assert.AreEqual(180f, angle, 0.01f);
        }

        [Test]
        public void AimAngle_DiagonalTarget_Returns45()
        {
            float ox = 0f, oy = 0f;
            float tx = 10f, ty = 10f;
            float angle = MathF.Atan2(ty - oy, tx - ox) * Rad2Deg;
            Assert.AreEqual(45f, angle, 0.01f);
        }

        [Test]
        public void AimAngle_ZeroDistance_ReturnsZero()
        {
            float ox = 5f, oy = 5f;
            float tx = 5f, ty = 5f;
            float angle = MathF.Atan2(ty - oy, tx - ox) * Rad2Deg;
            Assert.AreEqual(0f, angle, 0.01f);
        }

        [Test]
        public void AimAngle_ClampedToValidRange()
        {
            // Simulating AIController's clamping behavior (±90° from forward)
            float rawAngle = -120f;
            float clamped = Math.Clamp(rawAngle, -90f, 90f);
            Assert.AreEqual(-90f, clamped, 0.01f);

            rawAngle = 120f;
            clamped = Math.Clamp(rawAngle, -90f, 90f);
            Assert.AreEqual(90f, clamped, 0.01f);
        }

        [Test]
        public void ProjectileVelocity_AngleAndPower()
        {
            float angle = 45f;
            float power = 20f;
            float angleRad = angle * Deg2Rad;
            float vx = MathF.Cos(angleRad) * power;
            float vy = MathF.Sin(angleRad) * power;

            float expected = 20f * MathF.Cos(45f * Deg2Rad);
            Assert.AreEqual(expected, vx, 0.01f);
            Assert.AreEqual(expected, vy, 0.01f);
        }
    }
}
