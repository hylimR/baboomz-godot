using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void BalanceTest_200Matches_AIWinRateReasonable()
        {
            var config = SmallConfig();
            config.AIShootInterval = 2f;
            config.AIShootIntervalRandomness = 1f;

            int p1Wins = 0, p2Wins = 0, draws = 0, timeouts = 0;

            for (int match = 0; match < 200; match++)
            {
                var state = GameSimulation.CreateMatch(config, match * 7 + 13);
                AILogic.Reset(match * 7 + 13);

                // Simulate up to 60 seconds (3750 frames at 16ms)
                for (int frame = 0; frame < 3750; frame++)
                {
                    GameSimulation.Tick(state, 0.016f);
                    if (state.Phase == MatchPhase.Ended) break;
                }

                if (state.Phase == MatchPhase.Ended)
                {
                    if (state.WinnerIndex == 0) p1Wins++;
                    else if (state.WinnerIndex == 1) p2Wins++;
                    else draws++;
                }
                else
                {
                    timeouts++;
                }
            }

            int totalDecided = p1Wins + p2Wins + draws;
            Assert.Greater(totalDecided, 0, "At least some matches should end");

            // AI should win SOME matches (it shoots at the player)
            // If AI never wins, its aim/movement is broken
            Assert.Greater(p2Wins, 0,
                $"AI should win at least 1 match (P1:{p1Wins} P2:{p2Wins} Draw:{draws} Timeout:{timeouts})");
        }

        [Test]
        public void LongMatch_5000Frames_Stable()
        {
            var config = SmallConfig();
            config.AIShootInterval = 1f;
            config.AIShootIntervalRandomness = 0.5f;

            var state = GameSimulation.CreateMatch(config, 99);
            AILogic.Reset(99);

            // Run for 5000 frames (~80 seconds) — testing stability, no crashes
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 5000; i++)
                {
                    GameSimulation.Tick(state, 0.016f);
                    if (state.Phase == MatchPhase.Ended) break;
                }
            });

            // AI should have fired at least once
            Assert.Greater(state.Players[1].ShotsFired, 0,
                "AI should fire at least once in 5000 frames");
        }

        [Test]
        public void DifficultyEasy_AIHasHighErrorMargin()
        {
            var config = SmallConfig();
            // Simulate what GameRunner.ApplyDifficulty does for Easy
            config.AIAimErrorMargin = 12f;
            config.AIShootInterval = 5f;
            config.DefaultMaxHealth = 150f;

            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(150f, state.Players[0].MaxHealth, "Easy should give 150 HP");
            Assert.AreEqual(150f, state.Players[1].MaxHealth, "AI also gets 150 HP");
        }

        [Test]
        public void DifficultyHard_PlayerHasLessHP()
        {
            var config = SmallConfig();
            config.AIAimErrorMargin = 2f;
            config.AIShootInterval = 2f;
            config.DefaultMaxHealth = 80f;

            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(80f, state.Players[0].MaxHealth, "Hard should give 80 HP");
        }

        [Test]
        public void MultiRound_SequentialMatches_NoCorruption()
        {
            var config = SmallConfig();
            config.AIShootInterval = 1f;

            // Simulate 3 rounds like the round system does
            for (int round = 0; round < 3; round++)
            {
                var state = GameSimulation.CreateMatch(config, round * 100 + 7);
                AILogic.Reset(round * 100 + 7);

                Assert.AreEqual(MatchPhase.Playing, state.Phase);
                Assert.AreEqual(2, state.Players.Length);
                Assert.IsFalse(state.Players[0].IsDead);
                Assert.IsFalse(state.Players[1].IsDead);
                Assert.Greater(state.Players[0].Health, 0f);

                // Run until match ends or timeout
                for (int frame = 0; frame < 2000; frame++)
                {
                    GameSimulation.Tick(state, 0.016f);
                    if (state.Phase == MatchPhase.Ended) break;
                }

                // State should be valid regardless of outcome
                Assert.IsTrue(state.Phase == MatchPhase.Playing || state.Phase == MatchPhase.Ended);
            }
        }

        [Test]
        public void AllWeaponTypes_CanFire()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            for (int slot = 0; slot < 4; slot++)
            {
                if (state.Players[0].WeaponSlots[slot].WeaponId == null) continue;

                state.Players[0].ActiveWeaponSlot = slot;
                state.Players[0].AimPower = 15f;
                state.Players[0].ShootCooldownRemaining = 0f;
                state.Players[0].Energy = 100f;

                int before = state.Projectiles.Count;
                GameSimulation.Fire(state, 0);

                Assert.Greater(state.Projectiles.Count, before,
                    $"Weapon slot {slot} ({state.Players[0].WeaponSlots[slot].WeaponId}) should fire");
            }
        }

        [Test]
        public void SplashEvent_EmittedOnSwimEntry()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Move player below water level
            state.Players[0].Position = new Vec2(0f, state.Config.DeathBoundaryY - 1f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsSwimming, "Player should start swimming");
            Assert.Greater(state.SplashEvents.Count, 0, "Splash event should emit on swim entry");
        }

        [Test]
        public void Mines_SpawnOnTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 5;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(5, state.Mines.Count, "Should spawn 5 mines");
            foreach (var mine in state.Mines)
            {
                Assert.IsTrue(mine.Active, "All mines should start active");
                Assert.Greater(mine.ExplosionRadius, 0f);
            }
        }

        [Test]
        public void Mines_SkipSpawnWhenNoGroundAtX()
        {
            var config = SmallConfig();
            config.TerrainWidth = 20; // very narrow terrain (2.5 world units at PPU=8)
            config.MapWidth = 200f;   // wide map — most X samples miss terrain
            config.MineCount = 10;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // With fix: mines at X with no ground are skipped, so count <= requested
            Assert.LessOrEqual(state.Mines.Count, config.MineCount);
            foreach (var mine in state.Mines)
            {
                Assert.That(mine.Position.y, Is.Not.EqualTo(config.SpawnProbeY).Within(0.3f),
                    "Mine should not spawn at SpawnProbeY fallback height");
            }
        }

        [Test]
        public void Mine_ExplodesWhenPlayerWalksOver()
        {
            var config = SmallConfig();
            config.MineCount = 0; // no random mines
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Place a mine at a known safe position away from players
            float mineX = state.Players[0].Position.x + 10f;
            float mineY = GamePhysics.FindGroundY(state.Terrain, mineX, 20f);
            state.Mines.Add(new MineState
            {
                Position = new Vec2(mineX, mineY),
                TriggerRadius = 1.5f,
                ExplosionRadius = 3f,
                Damage = 45f,
                Active = true,
                OwnerIndex = -1 // environment mine
            });

            // Move player to mine position
            state.Players[0].Position = new Vec2(mineX, mineY);

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Mines.Count, "Triggered mine should be removed from list");
            Assert.Greater(state.ExplosionEvents.Count, 0, "Mine should produce explosion");
        }

        [Test]
        public void DynamiteBalance_MaxDamageWithinTargetRange()
        {
            // Regression for #111: Dynamite MaxDamage reduced from 80 -> 70 to keep
            // DPS (MaxDamage/ShootCooldown) close to 2x the weapon median and prevent
            // dynamite from dominating the weapon pool on DPS/Energy efficiency.
            var config = new GameConfig();
            bool found = false;
            WeaponDef dynamite = default;
            foreach (var w in config.Weapons)
            {
                if (w.WeaponId == "dynamite") { dynamite = w; found = true; break; }
            }

            Assert.IsTrue(found, "dynamite weapon definition must exist");
            Assert.LessOrEqual(dynamite.MaxDamage, 70f,
                "Dynamite MaxDamage must be <= 70 after #111 balance change");
            Assert.GreaterOrEqual(dynamite.MaxDamage, 65f,
                "Dynamite MaxDamage must stay within the 65-70 target range");
        }

        [Test]
        public void LightningRodBalance_CostAndCooldownReduced_Issue270()
        {
            var config = new GameConfig();
            WeaponDef lr = default;
            foreach (var w in config.Weapons)
                if (w.WeaponId == "lightning_rod") { lr = w; break; }

            Assert.AreEqual(22f, lr.EnergyCost, 0.01f,
                "Lightning Rod EnergyCost should be 22 after #270 rebalance");
            Assert.AreEqual(3.5f, lr.ShootCooldown, 0.01f,
                "Lightning Rod ShootCooldown should be 3.5s after #270 rebalance");
        }

    }
}
