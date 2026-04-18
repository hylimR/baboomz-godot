using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class PayloadTests
    {
        // --- Harpoon weapon tests (issue #308) ---

        [Test]
        public void Harpoon_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 21, "Should have at least 21 weapons");
            Assert.AreEqual("harpoon", config.Weapons[20].WeaponId);
            Assert.AreEqual(40f, config.Weapons[20].MaxDamage);
            Assert.AreEqual(1.0f, config.Weapons[20].ExplosionRadius);
            Assert.AreEqual(3.5f, config.Weapons[20].ShootCooldown);
            Assert.AreEqual(3, config.Weapons[20].Ammo);
            Assert.AreEqual(20f, config.Weapons[20].EnergyCost);
            Assert.IsTrue(config.Weapons[20].IsPiercing);
            Assert.AreEqual(1, config.Weapons[20].MaxPierceCount);
        }

        [Test]
        public void Harpoon_CreatesProjectileWithPiercingFlag()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].ActiveWeaponSlot = 20;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsPiercing, "Projectile should have piercing flag");
            Assert.AreEqual(1, state.Projectiles[0].MaxPierceCount);
            Assert.AreEqual(0, state.Projectiles[0].PierceCount);
            Assert.AreEqual(-1, state.Projectiles[0].LastPiercedPlayerId);
        }

        [Test]
        public void Harpoon_PiercesThroughFirstPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place first target at (3, 5) and second at (6, 5)
            state.Players[1].Position = new Vec2(3f, 5f);

            // Create harpoon heading right at player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            float healthBefore = state.Players[1].Health;

            // Tick a few frames so the projectile reaches player 1
            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].PierceCount > 0)
                    break;
            }

            Assert.Less(state.Players[1].Health, healthBefore, "First target should take damage");
            Assert.IsTrue(state.Projectiles.Count > 0 && state.Projectiles[0].Alive,
                "Harpoon should still be alive after piercing first target");
            Assert.AreEqual(1, state.Projectiles[0].PierceCount,
                "Pierce count should be 1 after passing through first target");
        }

        [Test]
        public void Harpoon_ExplodesWhenPierceCountExhausted()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player 1 directly in path
            state.Players[1].Position = new Vec2(3f, 5f);

            // Harpoon that already used its pierce allowance, targeting player 1
            // LastPiercedPlayerId = 0 (not player 1) so it can still collide with player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                PierceCount = 1,
                LastPiercedPlayerId = 0,
                SourceWeaponId = "harpoon"
            });

            float healthBefore = state.Players[1].Health;

            for (int i = 0; i < 30; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.Less(state.Players[1].Health, healthBefore,
                "Target should take damage from harpoon explosion");
            Assert.AreEqual(0, state.Projectiles.Count,
                "Harpoon should be destroyed when pierce count exhausted");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Explosion event should fire when pierce count exhausted");
        }

        [Test]
        public void Harpoon_ExplodesOnTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Create harpoon heading into terrain
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 2f),
                Velocity = new Vec2(0f, -20f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            // Tick until it hits terrain
            for (int i = 0; i < 60; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.AreEqual(0, state.Projectiles.Count,
                "Harpoon should be destroyed on terrain hit");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Explosion event should fire on terrain hit");
        }

        [Test]
        public void Harpoon_ShieldBlocksPierce()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Give player 1 a shield
            state.Players[1].Position = new Vec2(3f, 5f);
            state.Players[1].ShieldHP = 100f;
            state.Players[1].MaxShieldHP = 100f;
            state.Players[1].FacingDirection = -1; // facing left (toward projectile)

            // Create harpoon heading right at shielded player
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            // Tick until collision
            for (int i = 0; i < 30; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.AreEqual(0, state.Projectiles.Count,
                "Harpoon should stop on shielded target (not pierce through)");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Harpoon should explode on shielded target");
        }

        [Test]
        public void Harpoon_NoPierceDamageDoesNotCreateExplosionEvent()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(3f, 5f);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            int explosionsBefore = state.ExplosionEvents.Count;

            // Tick until pierce
            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].PierceCount > 0)
                    break;
            }

            Assert.AreEqual(explosionsBefore, state.ExplosionEvents.Count,
                "Pierce damage should NOT create an explosion event");
            Assert.IsTrue(state.DamageEvents.Count > 0,
                "Pierce damage should create a damage event");
        }

        [Test]
        public void Harpoon_PierceDamageTracksWeaponMastery()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(3f, 5f);
            float healthBefore = state.Players[1].Health;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].PierceCount > 0)
                    break;
            }

            Assert.IsTrue(state.WeaponHits[0].ContainsKey("harpoon"),
                "Pierce hit should be tracked in WeaponHits with SourceWeaponId");
        }

        [Test]
        public void Harpoon_PierceFrame_StillChecksWaterBounds()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place player 1 just above water level so harpoon hits them on the way down
            float waterY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel);
            state.Players[1].Position = new Vec2(5f, waterY + 1f);
            state.Players[1].Health = 100f;

            // Create a piercing projectile heading straight into player, then water
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(5f, waterY + 2f),
                Velocity = new Vec2(0f, -30f), // fast downward
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            // Tick enough for the projectile to pierce player 1 and hit water
            for (int i = 0; i < 20; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }
            // One more update to clean up dead projectiles
            if (state.Projectiles.Count > 0)
                ProjectileSimulation.Update(state, 0.02f);

            Assert.AreEqual(0, state.Projectiles.Count,
                "Piercing projectile should die to water/bounds after piercing a player, not fly through forever");
        }
    }
}
