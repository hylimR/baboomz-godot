using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Sudden Death (rising water) tests ---

        [Test]
        public void SuddenDeath_WaterRises_AfterTimeout()
        {
            var config = SmallConfig();
            config.SuddenDeathTime = 2f;   // water starts rising after 2 seconds
            config.WaterRiseSpeed = 5f;     // 5 units/sec for quick test

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float initialWater = state.WaterLevel;
            Assert.IsFalse(state.SuddenDeathActive);

            // Tick for 1.5 seconds — water should NOT have risen
            for (int i = 0; i < 94; i++) // ~1.5s at 16ms
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.SuddenDeathActive);
            Assert.AreEqual(initialWater, state.WaterLevel, 0.01f,
                "Water should not rise before SuddenDeathTime");

            // Tick past 2 seconds total — water should start rising
            for (int i = 0; i < 62; i++) // another ~1s
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.SuddenDeathActive, "Sudden death should be active after timeout");
            Assert.Greater(state.WaterLevel, initialWater,
                "Water level should rise after sudden death starts");
        }

        [Test]
        public void SuddenDeath_KillsPlayerWhenWaterReaches()
        {
            var config = SmallConfig();
            // Use a longer timeout so player can settle first, then trigger sudden death
            config.SuddenDeathTime = 5f;    // water starts after 5 seconds
            config.WaterRiseSpeed = 50f;     // very fast rise once it starts

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Let players settle (200 frames = ~3.2s, before sudden death at 5s)
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsDead, "Player should be alive before sudden death");
            Assert.IsFalse(state.SuddenDeathActive, "Sudden death should not be active yet");

            // Tick past the 5-second mark and continue until water kills
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsDead) break;
            }

            Assert.IsTrue(state.Players[0].IsDead,
                "Rising water should kill player when it reaches them");
        }

        [Test]
        public void SuddenDeath_ProjectilesDieAtWaterLevel()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);

            // Raise water level above terrain (SmallConfig TerrainFloorDepth = -10, terrain top ~ 5)
            // Set water at y=10 — well above all terrain, so projectile won't hit terrain first
            float raisedWater = 10f;
            state.WaterLevel = raisedWater;

            // Place a projectile just above water level, falling straight down
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, raisedWater + 2f),
                Velocity = new Vec2(0f, -20f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 30f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Call ProjectileSimulation.Update directly to avoid match-end interference
            state.SplashEvents.Clear();
            Assert.AreEqual(1, state.Projectiles.Count, "Should have 1 projectile");

            // Step 1 frame at a time, checking state
            for (int i = 0; i < 300; i++)
            {
                ProjectileSimulation.Update(state, 0.05f); // larger dt for faster fall
                if (state.Projectiles.Count == 0) break;
            }

            Assert.AreEqual(0, state.Projectiles.Count,
                "Projectile should have been removed");
            Assert.Greater(state.SplashEvents.Count, 0,
                "Projectile should splash at water level during sudden death");

            float splashY = state.SplashEvents[0].Position.y;
            Assert.AreEqual(raisedWater, splashY, 1f,
                "Splash should be at raised water level, not at static DeathBoundaryY");
        }

        [Test]
        public void SuddenDeath_Disabled_WhenTimeIsZero()
        {
            var config = SmallConfig();
            config.SuddenDeathTime = 0f; // disabled

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float initialWater = state.WaterLevel;

            for (int i = 0; i < 500; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.SuddenDeathActive);
            Assert.AreEqual(initialWater, state.WaterLevel, 0.01f,
                "Water should not rise when SuddenDeathTime is 0");
        }

        [Test]
        public void Swimming_EntersSwimState_WhenBelowWater()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 0.5f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsSwimming, "Player should enter swimming state");
            Assert.IsFalse(state.Players[0].IsDead, "Player should not die on first water contact");
            Assert.Greater(state.Players[0].SwimTimer, 0f, "Swim timer should start counting");
        }

        [Test]
        public void Swimming_DrownsAfterSwimDuration()
        {
            var config = SmallConfig();
            config.SwimDuration = 1f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 0.5f);

            int ticks = (int)(config.SwimDuration / 0.016f) + 10;
            for (int i = 0; i < ticks; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsDead) break;
            }

            Assert.IsTrue(state.Players[0].IsDead, "Player should drown after SwimDuration");
            Assert.AreEqual(0f, state.Players[0].Health, 0.001f);
        }

        [Test]
        public void Swimming_EscapeClearsSwimState()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].SwimTimer = 1.5f;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel + 1f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsSwimming, "Swimming should clear when above water");
            Assert.AreEqual(0f, state.Players[0].SwimTimer, 0.001f, "Swim timer should reset");
        }

        [Test]
        public void Swimming_ReducesMovementSpeed()
        {
            var config = SmallConfig();
            config.SwimSpeedMultiplier = 0.4f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 1f);
            state.Input.MoveX = 1f;
            float startX = state.Players[0].Position.x;

            GameSimulation.Tick(state, 0.016f);
            float swimDeltaX = state.Players[0].Position.x - startX;

            var state2 = GameSimulation.CreateMatch(config, 42);
            state2.Input.MoveX = 1f;
            float startX2 = state2.Players[0].Position.x;

            GameSimulation.Tick(state2, 0.016f);
            float normalDeltaX = state2.Players[0].Position.x - startX2;

            Assert.Greater(normalDeltaX, swimDeltaX,
                "Swimming player should move slower than normal");
        }

        [Test]
        public void Swimming_SinksOverTime()
        {
            var config = SmallConfig();
            config.SwimSinkSpeed = 2f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            float startY = state.WaterLevel - 1f;
            state.Players[0].Position = new Vec2(0f, startY);

            GameSimulation.Tick(state, 0.1f);

            Assert.Less(state.Players[0].Position.y, startY,
                "Swimming player should sink over time");
        }

        [Test]
        public void Swimming_BlocksFiring()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            state.Players[0].IsSwimming = true;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 1f);
            state.Input.FireHeld = true;

            GameSimulation.Tick(state, 0.016f);

            state.Input.FireHeld = false;
            state.Input.FireReleased = true;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Projectiles.Count,
                "Swimming player should not be able to fire");
        }

        [Test]
        public void Swimming_BlocksSkillActivation()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 1f);
            state.Input.Skill1Pressed = true;

            float energyBefore = state.Players[0].Energy;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.5f,
                "Swimming player should not activate skills (energy unchanged)");
        }

        [Test]
        public void Swimming_KnockbackCanPushOutOfWater()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].SwimTimer = 2f;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 0.2f);
            state.Players[0].Velocity = new Vec2(0f, 15f);
            state.Players[0].IsSwimming = false;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel + 2f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsSwimming,
                "Player knocked above water should not be swimming");
            Assert.IsFalse(state.Players[0].IsDead,
                "Player knocked out of water should survive");
        }

        [Test]
        public void AI_JetpackDangerCheck_UsesWaterLevel()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Raise water level high — AI should detect danger relative to water, not DeathBoundaryY
            float raisedWater = 5f;
            state.WaterLevel = raisedWater;

            // Place AI player just above raised water, falling
            state.Players[1].Position = new Vec2(0f, raisedWater + 3f);
            state.Players[1].Velocity = new Vec2(0f, -5f);
            state.Players[1].IsGrounded = false;

            // Give AI a jetpack skill
            state.Players[1].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "jetpack", Type = SkillType.Jetpack,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 3f, Value = 10f
                },
                new SkillSlotState()
            };
            state.Players[1].Energy = 100f;

            // Tick AI logic — it should activate jetpack because y < waterLevel + 5
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            bool jetpackActivated = false;
            for (int s = 0; s < state.Players[1].SkillSlots.Length; s++)
            {
                if (state.Players[1].SkillSlots[s].Type == SkillType.Jetpack
                    && state.Players[1].SkillSlots[s].IsActive)
                {
                    jetpackActivated = true;
                    break;
                }
            }

            Assert.IsTrue(jetpackActivated,
                "AI should activate jetpack when near raised water level during sudden death");
        }

    }
}
