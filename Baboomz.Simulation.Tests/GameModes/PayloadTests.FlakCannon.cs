using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class PayloadTests
    {
        // ── Flak Cannon (#309) ──────────────────────────────────────

        [Test]
        public void FlakCannon_WeaponDefExists()
        {
            var config = new GameConfig();
            var flak = config.Weapons[21];
            Assert.AreEqual("flak_cannon", flak.WeaponId);
            Assert.IsTrue(flak.IsFlak, "Flak cannon should have IsFlak = true");
            Assert.AreEqual(8, flak.ClusterCount, "Flak should spawn 8 fragments");
            Assert.AreEqual(2, flak.Ammo, "Flak ammo should be 2");
            Assert.AreEqual(5f, flak.FlakMinDist);
            Assert.AreEqual(25f, flak.FlakMaxDist);
        }

        [Test]
        public void FlakCannon_BurstDistance_DerivedFromCharge()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Find flak cannon slot
            int flakSlot = -1;
            for (int i = 0; i < state.Players[0].WeaponSlots.Length; i++)
                if (state.Players[0].WeaponSlots[i].WeaponId == "flak_cannon") { flakSlot = i; break; }
            Assert.IsTrue(flakSlot >= 0, "Flak cannon should exist in weapon slots");

            state.Players[0].ActiveWeaponSlot = flakSlot;
            var weapon = state.Players[0].WeaponSlots[flakSlot];
            // Set AimPower to mid-range (halfway between min and max)
            state.Players[0].AimPower = (weapon.MinPower + weapon.MaxPower) / 2f;
            state.Players[0].AimAngle = 45f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count, "Should fire one projectile");
            var proj = state.Projectiles[0];
            Assert.IsTrue(proj.IsFlak, "Projectile should be flak");
            // Mid-charge should yield mid-range burst distance
            float expected = (weapon.FlakMinDist + weapon.FlakMaxDist) / 2f;
            Assert.AreEqual(expected, proj.FlakBurstDistance, 0.5f, "Burst distance should be ~midpoint at 50% charge");
        }

        [Test]
        public void FlakCannon_DetonatesMidAir_Spawns8Fragments()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Manually create a flak projectile high in the air so it won't hit terrain
            int flakId = state.NextProjectileId;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 0f), // horizontal
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 2f, // short burst distance for quick trigger
                LaunchPosition = new Vec2(0f, 15f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until the flak projectile detonates (identified by ID becoming dead/removed)
            for (int t = 0; t < 100; t++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                bool flakAlive = false;
                foreach (var p in state.Projectiles) if (p.Id == flakId && p.Alive) { flakAlive = true; break; }
                if (!flakAlive) break; // flak detonated
            }

            // Count alive fragment projectiles (SourceWeaponId == flak_cannon, not the original)
            int alive = 0;
            foreach (var p in state.Projectiles) if (p.Alive && p.SourceWeaponId == "flak_cannon") alive++;
            Assert.AreEqual(8, alive, "Should have exactly 8 alive fragment projectiles");
        }

        [Test]
        public void FlakCannon_FragmentsScatterDownward()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            int flakId = state.NextProjectileId;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 1f,
                LaunchPosition = new Vec2(0f, 15f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until the flak projectile detonates
            for (int t = 0; t < 60; t++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                bool flakAlive = false;
                foreach (var p in state.Projectiles) if (p.Id == flakId && p.Alive) { flakAlive = true; break; }
                if (!flakAlive) break;
            }

            // All flak fragments should have negative Y velocity (downward)
            int downward = 0;
            foreach (var p in state.Projectiles)
                if (p.Alive && p.SourceWeaponId == "flak_cannon" && p.Velocity.y < 0f) downward++;
            Assert.AreEqual(8, downward, "All 8 fragments should have downward velocity");
        }

        [Test]
        public void FlakCannon_EarlyDetonation_OnTerrainHit()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Fire flak straight down at terrain — should detonate early + spawn fragments
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 8f),
                Velocity = new Vec2(0f, -15f), // straight down
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 100f, // large distance — won't reach it, should hit terrain first
                LaunchPosition = new Vec2(0f, 8f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until terrain impact spawns fragments
            for (int t = 0; t < 120; t++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (state.Projectiles.Count > 1) break;
            }

            // Fragments should have been spawned
            int alive = 0;
            foreach (var p in state.Projectiles) if (p.Alive) alive++;
            Assert.GreaterOrEqual(alive, 1, "Flak hitting terrain should spawn fragments");
        }

        [Test]
        public void FlakCannon_FragmentsDealDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Place player 1 under a burst point
            state.Players[1].Position = new Vec2(0f, 3f);
            state.Players[1].Health = 100f;

            // Create a fragment directly above player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 4f),
                Velocity = new Vec2(0f, -10f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 2f,
                Alive = true,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until impact
            for (int t = 0; t < 60; t++)
                GameSimulation.Tick(state, 1f / 60f);

            Assert.Less(state.Players[1].Health, 100f, "Fragment should have dealt damage to player");
        }

        [Test]
        public void FlakCannon_FragmentsInheritParentMaxDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Simulate a flak projectile fired with Double Damage active (10f weapon * 2f multiplier = 20f)
            const float parentDamage = 20f;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = parentDamage,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 0.01f, // trivially small — detonate on first tick
                LaunchPosition = new Vec2(0f, 15f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Run ProjectileSimulation directly (bypasses AI). A few ticks are enough
            // to cross the tiny FlakBurstDistance.
            for (int t = 0; t < 5; t++)
                ProjectileSimulation.Update(state, 1f / 60f);

            // Every flak fragment (IsFlak=false, SourceWeaponId=flak_cannon) must inherit
            // parent.MaxDamage, not the hardcoded 15f that existed before the fix.
            int fragmentCount = 0;
            foreach (var p in state.Projectiles)
            {
                if (p.IsFlak) continue; // skip the parent flak projectile
                if (p.SourceWeaponId != "flak_cannon") continue; // skip unrelated projectiles
                fragmentCount++;
                Assert.AreEqual(parentDamage, p.MaxDamage, 0.001f,
                    "Fragment should inherit parent.MaxDamage (including DamageMultiplier buffs)");
            }
            Assert.Greater(fragmentCount, 0, "At least one flak fragment should have been spawned");
        }

        // ── Cluster/Flak spread regression (#346) ───────────────────

        static void InvokeSpawner(string methodName, GameState state, Vec2 origin, ProjectileState parent)
        {
            var method = typeof(ProjectileSimulation).GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method, $"Could not find {methodName} via reflection");
            method.Invoke(null, new object[] { state, origin, parent });
        }

        static System.Collections.Generic.List<float> CollectAnglesDegrees(
            System.Collections.Generic.IEnumerable<ProjectileState> projectiles)
        {
            var angles = new System.Collections.Generic.List<float>();
            foreach (var p in projectiles)
            {
                if (!p.Alive) continue;
                float deg = MathF.Atan2(p.Velocity.y, p.Velocity.x) * 180f / MathF.PI;
                if (deg < 0f) deg += 360f;
                angles.Add(deg);
            }
            angles.Sort();
            return angles;
        }

        [Test]
        public void FlakCannon_LastFragmentReaches330Endpoint_Issue346()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Projectiles.Clear();
            var parent = new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            };

            InvokeSpawner("SpawnFlakFragments", state, new Vec2(1f, 14.9f), parent);

            var angles = CollectAnglesDegrees(state.Projectiles);
            Assert.AreEqual(8, angles.Count, "Flak should spawn 8 fragments");

            Assert.GreaterOrEqual(angles[7], 323f,
                "Last flak fragment should reach the 330° endpoint; bug capped it at ~315°");
            Assert.LessOrEqual(angles[0], 217f,
                "First flak fragment should sit near the 210° start");
        }

        [Test]
        public void FlakCannon_BurstDistance_ClampedToMaxDist_WhenOvercharged()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            int flakSlot = -1;
            for (int i = 0; i < state.Players[0].WeaponSlots.Length; i++)
                if (state.Players[0].WeaponSlots[i].WeaponId == "flak_cannon") { flakSlot = i; break; }
            Assert.IsTrue(flakSlot >= 0, "Flak cannon should exist in weapon slots");

            var weapon = state.Players[0].WeaponSlots[flakSlot];
            state.Players[0].ActiveWeaponSlot = flakSlot;
            state.Players[0].AimAngle = 45f;
            // Set AimPower well above MaxPower to trigger the bug
            state.Players[0].AimPower = weapon.MaxPower * 2f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count, "Should fire one projectile");
            var proj = state.Projectiles[0];
            Assert.IsTrue(proj.IsFlak);
            Assert.LessOrEqual(proj.FlakBurstDistance, weapon.FlakMaxDist,
                "FlakBurstDistance must not exceed FlakMaxDist even when AimPower > MaxPower");
            Assert.GreaterOrEqual(proj.FlakBurstDistance, weapon.FlakMinDist,
                "FlakBurstDistance must not be below FlakMinDist");
        }

        [Test]
        public void ClusterBomb_LastSubProjectileReaches150Endpoint_Issue346()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 99);
            state.Projectiles.Clear();
            var parent = new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 2f),
                Velocity = new Vec2(0f, -5f), // pure downward to avoid X sign-flip
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 20f,
                KnockbackForce = 4f,
                Alive = true,
                ClusterCount = 8
            };

            InvokeSpawner("SpawnClusterBombs", state, new Vec2(0f, 2f), parent);

            var angles = CollectAnglesDegrees(state.Projectiles);
            Assert.AreEqual(8, angles.Count, "Cluster should spawn 8 sub-projectiles");

            Assert.GreaterOrEqual(angles[7], 143f,
                "Last cluster sub-projectile should reach the 150° endpoint; bug capped it at ~135°");
            Assert.LessOrEqual(angles[0], 37f,
                "First cluster sub-projectile should sit near the 30° start");
        }
    }
}
