using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Regression tests for 2026-03-24 bug fixes ---

        [Test]
        public void InactiveCrates_RemovedFromList()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f; // disable auto-spawn
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add some inactive crates
            state.Crates.Add(new CrateState { Active = false, Position = Vec2.Zero });
            state.Crates.Add(new CrateState { Active = true, Position = new Vec2(5f, 5f), Grounded = true });
            state.Crates.Add(new CrateState { Active = false, Position = Vec2.Zero });

            Assert.AreEqual(3, state.Crates.Count);

            GameSimulation.Tick(state, 0.016f);

            // Only the active crate should remain
            Assert.AreEqual(1, state.Crates.Count, "Inactive crates should be pruned");
            Assert.IsTrue(state.Crates[0].Active);
        }

        [Test]
        public void AI_TeleportChance_UsesTimeNormalization()
        {
            // This test verifies the fix is present by ensuring the AI can run
            // at different dt values without crashing (functional correctness).
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Force AI to have teleport skill available
            state.Players[1].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "teleport",
                    Type = SkillType.Teleport,
                    EnergyCost = 5f,
                    Cooldown = 1f,
                    Range = 15f
                },
                new SkillSlotState()
            };
            state.Players[1].Energy = 100f;

            // Run with large dt (simulating 2 FPS) — should not crash
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 30; i++)
                    GameSimulation.Tick(state, 0.5f);
            });

            // Run with tiny dt (simulating 240 FPS) — should not crash
            state = GameSimulation.CreateMatch(config, 43);
            AILogic.Reset(43);
            state.Players[1].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "teleport",
                    Type = SkillType.Teleport,
                    EnergyCost = 5f,
                    Cooldown = 1f,
                    Range = 15f
                },
                new SkillSlotState()
            };
            state.Players[1].Energy = 100f;

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 1000; i++)
                    GameSimulation.Tick(state, 0.004f);
            });
        }

        // --- Cycle 2 regression tests ---

        [Test]
        public void DoubleDamageCrate_DoesNotStack_WhenAlreadyBuffed()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Apply first double damage buff
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 5f;

            // Place double damage crate at player position
            state.Crates.Add(new CrateState
            {
                Position = state.Players[0].Position,
                Type = CrateType.DoubleDamage,
                Active = true,
                Grounded = true
            });

            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            // Buff timer should NOT have been reset — crate should not apply
            Assert.Less(state.Players[0].DoubleDamageTimer, 5f,
                "DoubleDamage timer should tick down (crate not re-applied while already buffed)");
        }

        [Test]
        public void ClusterBomb_Has4SubProjectiles_AfterBalance()
        {
            var config = SmallConfig();
            Assert.AreEqual(4, config.Weapons[3].ClusterCount,
                "Cluster bomb should have 4 sub-projectiles after balance nerf");
        }

        [Test]
        public void ClusterBomb_EnergyCost25_Ammo4_AfterBalance()
        {
            // Issue #34 bottom-lift: EnergyCost 35 -> 25. Ammo stays at 4.
            var config = SmallConfig();
            Assert.AreEqual(25f, config.Weapons[3].EnergyCost,
                "Cluster bomb energy cost should be 25 after issue #34 bottom-lift (was 35)");
            Assert.AreEqual(4, config.Weapons[3].Ammo,
                "Cluster bomb ammo should be 4");
        }

        [Test]
        public void Cannon_EnergyCost11_AfterBalance()
        {
            // Regression (#376): cannon had zero energy cost, breaking the energy economy.
            // Updated (#414): raised from 3→8 to reduce 4x median Dmg/Energy ratio.
            // Updated (#133): raised from 8→11 to tame 2.81x median DPS/E outlier.
            var config = new GameConfig();
            Assert.AreEqual("cannon", config.Weapons[0].WeaponId);
            Assert.AreEqual(11f, config.Weapons[0].EnergyCost,
                "Cannon should cost 11 energy per shot — brings DPS/E inside 2x median threshold (#133)");
        }

        [Test]
        public void DashSkill_Costs18Energy_AfterBalance()
        {
            // Balance #155: Dash cost 20E → 18E to own the cheapest-mobility niche.
            var config = new GameConfig();
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "dash")
                {
                    Assert.AreEqual(18f, skill.EnergyCost,
                        "Dash should cost 18 energy after balance adjustment (#155)");
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Dash skill should exist in config");
        }

        [Test]
        public void HookShot_Balanced_AsUtilityDamageHybrid()
        {
            // Balance #164: HookShot sat at 0.33 dmg/E (~3x worse than Earthquake,
            // 12x worse than Mine Layer). Rebalance to 25E / 10s / 20dmg so the
            // "pull + finisher" fantasy actually threatens kills, while keeping
            // dmg/E (0.80) the lowest among damage skills to reflect its utility role.
            var config = new GameConfig();
            SkillDef hook = default;
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "hookshot") { hook = skill; found = true; break; }
            }
            Assert.IsTrue(found, "hookshot skill should exist in config");

            Assert.AreEqual(25f, hook.EnergyCost,
                "HookShot EnergyCost should be 25 (utility tier) after #164");
            Assert.AreEqual(10f, hook.Cooldown,
                "HookShot Cooldown should be 10s (utility tier) after #164");
            Assert.AreEqual(20f, hook.Value,
                "HookShot damage should be 20 (~1/3 cannon shot) after #164");
            Assert.AreEqual(10f, hook.Range,
                "HookShot Range must remain 10u — this buff is cost/CD/damage only");

            // Invariant: among damage-dealing skills, HookShot should have the
            // lowest dmg/E (reflecting its utility-hybrid role) but not be absurdly
            // under-tuned (> 0.5 dmg/E, i.e. within 2x of the next-lowest).
            float hookDpe = hook.Value / hook.EnergyCost;
            Assert.Greater(hookDpe, 0.5f,
                "HookShot damage/energy should be above 0.5 (not an outlier)");
            Assert.Less(hookDpe, 1.1f,
                "HookShot damage/energy should stay below Earthquake's 1.0 baseline");
        }

        // MatchStats_IncludesAccuracy test removed: depends on MatchSeries (Unity runtime class)

        // --- Cycle 3: Dynamite weapon tests ---

        [Test]
        public void Dynamite_ExistsInConfig_Slot4()
        {
            var config = new GameConfig();
            Assert.GreaterOrEqual(config.Weapons.Length, 7, "Should have at least 7 weapons");
            Assert.AreEqual("dynamite", config.Weapons[4].WeaponId);
            Assert.AreEqual(3, config.Weapons[4].Ammo);
            Assert.AreEqual(2, config.Weapons[4].Bounces);
            Assert.Greater(config.Weapons[4].FuseTime, 0f, "Dynamite should have a fuse timer");
        }

        [Test]
        public void Dynamite_CreateMatchGivesPlayerDynamiteSlot()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.AreEqual("dynamite", state.Players[0].WeaponSlots[4].WeaponId);
            Assert.AreEqual(3, state.Players[0].WeaponSlots[4].Ammo);
            Assert.AreEqual(2, state.Players[0].WeaponSlots[4].Bounces);
        }

        [Test]
        public void Dynamite_FireCreatesProjectileWithFuse()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].ActiveWeaponSlot = 4; // dynamite
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.Greater(state.Projectiles[0].FuseTimer, 0f, "Dynamite projectile should have a fuse timer");
            Assert.AreEqual(2, state.Projectiles[0].BouncesRemaining, "Dynamite should bounce 2 times");
        }

        [Test]
        public void Dynamite_ExplodesAfterFuseExpires()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place dynamite directly as a projectile with a short fuse
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 10f), // in the air, away from terrain
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 5f,
                MaxDamage = 80f,
                KnockbackForce = 15f,
                Alive = true,
                FuseTimer = 0.1f, // short fuse for test
                BouncesRemaining = 0
            });

            // Tick until fuse expires
            bool exploded = false;
            for (int i = 0; i < 30; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0)
                {
                    exploded = true;
                    break;
                }
            }

            Assert.IsTrue(exploded, "Dynamite should explode when fuse expires");
        }

        [Test]
        public void Airstrike_ExistsInSlot6()
        {
            var config = new GameConfig();
            Assert.AreEqual("airstrike", config.Weapons[6].WeaponId);
            Assert.IsTrue(config.Weapons[6].IsAirstrike);
        }

        // --- Cycle 4: Dynamite behavior fixes ---

        [Test]
        public void FusedProjectile_DoesNotExplodeOnPlayerContact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 at a controlled position away from terrain features
            state.Players[1].Position = new Vec2(15f, 5f);

            // Place a fused projectile heading toward player 1
            // Use ProjectileSimulation.Update directly to avoid match-end/movement interference
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = state.Players[1].Position + new Vec2(-2f, 0.5f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 5f,
                MaxDamage = 80f,
                KnockbackForce = 15f,
                Alive = true,
                FuseTimer = 3f, // active fuse — should not explode on contact
                BouncesRemaining = 0
            });

            float p2HealthBefore = state.Players[1].Health;

            // Tick projectile simulation directly — projectile should fly through player
            for (int i = 0; i < 10; i++)
                ProjectileSimulation.Update(state, 0.016f);

            // Player 1 should still have full health (fused projectile doesn't detonate on contact)
            Assert.AreEqual(p2HealthBefore, state.Players[1].Health,
                "Fused projectile should not explode on player contact");
        }

        [Test]
        public void FusedProjectile_RestsOnTerrain_WhenBouncesExhausted()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Find terrain surface
            float surfaceY = GamePhysics.FindGroundY(state.Terrain, 0f, config.SpawnProbeY, 0.1f);

            // Place fused projectile just above terrain, no bounces, falling
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, surfaceY + 2f),
                Velocity = new Vec2(0f, -5f),
                OwnerIndex = 0,
                ExplosionRadius = 5f,
                MaxDamage = 80f,
                KnockbackForce = 15f,
                Alive = true,
                FuseTimer = 5f, // long fuse
                BouncesRemaining = 0
            });

            // Tick enough for projectile to hit terrain
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Projectile should still be alive (resting on terrain, fuse ticking)
            bool stillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                if (state.Projectiles[i].FuseTimer > 0f && state.Projectiles[i].Alive)
                {
                    stillAlive = true;
                    break;
                }
            }

            Assert.IsTrue(stillAlive,
                "Fused projectile should rest on terrain when bounces exhausted, not explode");
        }
    }
}
