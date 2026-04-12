using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Ricochet Disc weapon tests ---

        [Test]
        public void RicochetDisc_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 18, "Should have at least 18 weapons");
            Assert.AreEqual("ricochet_disc", config.Weapons[17].WeaponId);
            Assert.AreEqual(28f, config.Weapons[17].MaxDamage);
            Assert.AreEqual(-1, config.Weapons[17].Ammo); // infinite
            Assert.IsTrue(config.Weapons[17].IsRicochet);
            Assert.AreEqual(3, config.Weapons[17].Bounces);
            Assert.AreEqual(1.5f, config.Weapons[17].ExplosionRadius, 0.01f);
            Assert.AreEqual(15f, config.Weapons[17].EnergyCost);
        }

        [Test]
        public void RicochetDisc_CreatesProjectileWithRicochetFlag()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].ActiveWeaponSlot = 17; // ricochet_disc
            state.Players[0].Energy = 100f;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsRicochet, "Projectile should have ricochet flag");
            Assert.AreEqual(3, state.Projectiles[0].BouncesRemaining);
        }

        [Test]
        public void RicochetDisc_BouncesWithDamageOnTerrainHit()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Projectiles.Add(new ProjectileState
            {
                Id = 1, Alive = true,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(10f, -10f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f, MaxDamage = 25f,
                KnockbackForce = 3f,
                BouncesRemaining = 3,
                IsRicochet = true,
                StuckToPlayerId = -1
            });

            // Tick until terrain collision (max 120 frames)
            bool sawExplosion = false;
            for (int i = 0; i < 120; i++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (state.ExplosionEvents.Count > 0) sawExplosion = true;
                if (sawExplosion) break;
            }

            Assert.IsTrue(sawExplosion,
                "Ricochet should emit explosion on bounce");
            // Projectile should still be alive after first bounce (2 remaining)
            bool stillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
                if (state.Projectiles[i].IsRicochet && state.Projectiles[i].Alive)
                    stillAlive = true;
            Assert.IsTrue(stillAlive, "Ricochet disc should survive after bounce");
        }

        [Test]
        public void RicochetDisc_DirectPlayerHitExplodesAndDies()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place player 1 directly in front of the disc
            state.Players[1].Position = new Vec2(2f, 5f);
            state.Players[1].Health = 100f;

            state.Projectiles.Add(new ProjectileState
            {
                Id = 1, Alive = true,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f, MaxDamage = 25f,
                KnockbackForce = 3f,
                BouncesRemaining = 3,
                IsRicochet = true,
                StuckToPlayerId = -1
            });

            for (int i = 0; i < 60; i++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.Less(state.Players[1].Health, 100f,
                "Player should take damage from direct ricochet disc hit");
        }

        [Test]
        public void Reflect_ReflectsVelocityAroundNormal()
        {
            // Velocity going down-right, horizontal surface normal pointing up
            Vec2 vel = new Vec2(5f, -5f);
            Vec2 normal = new Vec2(0f, 1f);
            Vec2 result = GamePhysics.Reflect(vel, normal);
            Assert.AreEqual(5f, result.x, 0.01f, "X should stay the same");
            Assert.AreEqual(5f, result.y, 0.01f, "Y should flip");
        }

        [Test]
        public void MultipleFlyers_OrbitDifferentPositions()
        {
            // Regression: #135 — multiple Flyer mobs all computed the same orbit
            // position because the Sin phase had no per-mob offset.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Expand Players to include 2 Flyer mobs (indices 2 and 3)
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[1].IsAI = false; // disable normal AI so only flyers act

            for (int i = 2; i <= 3; i++)
            {
                players[i] = new PlayerState
                {
                    Position = new Vec2(0f, 10f),
                    Health = 50f,
                    MaxHealth = 50f,
                    MoveSpeed = 4f,
                    IsAI = true,
                    IsMob = true,
                    MobType = "flyer",
                    FacingDirection = 1,
                    Name = $"Flyer{i - 1}",
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
            }
            state.Players = players;

            AILogic.Reset(42, 4);

            // Tick enough frames for orbits to diverge
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            float x2 = state.Players[2].Position.x;
            float x3 = state.Players[3].Position.x;
            float separation = MathF.Abs(x2 - x3);

            Assert.Greater(separation, 0.5f,
                "Two Flyer mobs should NOT orbit at the same X position");
        }

        [Test]
        public void AILogic_Reset_SizesArraysToPlayerCount_Over16()
        {
            // Regression test for #136: arrays were fixed at 16, crashing with >16 players
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Extend Players to 18 to simulate PVE with many mobs
            int totalPlayers = 18;
            var extended = new PlayerState[totalPlayers];
            System.Array.Copy(state.Players, extended, state.Players.Length);
            for (int i = state.Players.Length; i < totalPlayers; i++)
            {
                extended[i] = new PlayerState
                {
                    IsAI = true,
                    IsMob = true,
                    MobType = "bomber",
                    Health = 50f,
                    MaxHealth = 50f,
                    MoveSpeed = 3f,
                    Position = new Vec2(-10f + i * 2f, 5f),
                    WeaponSlots = new[] { new WeaponSlotState
                    {
                        WeaponId = "cannon", Ammo = -1,
                        MinPower = 5f, MaxPower = 30f,
                        ShootCooldown = 1f, ExplosionRadius = 2f,
                        MaxDamage = 20f, KnockbackForce = 3f
                    }}
                };
            }
            state.Players = extended;

            AILogic.Reset(42, totalPlayers);
            BossLogic.Reset(42, totalPlayers);

            Assert.DoesNotThrow(() =>
            {
                for (int frame = 0; frame < 120; frame++)
                    GameSimulation.Tick(state, 1f / 60f);
            }, "AI tick with 18 players must not throw IndexOutOfRangeException");
        }

        [Test]
        public void AI_SelectsFreezeGrenade_AtCloseMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 12) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 12) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select freeze grenade (slot 12) at close-medium range");
        }
    }
}
