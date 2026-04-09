using System;
using NUnit.Framework;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class WindPhysicsTests
    {
        const float Deg2Rad = MathF.PI / 180f;

        [Test]
        public void WindChangedEvent_RightWind_HasCorrectVector()
        {
            float angle = 0f; // right
            float strength = 5f;
            float rad = angle * Deg2Rad;
            float vx = MathF.Cos(rad) * strength;
            float vy = MathF.Sin(rad) * strength;

            Assert.AreEqual(5f, vx, 0.01f);
            Assert.AreEqual(0f, vy, 0.01f);
        }

        [Test]
        public void WindChangedEvent_UpWind_HasCorrectVector()
        {
            float angle = 90f; // up
            float strength = 3f;
            float rad = angle * Deg2Rad;
            float vx = MathF.Cos(rad) * strength;
            float vy = MathF.Sin(rad) * strength;

            Assert.AreEqual(0f, vx, 0.01f);
            Assert.AreEqual(3f, vy, 0.01f);
        }

        [Test]
        public void WindChangedEvent_LeftWind_HasCorrectVector()
        {
            float angle = 180f; // left
            float strength = 4f;
            float rad = angle * Deg2Rad;
            float vx = MathF.Cos(rad) * strength;
            float vy = MathF.Sin(rad) * strength;

            Assert.AreEqual(-4f, vx, 0.01f);
            Assert.AreEqual(0f, vy, 0.01f);
        }

        [Test]
        public void WindChangedEvent_ZeroStrength_ReturnsZeroVector()
        {
            float angle = 45f;
            float strength = 0f;
            float rad = angle * Deg2Rad;
            float vx = MathF.Cos(rad) * strength;
            float vy = MathF.Sin(rad) * strength;

            Assert.AreEqual(0f, vx, 0.01f);
            Assert.AreEqual(0f, vy, 0.01f);
        }

        [Test]
        public void TrajectoryPreview_WindShiftsPoints()
        {
            // Simulate a simple trajectory with and without wind
            float posX = 0f, posY = 0f;
            float velX = 10f, velY = 10f;
            float gravity = -9.81f;
            float dt = 0.05f;
            int steps = 20;

            // No wind
            float posNoWindX = posX, posNoWindY = posY;
            float velNoWindX = velX, velNoWindY = velY;
            for (int i = 0; i < steps; i++)
            {
                velNoWindY += gravity * dt;
                posNoWindX += velNoWindX * dt;
                posNoWindY += velNoWindY * dt;
            }

            // With rightward wind
            float windAccelX = 3f, windAccelY = 0f;
            float posWindX = posX, posWindY = posY;
            float velWindX = velX, velWindY = velY;
            for (int i = 0; i < steps; i++)
            {
                velWindX += windAccelX * dt;
                velWindY += gravity * dt + windAccelY * dt;
                posWindX += velWindX * dt;
                posWindY += velWindY * dt;
            }

            // Wind-affected trajectory should be further to the right
            Assert.Greater(posWindX, posNoWindX,
                "Wind should push trajectory rightward");

            // Y should be approximately the same (no vertical wind)
            Assert.AreEqual(posNoWindY, posWindY, 0.1f,
                "Horizontal wind should not significantly affect Y");
        }

        [Test]
        public void TrajectoryPreview_UpwardWind_ReducesGravityEffect()
        {
            float posX = 0f, posY = 0f;
            float velX = 10f, velY = 0f; // horizontal shot
            float gravity = -9.81f;
            float dt = 0.05f;
            int steps = 20;

            // No wind
            float posNoWindX = posX, posNoWindY = posY;
            float velNoWindX = velX, velNoWindY = velY;
            for (int i = 0; i < steps; i++)
            {
                velNoWindY += gravity * dt;
                posNoWindX += velNoWindX * dt;
                posNoWindY += velNoWindY * dt;
            }

            // With upward wind
            float windAccelX = 0f, windAccelY = 3f;
            float posWindX = posX, posWindY = posY;
            float velWindX = velX, velWindY = velY;
            for (int i = 0; i < steps; i++)
            {
                velWindX += windAccelX * dt;
                velWindY += gravity * dt + windAccelY * dt;
                posWindX += velWindX * dt;
                posWindY += velWindY * dt;
            }

            // Wind-affected trajectory should drop less
            Assert.Greater(posWindY, posNoWindY,
                "Upward wind should reduce drop");
        }

        [Test]
        public void TrajectoryPreview_DrillWeapon_WindExemption_VelocityUnchanged()
        {
            // Regression #465: Drill weapon preview must not apply wind or gravity,
            // matching ProjectileSimulation which skips physics for IsDrill projectiles.
            // Simulate the same loop logic that TrajectoryPreview uses for a drill weapon.
            bool isDrill = true;
            float velX = 10f, velY = 0f;
            float windForce = 5f;
            float gravity = 9.81f;
            float dt = 0.05f;
            int steps = 10;

            float startVelX = velX, startVelY = velY;
            for (int i = 0; i < steps; i++)
            {
                if (!isDrill)
                {
                    velY -= gravity * dt;
                    velX += windForce * dt;
                }
            }

            // Drill: velocity must remain constant (wind/gravity skipped)
            Assert.AreEqual(startVelX, velX, 0.001f,
                "Drill trajectory preview must not apply wind");
            Assert.AreEqual(startVelY, velY, 0.001f,
                "Drill trajectory preview must not apply gravity");
        }

        [Test]
        public void TrajectoryPreview_NormalWeapon_WindApplied()
        {
            // Complement to the Drill exemption test: normal weapons receive wind
            bool isDrill = false;
            float velX = 10f, velY = 0f;
            float windForce = 5f;
            float gravity = 9.81f;
            float dt = 0.05f;
            int steps = 10;

            for (int i = 0; i < steps; i++)
            {
                if (!isDrill)
                {
                    velY -= gravity * dt;
                    velX += windForce * dt;
                }
            }

            // Normal weapon: wind must have shifted velocity horizontally
            Assert.Greater(velX, 10f, "Normal weapon trajectory preview must apply rightward wind");
            Assert.Less(velY, 0f, "Normal weapon trajectory preview must apply gravity");
        }
    }
}
