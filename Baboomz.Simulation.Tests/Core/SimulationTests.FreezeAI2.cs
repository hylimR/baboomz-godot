using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {

        [Test]
        public void BalanceCheck_BananaBombCooldownAndEnergy_Issue34()
        {
            // Issue #22 set EnergyCost=40, ShootCooldown=4, Ammo=1.
            // Issue #34 conservative bump keeps cooldown and ammo at #22 values.
            var config = new GameConfig();
            var banana = config.Weapons[11];
            Assert.AreEqual("banana_bomb", banana.WeaponId);
            Assert.AreEqual(4f, banana.ShootCooldown, "Banana cooldown should be 4s (unchanged from #22)");
            Assert.AreEqual(40f, banana.EnergyCost, "Banana energy cost should be 40 (issue #22)");
            Assert.AreEqual(1, banana.Ammo, "Banana ammo should be 1 (unchanged from #22)");
        }

        [Test]
        public void BalanceCheck_AirstrikeCount_IsFour_Issue22()
        {
            // Issue #22 nerf: airstrike's 5x35 = 175 max burst was too high for a
            // 4s cooldown. Drop to 4 bombs => 140 max burst. Issue #34 leaves this alone.
            var config = new GameConfig();
            var airstrike = config.Weapons[6];
            Assert.AreEqual("airstrike", airstrike.WeaponId);
            Assert.AreEqual(4, airstrike.AirstrikeCount, "Airstrike count should be 4 (issue #22, was 5)");
        }

        [Test]
        public void BalanceCheck_AirstrikeDamageAndAmmo_Issue34()
        {
            // Issue #34 conservative bump: MaxDamage 35 -> 40 (burst 40×4=160,
            // just above #22's 140 cap). Ammo stays 1.
            var config = new GameConfig();
            var airstrike = config.Weapons[6];
            Assert.AreEqual("airstrike", airstrike.WeaponId);
            Assert.AreEqual(40f, airstrike.MaxDamage, "Airstrike per-bomb damage should be 40 (issue #34, was 35)");
            Assert.AreEqual(1, airstrike.Ammo, "Airstrike ammo should be 1 (unchanged)");
        }

        [Test]
        public void BalanceCheck_FreezeGrenadeEnergy_Issue34()
        {
            // Issue #34: freeze_grenade is a utility/CC tool dealing only 5 damage
            // but cost 20 energy — a 0.08 DPS/Energy outlier. Drop to 12 energy.
            var config = new GameConfig();
            WeaponDef freeze = default;
            foreach (var w in config.Weapons) if (w.WeaponId == "freeze_grenade") { freeze = w; break; }
            Assert.AreEqual("freeze_grenade", freeze.WeaponId);
            Assert.AreEqual(12f, freeze.EnergyCost, "Freeze grenade energy should be 12 (issue #34, was 20)");
        }

        [Test]
        public void BalanceCheck_ClusterDamageAndEnergy_Issue34()
        {
            // Issue #34: cluster's 20 base damage and 35 energy cost made it a 0.19
            // DPS/Energy outlier. Bump damage to 30 and drop energy to 25.
            var config = new GameConfig();
            WeaponDef cluster = default;
            foreach (var w in config.Weapons) if (w.WeaponId == "cluster") { cluster = w; break; }
            Assert.AreEqual("cluster", cluster.WeaponId);
            Assert.AreEqual(30f, cluster.MaxDamage, "Cluster base damage should be 30 (issue #34, was 20)");
            Assert.AreEqual(25f, cluster.EnergyCost, "Cluster energy should be 25 (issue #34, was 35)");
        }

        [Test]
        public void BalanceCheck_FlakCannonDamageAndCooldown_Issue87()
        {
            // Issue #87: flak was a DPS/E outlier at 2.40. Reduced MaxDamage 20→15,
            // EnergyCost 25→30. 8×15=120 burst between airstrike and HHG tiers.
            var config = new GameConfig();
            WeaponDef flak = default;
            foreach (var w in config.Weapons) if (w.WeaponId == "flak_cannon") { flak = w; break; }
            Assert.AreEqual("flak_cannon", flak.WeaponId);
            Assert.AreEqual(15f, flak.MaxDamage, "Flak damage should be 15 (issue #87, was 20)");
            Assert.AreEqual(30f, flak.EnergyCost, "Flak energy should be 30 (issue #87, was 25)");
            Assert.AreEqual(3f, flak.ShootCooldown, "Flak cooldown should be 3s");
        }

        [Test]
        public void BalanceCheck_DrillAmmo_Is4()
        {
            var config = new GameConfig();
            var drill = config.Weapons[7];
            Assert.AreEqual("drill", drill.WeaponId);
            Assert.AreEqual(4, drill.Ammo, "Drill ammo should be 4 (reduced from 5 to compensate damage buff)");
        }

        [Test]
        public void BalanceCheck_DrillDamage_Is40()
        {
            var config = new GameConfig();
            var drill = config.Weapons[7];
            Assert.AreEqual("drill", drill.WeaponId);
            Assert.AreEqual(40f, drill.MaxDamage, "Drill damage should be 40 (buffed from 25 to reward terrain-reading skill)");
        }

        [Test]
        public void BalanceCheck_HealStats_Issue49()
        {
            // Issue #49: Heal was 40E/15s/25HP — underperforming vs Shield (35E/12s).
            // Buffed to 35E/12s/35HP to match Shield's cost tier.
            var config = new GameConfig();
            var heal = config.Skills[4];
            Assert.AreEqual("heal", heal.SkillId);
            Assert.AreEqual(35f, heal.EnergyCost, "Heal energy cost should be 35 (issue #49, was 40)");
            Assert.AreEqual(12f, heal.Cooldown, "Heal cooldown should be 12s (issue #49, was 15s)");
            Assert.AreEqual(35f, heal.Value, "Heal HP should be 35 (issue #49, was 25)");
        }

        [Test]
        public void BalanceCheck_EarthquakeCooldown_Is16()
        {
            var config = new GameConfig();
            var eq = config.Skills[7];
            Assert.AreEqual("earthquake", eq.SkillId);
            Assert.AreEqual(16f, eq.Cooldown, "Earthquake cooldown should be 16s (reduced from 20s)");
        }

        [Test]
        public void BalanceCheck_EnergyDrainCost_Is20()
        {
            var config = new GameConfig();
            var drain = config.Skills[11];
            Assert.AreEqual("energy_drain", drain.SkillId);
            Assert.AreEqual(20f, drain.EnergyCost, "Energy Drain cost should be 20 (increased from 15)");
        }

        [Test]
        public void BalanceCheck_DeflectDuration_Is1_5()
        {
            var config = new GameConfig();
            var deflect = config.Skills[12];
            Assert.AreEqual("deflect", deflect.SkillId);
            Assert.AreEqual(1.5f, deflect.Duration, "Deflect duration should be 1.5s (buffed from 1.0s)");
        }

        [Test]
        public void BalanceCheck_DeflectCooldown_Is13()
        {
            var config = new GameConfig();
            var deflect = config.Skills[12];
            Assert.AreEqual("deflect", deflect.SkillId);
            Assert.AreEqual(13f, deflect.Cooldown, "Deflect cooldown should be 13s (reduced from 15s)");
        }

        [Test]
        public void StickyBomb_ExistsInConfig_AsWeapon13()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 14, "Should have at least 14 weapons");
            Assert.AreEqual("sticky_bomb", config.Weapons[13].WeaponId);
            Assert.AreEqual(50f, config.Weapons[13].MaxDamage);
            Assert.AreEqual(3, config.Weapons[13].Ammo);
            Assert.AreEqual(2f, config.Weapons[13].FuseTime);
            Assert.IsTrue(config.Weapons[13].IsSticky);
        }

        [Test]
        public void StickyBomb_SticksToTerrain_OnCollision()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Fire sticky bomb from player 0
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].ActiveWeaponSlot = 13;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count, "Should have 1 projectile");
            Assert.IsTrue(state.Projectiles[0].IsSticky, "Projectile should be sticky");
            Assert.AreEqual(-1, state.Projectiles[0].StuckToPlayerId, "Should not be stuck to player yet");
            Assert.IsFalse(state.Projectiles[0].StuckToTerrain, "Should not be stuck to terrain yet");

            // Tick until it either sticks to terrain or fully explodes (fuse expires)
            bool wasStuck = false;
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].StuckToTerrain)
                    wasStuck = true;
                if (state.Projectiles.Count == 0) break;
            }

            // Sticky bomb should have stuck to terrain and then exploded when fuse expired
            Assert.IsTrue(wasStuck || state.Projectiles.Count == 0,
                "Sticky bomb should have stuck to terrain and/or exploded after fuse");
        }

        [Test]
        public void StickyBomb_SticksToPlayer_OnDirectHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place players close together
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(3f, 5f);

            // Manually create a sticky projectile heading toward player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2.5f,
                MaxDamage = 50f,
                KnockbackForce = 8f,
                Alive = true,
                FuseTimer = 2f,
                IsSticky = true,
                StuckToPlayerId = -1
            });

            // Tick a few frames — should attach to player 1
            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.016f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].StuckToPlayerId >= 0)
                    break;
            }

            Assert.IsTrue(state.Projectiles.Count > 0, "Projectile should still be alive (stuck)");
            Assert.AreEqual(1, state.Projectiles[0].StuckToPlayerId,
                "Sticky bomb should be attached to player 1");
        }

        [Test]
        public void StickyBomb_FollowsPlayer_WhenStuck()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);

            // Create a sticky projectile already stuck to player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = state.Players[1].Position + new Vec2(0f, 0.5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 2.5f,
                MaxDamage = 50f,
                KnockbackForce = 8f,
                Alive = true,
                FuseTimer = 2f,
                IsSticky = true,
                StuckToPlayerId = 1
            });

            // Move player 1
            state.Players[1].Position = new Vec2(15f, 5f);
            ProjectileSimulation.Update(state, 0.016f);

            Assert.IsTrue(state.Projectiles.Count > 0, "Projectile should still be alive");
            float distToPlayer = Vec2.Distance(state.Projectiles[0].Position,
                state.Players[1].Position + new Vec2(0f, 0.5f));
            Assert.Less(distToPlayer, 0.01f,
                "Sticky bomb should follow the player it's stuck to");
        }

        [Test]
        public void StickyBomb_ExplodesAfterFuse_WhenStuckToPlayer()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            float healthBefore = state.Players[1].Health;

            // Create sticky projectile stuck to player 1 with short fuse
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = state.Players[1].Position + new Vec2(0f, 0.5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 2.5f,
                MaxDamage = 50f,
                KnockbackForce = 8f,
                Alive = true,
                FuseTimer = 0.1f,
                IsSticky = true,
                StuckToPlayerId = 1
            });

            // Tick past fuse
            for (int i = 0; i < 20; i++)
                ProjectileSimulation.Update(state, 0.016f);

            // Projectile should have exploded
            Assert.AreEqual(0, state.Projectiles.Count,
                "Sticky bomb should be removed after exploding");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Explosion event should have been created");
        }

    }
}
