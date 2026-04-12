using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Regression tests for #305: inactive mines/barrels cleaned up ---

        [Test]
        public void Mines_InactiveEntriesRemovedAfterTrigger()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Settle players
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Add several mines manually (simulating boss/skill spawns)
            for (int m = 0; m < 5; m++)
            {
                float mx = state.Players[0].Position.x + 20f + m * 5f;
                float my = GamePhysics.FindGroundY(state.Terrain, mx, 20f);
                state.Mines.Add(new MineState
                {
                    Position = new Vec2(mx, my),
                    TriggerRadius = 1.5f,
                    ExplosionRadius = 3f,
                    Damage = 30f,
                    Active = true,
                    OwnerIndex = -1
                });
            }

            Assert.AreEqual(5, state.Mines.Count);

            // Walk player onto the first mine to trigger it
            var firstMinePos = state.Mines[0].Position;
            state.Players[0].Position = firstMinePos;
            GameSimulation.Tick(state, 0.016f);

            // The triggered mine should be removed, not just deactivated
            foreach (var mine in state.Mines)
                Assert.IsTrue(mine.Active, "Only active mines should remain in the list");
            Assert.Less(state.Mines.Count, 5, "Inactive mine should have been removed");
        }

        [Test]
        public void Barrels_InactiveEntriesRemovedAfterExplosion()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place 2 barrels far apart so only one gets hit
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(5f, 5f),
                ExplosionRadius = 3f,
                Damage = 50f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(50f, 5f), // far away, won't be hit
                ExplosionRadius = 3f,
                Damage = 50f,
                Active = true
            });

            Assert.AreEqual(2, state.Barrels.Count);

            // Build terrain wall at first barrel to force projectile impact
            int wallPx = state.Terrain.WorldToPixelX(5f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 5; py < wallPy + 5; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            // Fire projectile close to first barrel (same pattern as Barrels_ExplodeWhenHitByExplosion)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(4.5f, 5f),
                Velocity = new Vec2(2f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick until first barrel explodes
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count < 2) break;
            }

            // The exploded barrel should be removed, leaving only the far-away one
            Assert.AreEqual(1, state.Barrels.Count, "Inactive barrel should have been removed");
            Assert.IsTrue(state.Barrels[0].Active, "Only active barrels should remain in the list");
        }

        // --- Airstrike weapon tests ---

        [Test]
        public void Airstrike_WeaponExists_InSlot6()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.GreaterOrEqual(state.Players[0].WeaponSlots.Length, 7, "Should have at least 7 weapon slots");
            Assert.AreEqual("airstrike", state.Players[0].WeaponSlots[6].WeaponId);
            Assert.AreEqual(1, state.Players[0].WeaponSlots[6].Ammo);
            Assert.IsTrue(state.Players[0].WeaponSlots[6].IsAirstrike);
        }

        [Test]
        public void AI_StartingWeapon_NeverSelectsAirstrike()
        {
            // Test multiple seeds to verify airstrike is never selected
            for (int seed = 0; seed < 100; seed++)
            {
                var state = GameSimulation.CreateMatch(SmallConfig(), seed);
                int aiSlot = state.Players[1].ActiveWeaponSlot;
                Assert.IsFalse(state.Players[1].WeaponSlots[aiSlot].IsAirstrike,
                    $"Seed {seed}: AI started with airstrike at slot {aiSlot}");
            }
        }

        [Test]
        public void Airstrike_MarkerProjectile_SpawnsBombsOnImpact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire airstrike (slot 6 after dynamite/napalm were added)
            state.Players[0].ActiveWeaponSlot = 6;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsAirstrike);

            // Tick until marker hits terrain — should spawn 5 bombs
            bool spawnedBombs = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Projectiles.Count > 1)
                {
                    spawnedBombs = true;
                    break;
                }
                if (state.Projectiles.Count == 0) break; // hit terrain, spawned and some already hit
            }

            // Either bombs were spawned (count > 1) or they already exploded (explosions happened)
            Assert.IsTrue(spawnedBombs || state.ExplosionEvents.Count > 0,
                "Airstrike should spawn bombs on impact that create explosions");
        }

        // --- Team mode tests ---

        [Test]
        public void TeamMode_MatchEnds_WhenOneTeamEliminated()
        {
            var config = SmallConfig();
            config.TeamMode = true;

            var state = GameSimulation.CreateMatch(config, 42);

            // Assign teams: P0 = team 0, P1 = team 1
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Kill team 1
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerTeamIndex, "Team 0 should win");
        }

        [Test]
        public void TeamMode_MatchContinues_WhileBothTeamsHaveAlive()
        {
            var config = SmallConfig();
            config.TeamMode = true;

            var state = GameSimulation.CreateMatch(config, 42);

            // 4-player match: 2v2
            // Expand players array
            state.Players = new PlayerState[4];
            for (int i = 0; i < 4; i++)
            {
                state.Players[i] = new PlayerState
                {
                    Position = new Vec2(-10f + i * 7f, 5f),
                    Health = 100f, MaxHealth = 100f,
                    Energy = 100f, MaxEnergy = 100f,
                    MoveSpeed = 5f, IsAI = i > 0,
                    TeamIndex = i < 2 ? 0 : 1,
                    WeaponSlots = new WeaponSlotState[1],
                    SkillSlots = new SkillSlotState[0]
                };
            }

            // Kill one member of each team — match should continue
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[2].IsDead = true;
            state.Players[2].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Match should continue with alive members on both teams");
        }

        [Test]
        public void TeamMode_Draw_WhenAllTeamsEliminated()
        {
            var config = SmallConfig();
            config.TeamMode = true;

            var state = GameSimulation.CreateMatch(config, 42);
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Kill both
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerTeamIndex, "Draw when all teams eliminated");
        }

        [Test]
        public void DefaultMode_TeamIndexIgnored()
        {
            var config = SmallConfig();
            config.TeamMode = false;

            var state = GameSimulation.CreateMatch(config, 42);

            // TeamIndex defaults to -1 in FFA mode
            Assert.AreEqual(-1, state.Players[0].TeamIndex);
            Assert.AreEqual(-1, state.Players[1].TeamIndex);

            // Standard FFA win condition still works
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P0 wins in FFA mode");
        }

    }
}
