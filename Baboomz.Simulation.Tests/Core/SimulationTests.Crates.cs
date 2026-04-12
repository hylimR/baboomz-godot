using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Crate drop tests ---

        [Test]
        public void Crates_SpawnAfterInterval()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 2f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            Assert.AreEqual(0, state.Crates.Count);

            // Tick past the first spawn interval (2 seconds)
            for (int i = 0; i < 200; i++) // ~3.2 seconds
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Crates.Count, 0, "Crate should spawn after interval");
        }

        [Test]
        public void Crates_SpawnedTypeIsValidEnumValue()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 1f;
            config.SuddenDeathTime = 0f;

            int crateTypeCount = System.Enum.GetValues(typeof(CrateType)).Length;

            // Test many seeds to cover random distribution
            for (int seed = 0; seed < 100; seed++)
            {
                var state = GameSimulation.CreateMatch(config, seed);
                AILogic.Reset(seed);

                for (int i = 0; i < 200; i++)
                    GameSimulation.Tick(state, 0.016f);

                foreach (var crate in state.Crates)
                {
                    int typeInt = (int)crate.Type;
                    Assert.IsTrue(typeInt >= 0 && typeInt < crateTypeCount,
                        $"Crate type {crate.Type} ({typeInt}) out of valid enum range [0, {crateTypeCount}) for seed {seed}");
                }
            }
        }

        [Test]
        public void Crates_Disabled_WhenIntervalIsZero()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int i = 0; i < 500; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Crates.Count, "No crates when interval is 0");
        }

        [Test]
        public void Crates_CollectedWhenPlayerWalksOver()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Damage player, then place health crate exactly at their position
            state.Players[0].Health = 50f;
            Vec2 playerPos = state.Players[0].Position;
            state.Crates.Add(new CrateState
            {
                Position = playerPos,
                Type = CrateType.Health,
                Active = true,
                Grounded = true
            });

            // Stop player movement to prevent drifting
            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Crates.Count, "Crate should be collected and removed");
            Assert.Greater(state.Players[0].Health, 50f, "Health crate should restore HP");
        }

        [Test]
        public void Crates_AmmoRefill_RestoresLimitedAmmoWeapons()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Verify rocket weapon exists and deplete it
            Assert.AreEqual("rocket", state.Players[0].WeaponSlots[2].WeaponId);
            state.Players[0].WeaponSlots[2].Ammo = 0;

            // Place ammo crate exactly at player position (grounded, no physics)
            state.Crates.Add(new CrateState
            {
                Position = state.Players[0].Position,
                Type = CrateType.AmmoRefill,
                Active = true,
                Grounded = true
            });

            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Crates.Count, "Ammo crate should be collected and removed");
            Assert.AreEqual(4, state.Players[0].WeaponSlots[2].Ammo,
                "Ammo crate should refill rocket ammo to original count");
        }

        [Test]
        public void Crates_DoubleDamage_AppliesAndExpires()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;
            config.CrateDoubleDamageDuration = 1f; // 1 second duration for quick test

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            Assert.AreEqual(1f, state.Players[0].DamageMultiplier, 0.01f);

            // Place double damage crate at player
            state.Crates.Add(new CrateState
            {
                Position = state.Players[0].Position,
                Type = CrateType.DoubleDamage,
                Active = true,
                Grounded = true
            });

            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "Double damage should be active after collecting crate");
            Assert.Greater(state.Players[0].DoubleDamageTimer, 0f,
                "Timer should be set");

            // Tick past the 1-second duration
            for (int i = 0; i < 80; i++) // ~1.3 seconds
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "Damage multiplier should reset after buff expires");
            Assert.AreEqual(0f, state.Players[0].DoubleDamageTimer, 0.01f,
                "Timer should be zero after expiry");
        }

        [Test]
        public void Crates_FallingCrate_LandsOnTerrain()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add a falling crate high above terrain
            state.Crates.Add(new CrateState
            {
                Position = new Vec2(0f, 20f),
                Velocity = Vec2.Zero,
                Type = CrateType.Health,
                Active = true,
                Grounded = false
            });

            Assert.IsFalse(state.Crates[0].Grounded);

            // Tick until crate lands or times out
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Crates[0].Grounded) break;
                if (!state.Crates[0].Active) break; // fell into void
            }

            // Crate should either land on terrain or fall into void
            Assert.IsTrue(state.Crates[0].Grounded || !state.Crates[0].Active,
                "Falling crate should either land on terrain or deactivate in void");
        }

        [Test]
        public void Crates_SubmergedByRisingWater_Deactivate()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 1f;
            config.WaterRiseSpeed = 100f; // very fast rise

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a grounded crate at a known low position
            state.Crates.Add(new CrateState
            {
                Position = new Vec2(0f, -15f), // near death boundary
                Type = CrateType.Health,
                Active = true,
                Grounded = true
            });

            Assert.AreEqual(1, state.Crates.Count);

            // Tick until water rises past the crate and it gets removed
            for (int i = 0; i < 200; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Crates.Count == 0) break;
            }

            Assert.AreEqual(0, state.Crates.Count,
                "Grounded crate should be removed when submerged by rising water");
        }

        [Test]
        public void FullMatch_WithCratesAndSuddenDeath_Stable()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 5f;
            config.SuddenDeathTime = 10f;
            config.WaterRiseSpeed = 2f;
            config.AIShootInterval = 1f;

            Assert.DoesNotThrow(() =>
            {
                for (int match = 0; match < 20; match++)
                {
                    var state = GameSimulation.CreateMatch(config, match * 17);
                    AILogic.Reset(match * 17);

                    for (int frame = 0; frame < 2000; frame++)
                    {
                        GameSimulation.Tick(state, 0.016f);
                        if (state.Phase == MatchPhase.Ended) break;
                    }
                }
            });
        }

    }
}
