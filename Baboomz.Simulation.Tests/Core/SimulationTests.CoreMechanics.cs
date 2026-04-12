using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void RunMultipleMatches_NoExceptions()
        {
            var config = SmallConfig();

            Assert.DoesNotThrow(() =>
            {
                for (int match = 0; match < 50; match++)
                {
                    var state = GameSimulation.CreateMatch(config, match);
                    AILogic.Reset(match);

                    for (int frame = 0; frame < 300; frame++)
                    {
                        GameSimulation.Tick(state, 0.016f);

                        if (state.Phase == MatchPhase.Ended) break;
                    }
                }
            });
        }

        [Test]
        public void RunMatch_AIEventuallyFires()
        {
            var config = SmallConfig();
            config.AIShootInterval = 0.5f;
            config.AIShootIntervalRandomness = 0.1f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            bool aiFired = false;
            for (int i = 0; i < 300; i++)
            {
                int projBefore = state.Projectiles.Count;
                GameSimulation.Tick(state, 0.016f);

                if (state.Projectiles.Count > projBefore)
                {
                    // Check if AI fired (owner = 1)
                    for (int j = projBefore; j < state.Projectiles.Count; j++)
                    {
                        if (state.Projectiles[j].OwnerIndex == 1)
                        {
                            aiFired = true;
                            break;
                        }
                    }
                }
                if (aiFired) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.IsTrue(aiFired, "AI should fire at least once within 300 frames");
        }

        [Test]
        public void Tick_DoesNothing_WhenMatchEnded()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Ended;

            float timeBefore = state.Time;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(timeBefore, state.Time, "Time should not advance when ended");
        }

        [Test]
        public void DeadPlayer_IsNotUpdated()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            var posBefore = state.Players[0].Position;

            // Only one alive — match should end, but check position didn't change
            state.Input.MoveX = 1f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(posBefore.x, state.Players[0].Position.x, 0.001f);
        }

        [Test]
        public void DeadPlayer_NoDoubleFallDamageKill()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 1f;
            config.FallDamagePerMeter = 100f;
            config.MatchType = MatchType.Survival;
            config.SurvivalScorePerKill = 10;
            var state = GameSimulation.CreateMatch(config, 42);

            // Mark player 0 as a mob so ScoreSurvivalKill can score
            state.Players[0].IsMob = true;

            // Simulate: player is dead and airborne (killed by explosion mid-air)
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[0].IsGrounded = false;
            state.Players[0].LastGroundedY = state.Players[0].Position.y + 20f;
            state.Players[0].Velocity = new Vec2(0f, -5f);

            int scoreBefore = state.Survival.Score;

            // Tick many frames — dead player should NOT accumulate fall damage or trigger ScoreSurvivalKill
            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(scoreBefore, state.Survival.Score,
                "Dead player landing should not trigger a second ScoreSurvivalKill");
        }

        [Test]
        public void FallDamage_LargeDropDealsDamage()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 3f;
            config.FallDamagePerMeter = 10f;
            config.FallDamageMax = 50f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on ground first
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded, "Player should be grounded after settling");
            float groundY = state.Players[0].Position.y;

            // Move player high above current position
            state.Players[0].Position = new Vec2(state.Players[0].Position.x, groundY + 15f);
            state.Players[0].LastGroundedY = groundY + 15f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;

            float startHealth = state.Players[0].Health;

            // Tick until player lands
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsGrounded) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.Less(state.Players[0].Health, startHealth,
                "Player should take fall damage from 15m drop");
        }

        [Test]
        public void FallDamage_RespectsArmorMultiplier()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 3f;
            config.FallDamagePerMeter = 10f;
            config.FallDamageMax = 50f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on ground first
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded, "Player should be grounded after settling");
            float groundY = state.Players[0].Position.y;

            // Give player 2x armor (shield skill sets this)
            state.Players[0].ArmorMultiplier = 2f;

            // Move player high above current position
            state.Players[0].Position = new Vec2(state.Players[0].Position.x, groundY + 15f);
            state.Players[0].LastGroundedY = groundY + 15f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;

            float startHealth = state.Players[0].Health;

            // Tick until player lands
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsGrounded) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            float damageTaken = startHealth - state.Players[0].Health;
            Assert.Greater(damageTaken, 0f, "Player should still take some fall damage");

            // With 2x armor, damage should be halved compared to unarmored
            // Unarmored: excess * 10 capped at 50 → for 12m excess = 50 (capped)
            // Armored: 50 / 2 = 25
            Assert.LessOrEqual(damageTaken, 25f + 0.1f,
                "Fall damage should be reduced by ArmorMultiplier");
        }

        [Test]
        public void FallDamage_InvulnerablePlayerTakesNoDamage()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 3f;
            config.FallDamagePerMeter = 10f;
            config.FallDamageMax = 50f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on ground first
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded, "Player should be grounded after settling");
            float groundY = state.Players[0].Position.y;

            // Move player high above current position and flag invulnerable (e.g. boss shield phase)
            state.Players[0].Position = new Vec2(state.Players[0].Position.x, groundY + 15f);
            state.Players[0].LastGroundedY = groundY + 15f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;
            state.Players[0].IsInvulnerable = true;

            float startHealth = state.Players[0].Health;

            // Tick until player lands
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsGrounded) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.IsTrue(state.Players[0].IsGrounded, "Invulnerable player should still land");
            Assert.AreEqual(startHealth, state.Players[0].Health, 0.001f,
                "Invulnerable player should take no fall damage on landing");
        }

        [Test]
        public void UpwardCollision_PlayerDoesNotClipThroughCeiling()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle on ground
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            float groundY = state.Players[0].Position.y;
            float playerX = state.Players[0].Position.x;

            // Place a solid ceiling 3 units above the player (head is at +1.5, so ceiling at +3)
            float ceilingWorldY = groundY + 3f;
            int ceilPy = state.Terrain.WorldToPixelY(ceilingWorldY);
            int centerPx = state.Terrain.WorldToPixelX(playerX);
            // Fill a wide ceiling slab (20 pixels wide, 5 pixels thick)
            for (int dx = -10; dx <= 10; dx++)
                for (int dy = 0; dy < 5; dy++)
                    state.Terrain.SetSolid(centerPx + dx, ceilPy + dy, true);

            // Give player upward velocity to jump into the ceiling
            state.Players[0].Velocity = new Vec2(0f, 15f);
            state.Players[0].IsGrounded = false;

            float startY = state.Players[0].Position.y;

            // Tick a few frames — player should be blocked by ceiling
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should not have passed through the ceiling
            Assert.Less(state.Players[0].Position.y, ceilingWorldY,
                "Player should be blocked by ceiling terrain, not clip through it");
        }

        [Test]
        public void MultipleWeapons_AllPopulated()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Default config has 4 weapons
            Assert.IsNotNull(state.Players[0].WeaponSlots[0].WeaponId, "Slot 0 should have cannon");
            Assert.IsNotNull(state.Players[0].WeaponSlots[1].WeaponId, "Slot 1 should have shotgun");
            Assert.IsNotNull(state.Players[0].WeaponSlots[2].WeaponId, "Slot 2 should have rocket");
            Assert.IsNotNull(state.Players[0].WeaponSlots[3].WeaponId, "Slot 3 should have drill");
        }

        [Test]
        public void Shotgun_FiresMultipleProjectiles()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 1; // shotgun
            state.Players[0].AimPower = 20f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(4, state.Projectiles.Count, "Shotgun should fire 4 projectiles");
        }

        [Test]
        public void Rocket_HasLimitedAmmo()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 2; // rocket (4 ammo)
            state.Players[0].AimPower = 20f;

            Assert.AreEqual(4, state.Players[0].WeaponSlots[2].Ammo);

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(3, state.Players[0].WeaponSlots[2].Ammo);

            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 20f;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(2, state.Players[0].WeaponSlots[2].Ammo);
        }

        [Test]
        public void WindChanges_DuringMatch()
        {
            var config = SmallConfig();
            config.WindChangeInterval = 0.1f; // change every 0.1s
            var state = GameSimulation.CreateMatch(config, 42);

            float initialWind = state.WindForce;

            // Tick past the wind change interval
            for (int i = 0; i < 20; i++)
                GameSimulation.Tick(state, 0.016f);

            // Wind should have changed at least once
            // (may or may not be different value, but NextWindChangeTime should have advanced)
            Assert.Greater(state.NextWindChangeTime, 0.1f, "Wind change time should advance");
        }

        [Test]
        public void EnergyWeapon_CannotFire_WhenOutOfEnergy()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 1; // shotgun (18 energy cost)
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 5f; // not enough

            int before = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(before, state.Projectiles.Count, "Should not fire without enough energy");
        }

        [Test]
        public void Fire_ZeroAmmo_DoesNotDeductEnergy()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 1; // shotgun (18 energy cost)
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            state.Players[0].WeaponSlots[1].Ammo = 0; // depleted

            float energyBefore = state.Players[0].Energy;
            int projBefore = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, "Energy must not be deducted when ammo is 0");
            Assert.AreEqual(projBefore, state.Projectiles.Count, "No projectile should be created when ammo is 0");
        }
    }
}
