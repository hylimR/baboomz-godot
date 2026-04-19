using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class LoadoutSelectionTests
    {
        [Test]
        public void DrillExpiry_TracksSourceWeaponId()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[1].Position = new Vec2(35f, state.Players[0].Position.y);
            state.Players[1].Health = 100f;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(state.Players[0].Position.x, state.Players[1].Position.y + 0.5f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 25f,
                KnockbackForce = 3f,
                Alive = true,
                IsDrill = true,
                SourceWeaponId = "drill"
            });

            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            bool hasHit = state.WeaponHits[0].ContainsKey("drill") && state.WeaponHits[0]["drill"] > 0;
            Assert.IsTrue(hasHit, "Drill expiry explosion should track SourceWeaponId in WeaponHits");
        }

        [Test]
        public void AI_LowEnergy_DoesNotSelectExpensiveWeapon()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(15f, 5f);
            state.Players[1].IsAI = true;
            state.Players[1].Energy = 12f;
            state.Players[1].MaxEnergy = 12f;
            state.Players[1].EnergyRegen = 0f;
            state.Players[1].SkillSlots = null;

            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[1].ShotsFired, 0,
                "AI should find and fire an affordable weapon when energy is low");
        }

        // --- Balance Cycle 19 regression tests (#204) ---

        [Test]
        public void BalanceCycle19_GustCannon_KnockbackReduced()
        {
            var config = new GameConfig();
            var gust = config.Weapons[19];
            Assert.AreEqual("gust_cannon", gust.WeaponId);
            Assert.AreEqual(20f, gust.KnockbackForce, "Gust Cannon KB should be 20 (reduced from 30)");
            Assert.AreEqual(3f, gust.ShootCooldown, "Gust Cannon cooldown should be 3s (increased from 2.5s)");
        }

        [Test]
        public void BalanceCycle19_GravityBomb_Buffed()
        {
            var config = new GameConfig();
            var gb = config.Weapons[16];
            Assert.AreEqual("gravity_bomb", gb.WeaponId);
            Assert.AreEqual(2, gb.Ammo, "Gravity Bomb ammo should be 2 (buffed from 1)");
            Assert.AreEqual(25f, gb.EnergyCost, "Gravity Bomb energy should be 25 (reduced from 30)");
        }

        [Test]
        public void BalanceCycle19_Decoy_Buffed()
        {
            var config = new GameConfig();
            SkillDef decoy = default;
            foreach (var s in config.Skills)
            {
                if (s.SkillId == "decoy") { decoy = s; break; }
            }
            Assert.AreEqual("decoy", decoy.SkillId);
            Assert.AreEqual(30f, decoy.Value, "Decoy HP should be 30 (buffed from 1)");
            Assert.AreEqual(4f, decoy.Duration, "Decoy duration should be 4s (buffed from 2s)");
            Assert.AreEqual(30f, decoy.EnergyCost, "Decoy energy should be 30 (reduced from 35)");
        }

        // --- AI weapon selection for slots 17-19 (regression for #222) ---

        [Test]
        public void AI_SelectsRicochetDisc_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 17) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 17) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select ricochet disc (slot 17) at medium range");
        }

        [Test]
        public void AI_SelectsMagmaBall_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 18) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(15f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 18) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select magma ball (slot 18) at medium range");
        }

        [Test]
        public void AIPickLoadout_CanIncludePetrify()
        {
            var config = SmallConfig();
            config.AIDifficultyLevel = 1;
            bool hasPetrify = false;
            for (int seed = 0; seed < 500; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                if (loadout[0] == 19 || loadout[1] == 19) { hasPetrify = true; break; }
            }
            Assert.IsTrue(hasPetrify, "Petrify (index 19) should appear in AI loadouts on Normal difficulty");
        }

        [Test]
        public void AI_SelectsGustCannon_AtCloseRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 19) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(8f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 19) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select gust cannon (slot 19) at close range");
        }
    }
}
