using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {

        [Test]
        public void GustCannon_DoesNotEmitExplosionEvent()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            int explosionsBefore = state.ExplosionEvents.Count;
            CombatResolver.ApplyWindBlast(state, new Vec2(0f, 15f), 4f, 30f, 0);

            Assert.AreEqual(explosionsBefore, state.ExplosionEvents.Count, "Wind blast should not emit ExplosionEvent");
        }

        [Test]
        public void GustCannon_DoesNotKnockbackOwner()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 15f);
            state.Players[0].Velocity = Vec2.Zero;

            CombatResolver.ApplyWindBlast(state, new Vec2(0.5f, 15f), 4f, 30f, 0);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f, "Owner should not be knocked back");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.01f, "Owner should not be knocked back");
        }

        [Test]
        public void GustCannon_KnockbackFallsOffWithDistance()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(-5f, 15f);
            state.Players[1].Position = new Vec2(1f, 15f);
            Vec2 blastPos = new Vec2(0f, 15f);

            CombatResolver.ApplyWindBlast(state, blastPos, 4f, 30f, 0);
            float nearKnockback = state.Players[1].Velocity.x;

            state.Players[1].Velocity = Vec2.Zero;
            state.Players[1].Position = new Vec2(3.5f, 15f);
            CombatResolver.ApplyWindBlast(state, blastPos, 4f, 30f, 0);
            float farKnockback = state.Players[1].Velocity.x;

            Assert.Greater(nearKnockback, farKnockback, "Knockback should fall off with distance");
        }

        [Test]
        public void GustCannon_NonArmsRace_DoesNotTrackDirectHit()
        {
            // Regression: gust cannon deals 0 damage outside ArmsRace and should
            // not inflate DirectHits or trigger combo events (see issue #342)
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            int hitsBefore = state.Players[0].DirectHits;
            int comboBefore = state.Players[0].ConsecutiveHits;

            CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.AreEqual(hitsBefore, state.Players[0].DirectHits,
                "Zero-damage gust should not increment DirectHits");
            Assert.AreEqual(comboBefore, state.Players[0].ConsecutiveHits,
                "Zero-damage gust should not increment ConsecutiveHits");
        }

        [Test]
        public void GustCannon_NonArmsRace_DoesNotInflateComboStreak()
        {
            // Regression: spamming gust cannon (zero damage) should not build combo
            // multipliers (DoubleHit, TripleHit, Unstoppable) — issue #342
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            state.ComboEvents.Clear();

            // Fire wind blast 5 times — enough to reach Unstoppable if bug existed
            for (int i = 0; i < 5; i++)
                CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.AreEqual(0, state.Players[0].ConsecutiveHits,
                "Zero-damage gust spam must not build a combo streak");
            Assert.AreEqual(0, state.ComboEvents.Count,
                "Zero-damage gust spam must not emit any ComboEvent");
        }

        [Test]
        public void GustCannon_ArmsRace_TracksDirectHit()
        {
            // In ArmsRace the gust deals minimum damage, so it should still track
            var config = SmallConfig();
            config.MatchType = MatchType.ArmsRace;
            config.ArmsRaceGustMinDamage = 1f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            int hitsBefore = state.Players[0].DirectHits;

            CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.Greater(state.Players[0].DirectHits, hitsBefore,
                "ArmsRace gust deals minimum damage and should track direct hit");
        }

        [Test]
        public void SpawnCrate_UsesEnumValues_NotMagicNumber()
        {
            // Verify crate type count equals the number of actual crate types
            int expectedCount = 4; // Health, Energy, AmmoRefill, DoubleDamage
            int actualCount = System.Enum.GetValues(typeof(CrateType)).Length;
            Assert.AreEqual(expectedCount, actualCount,
                "CrateType enum must have exactly the expected number of real crate types");

            // Verify spawned crates always have a valid type
            var config = SmallConfig();
            config.CrateSpawnInterval = 1f;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Tick until several crates spawn
            for (int i = 0; i < 500; i++)
                GameSimulation.Tick(state, 0.1f);

            // Force many spawns with different seeds to cover RNG range
            for (int seed = 0; seed < 100; seed++)
            {
                state.NextCrateSpawnTime = 0.001f;
                state.Time = 0.002f;
                state.Seed = seed;
                GameSimulation.Tick(state, 0.016f);
            }

            foreach (var crate in state.Crates)
            {
                Assert.That((int)crate.Type, Is.GreaterThanOrEqualTo(0),
                    "Crate type must be non-negative");
                Assert.That((int)crate.Type, Is.LessThan(actualCount),
                    $"Crate type {crate.Type} must be less than type count ({actualCount})");
            }
        }

        [Test]
        public void Shielder_DoesNotFire_WhileShootCooldownActive()
        {
            // Regression: #274 — Shielder fired without checking ShootCooldownRemaining
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up: player 0 = human target, player 1 = shielder mob in melee range
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsAI = false;

            state.Players[1].Position = new Vec2(1f, 5f); // within 2.5 units
            state.Players[1].IsAI = true;
            state.Players[1].IsMob = true;
            state.Players[1].MobType = "shielder";
            state.Players[1].Health = 50f;
            state.Players[1].MaxHealth = 50f;
            state.Players[1].ShootCooldownRemaining = 5f; // cooldown active
            state.Players[1].ShotsFired = 0;

            AILogic.Reset(42, 2);

            // Tick several frames — shielder should NOT fire while cooldown is active
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Players[1].ShotsFired,
                "Shielder should not fire while ShootCooldownRemaining > 0");
        }

        [Test]
        public void HealerMob_MovesTowardMostWoundedAlly_NotNearest()
        {
            // Regression test for #299: healer moved toward nearest ally, not most-wounded
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            float safeY = state.Players[0].Position.y;

            WeaponSlotState safeWeapon() => new WeaponSlotState
            {
                WeaponId = "cannon", Ammo = -1,
                MinPower = 5f, MaxPower = 30f, ShootCooldown = 999f
            };

            // [0] human player far away (no threat in range, so healer enters move-to-ally branch)
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[0].Position = new Vec2(5f, safeY);

            // [1] healer mob
            float healerX = 50f;
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

            // [2] healthy ally NEARBY (3 units right of healer, 95% HP)
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

            // [3] critically wounded ally FAR (15 units LEFT of healer, 10% HP)
            float mob3X = healerX - 15f;
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

            float healerStartX = state.Players[1].Position.x;

            // Tick a few frames
            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            // Healer should move LEFT toward the critically wounded ally at mob3X,
            // not RIGHT toward the nearby healthy ally at mob2X
            Assert.Less(state.Players[1].Position.x, healerStartX,
                "Healer should move toward most-wounded ally (left), not nearest healthy ally (right)");
        }
    }
}
