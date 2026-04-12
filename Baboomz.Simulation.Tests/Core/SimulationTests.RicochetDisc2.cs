using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {

        [Test]
        public void AI_SelectsLightningRod_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 14) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 14) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select lightning rod (slot 14) at medium range");
        }

        [Test]
        public void AI_SelectsBoomerang_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 15) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 15) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select boomerang (slot 15) at medium range");
        }

        [Test]
        public void AI_SelectsGravityBomb_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 16) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 16) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select gravity bomb (slot 16) at medium range");
        }

        [Test]
        public void AI_SkipsFreezeGrenade_WhenTargetAlreadyFrozen()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 12) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].FreezeTimer = 10f;
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 3000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 12) { selected = true; break; }
                if (state.Players[0].FreezeTimer <= 0f) state.Players[0].FreezeTimer = 10f;
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsFalse(selected, "AI should not select freeze grenade when target is already frozen");
        }

        [Test]
        public void Bomber_RepositionsLaterally_WhenInPreferredRange()
        {
            // Regression: #148 — Bomber reposition timer set velocity to 0 in both branches,
            // making the Bomber a stationary turret once it reached preferred range.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Player 0 is the human target at origin
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsAI = false;

            // Replace player 1 with a Bomber mob at distance 12 (within 8-16 preferred range)
            state.Players[1] = new PlayerState
            {
                Position = new Vec2(12f, 5f),
                Health = 50f,
                MaxHealth = 50f,
                MoveSpeed = 3f,
                IsAI = true,
                IsMob = true,
                MobType = "bomber",
                FacingDirection = -1,
                Name = "Bomber",
                WeaponSlots = new[] { new WeaponSlotState
                {
                    WeaponId = "cannon",
                    Ammo = -1,
                    MinPower = 5f,
                    MaxPower = 30f,
                    ShootCooldown = 2f
                }},
                ShootCooldownRemaining = 999f // prevent firing during test
            };

            AILogic.Reset(42, 2);

            // Tick enough frames for the reposition timer to fire and bomber to move
            bool movedLaterally = false;
            for (int frame = 0; frame < 600; frame++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (MathF.Abs(state.Players[1].Velocity.x) > 0.01f)
                {
                    movedLaterally = true;
                    break;
                }
            }

            Assert.IsTrue(movedLaterally,
                "Bomber should reposition laterally when within preferred range (8-16 units)");
        }

        [Test]
        public void HealerMob_HealsAllyWithLowestHpRatio_NotFirstByIndex()
        {
            // Regression test for #149: healer healed by array index, not damage need
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Use terrain-safe spawn Y from the generated match
            float safeY = state.Players[0].Position.y;

            // 4 players: [0] human (far away), [1] healer, [2] barely scratched, [3] critically wounded
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[0].Position = new Vec2(config.Player1SpawnX, safeY);

            // Shared weapon slot with very high cooldown to prevent any firing
            WeaponSlotState safeWeapon() => new WeaponSlotState
            {
                WeaponId = "cannon", Ammo = -1,
                MinPower = 5f, MaxPower = 30f, ShootCooldown = 999f
            };

            // Healer mob near spawn2 position (known safe terrain)
            float healerX = config.Player2SpawnX;
            float healerY = GamePhysics.FindGroundY(state.Terrain, healerX, config.SpawnProbeY);
            players[1] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "healer",
                Health = 100f, MaxHealth = 100f, MoveSpeed = 3f,
                Position = new Vec2(healerX, healerY),
                IsGrounded = true, FacingDirection = 1, Name = "Healer",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // Mob at index 2: 95% HP (barely scratched), 3 units from healer
            float mob2X = healerX + 3f;
            float mob2Y = GamePhysics.FindGroundY(state.Terrain, mob2X, config.SpawnProbeY);
            players[2] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 95f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(mob2X, mob2Y),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber1",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // Mob at index 3: 10% HP (critically wounded), 5 units from healer
            float mob3X = healerX + 5f;
            float mob3Y = GamePhysics.FindGroundY(state.Terrain, mob3X, config.SpawnProbeY);
            players[3] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 10f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(mob3X, mob3Y),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber2",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            state.Players = players;
            AILogic.Reset(42, 4);
            BossLogic.Reset(42, 4);

            float healthBefore2 = state.Players[2].Health;
            float healthBefore3 = state.Players[3].Health;

            // Tick a few frames so healer AI runs (small dt to avoid physics side-effects)
            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            // The critically wounded mob (index 3) should be healed, not the barely-scratched one
            Assert.Greater(state.Players[3].Health, healthBefore3,
                "Healer should heal the critically wounded ally (index 3, 10% HP)");
            Assert.AreEqual(healthBefore2, state.Players[2].Health, 0.01f,
                "Barely-scratched ally (index 2, 95% HP) should NOT be healed first");
        }

    }
}
