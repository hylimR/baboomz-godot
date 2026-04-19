using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class HeadhunterTests
    {
        static GameConfig HhConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Headhunter,
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f,
                Player1SpawnX = -10f,
                Player2SpawnX = 10f,
                SpawnProbeY = 20f,
                DeathBoundaryY = -25f,
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                SuddenDeathTime = 0f,
                HeadhunterTokensOnDeath = 3,
                HeadhunterTokensToWin = 10,
                HeadhunterRespawnDelay = 5f,
                HeadhunterRespawnHealthFraction = 0.5f,
                HeadhunterTokenCollectRadius = 1.5f
            };
        }

        [Test]
        public void CreateMatch_Headhunter_InitializesState()
        {
            var state = GameSimulation.CreateMatch(HhConfig(), 42);

            Assert.IsNotNull(state.Headhunter.TokensCollected);
            Assert.AreEqual(2, state.Headhunter.TokensCollected.Length);
            Assert.AreEqual(0, state.Headhunter.TokensCollected[0]);
            Assert.AreEqual(0, state.Headhunter.TokensCollected[1]);
            Assert.AreEqual(0, state.Headhunter.TokenCount);
        }

        [Test]
        public void CreateMatch_Deathmatch_NoHeadhunterInit()
        {
            var config = HhConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.IsNull(state.Headhunter.TokensCollected);
        }

        [Test]
        public void Headhunter_Death_SpawnsTokens()
        {
            var state = GameSimulation.CreateMatch(HhConfig(), 42);

            // Kill P2
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            GameSimulation.SpawnHeadhunterTokens(state, 1);

            Assert.AreEqual(3, state.Headhunter.TokenCount,
                "Should spawn 3 tokens on death");
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(state.Headhunter.Tokens[i].Active);
        }

        [Test]
        public void Headhunter_TokenCollection()
        {
            var state = GameSimulation.CreateMatch(HhConfig(), 42);

            // Spawn tokens manually near P1
            state.Headhunter.Tokens[0] = new TokenPickup
            {
                Position = state.Players[0].Position,
                Active = true
            };
            state.Headhunter.TokenCount = 1;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(20f, 0f); // far away

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Headhunter.TokensCollected[0],
                "P1 should collect nearby token");
            Assert.AreEqual(0, state.Headhunter.TokenCount,
                "Token should be removed from map");
        }

        [Test]
        public void Headhunter_DeadPlayerCannotCollect()
        {
            var state = GameSimulation.CreateMatch(HhConfig(), 42);

            state.Headhunter.Tokens[0] = new TokenPickup
            {
                Position = state.Players[0].Position,
                Active = true
            };
            state.Headhunter.TokenCount = 1;
            state.Players[0].IsDead = true;
            state.Players[1].Position = new Vec2(20f, 0f);

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Headhunter.TokensCollected[0],
                "Dead player should not collect tokens");
        }

        [Test]
        public void Headhunter_Respawn_AfterDelay()
        {
            var config = HhConfig();
            config.HeadhunterRespawnDelay = 2f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Kill P2
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            // Tick for less than respawn delay
            for (int i = 0; i < 10; i++) // 0.16s
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[1].IsDead, "Should still be dead before delay");

            // Tick past respawn delay
            for (int i = 0; i < 200; i++) // 3.2s total
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[1].IsDead, "Should respawn after delay");
        }

        [Test]
        public void Headhunter_Respawn_ReducedHealth()
        {
            var config = HhConfig();
            config.HeadhunterRespawnDelay = 0.1f;
            config.HeadhunterRespawnHealthFraction = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            // Tick past respawn
            for (int i = 0; i < 20; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[1].IsDead);
            Assert.AreEqual(50f, state.Players[1].Health, 1f,
                "Respawned player should have 50% HP");
        }

        [Test]
        public void Headhunter_WinCondition()
        {
            var config = HhConfig();
            config.HeadhunterTokensToWin = 5;
            var state = GameSimulation.CreateMatch(config, 42);

            // Give P1 enough tokens to win
            state.Headhunter.TokensCollected[0] = 4;

            // Place a token near P1
            state.Headhunter.Tokens[0] = new TokenPickup
            {
                Position = state.Players[0].Position,
                Active = true
            };
            state.Headhunter.TokenCount = 1;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(20f, 0f);

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P1 should win");
        }

        [Test]
        public void Headhunter_MatchDoesNotEndOnDeath()
        {
            var state = GameSimulation.CreateMatch(HhConfig(), 42);

            // Kill P2 — match should NOT end (respawn mode)
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Match should not end on death in Headhunter");
        }

        [Test]
        public void Headhunter_TokenCollectEvent_Emitted()
        {
            var state = GameSimulation.CreateMatch(HhConfig(), 42);

            state.Headhunter.Tokens[0] = new TokenPickup
            {
                Position = state.Players[0].Position,
                Active = true
            };
            state.Headhunter.TokenCount = 1;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(20f, 0f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.TokenCollectEvents.Count > 0,
                "Should emit token collect event");
            Assert.AreEqual(0, state.TokenCollectEvents[0].PlayerIndex);
        }

        [Test]
        public void Headhunter_SpawnTokens_NotHeadhunterMode_NoOp()
        {
            var config = HhConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            // Should not throw
            GameSimulation.SpawnHeadhunterTokens(state, 0);
        }

        [Test]
        public void Headhunter_MultipleDeaths_AccumulateTokens()
        {
            var config = HhConfig();
            config.HeadhunterRespawnDelay = 0.05f;
            config.HeadhunterTokensOnDeath = 3;
            var state = GameSimulation.CreateMatch(config, 42);

            // First death
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            GameSimulation.SpawnHeadhunterTokens(state, 1);

            Assert.AreEqual(3, state.Headhunter.TokenCount);

            // Respawn
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // Second death
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            GameSimulation.SpawnHeadhunterTokens(state, 1);

            // Should have accumulated tokens (minus any collected)
            Assert.GreaterOrEqual(state.Headhunter.TokenCount, 3,
                "Tokens should accumulate from multiple deaths");
        }

        [Test]
        public void Headhunter_Respawn_RestoresWeaponAmmo_Issue84()
        {
            // Issue #84: Respawn didn't restore weapon ammo, causing
            // progressive weapon starvation across multiple deaths.
            var config = HhConfig();
            config.HeadhunterRespawnDelay = 0.01f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            // Deplete ammo on a limited weapon (rocket = slot 2, default ammo = 4)
            int rocketSlot = -1;
            for (int s = 0; s < state.Players[0].WeaponSlots.Length; s++)
            {
                if (state.Players[0].WeaponSlots[s].WeaponId == "rocket")
                {
                    rocketSlot = s;
                    state.Players[0].WeaponSlots[s].Ammo = 0;
                    break;
                }
            }
            Assert.GreaterOrEqual(rocketSlot, 0, "Should have rocket weapon");
            Assert.AreEqual(0, state.Players[0].WeaponSlots[rocketSlot].Ammo);

            // Kill and respawn player
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            GameSimulation.SpawnHeadhunterTokens(state, 0);

            // Tick through respawn delay
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // After respawn, ammo should be restored
            Assert.IsFalse(state.Players[0].IsDead, "Player should have respawned");
            Assert.AreNotEqual(0, state.Players[0].WeaponSlots[rocketSlot].Ammo,
                "Rocket ammo should be restored after respawn (issue #84)");
        }
        [Test]
        public void Headhunter_Respawn_ResetsSkillCooldowns_Issue272()
        {
            var config = HhConfig();
            config.HeadhunterRespawnDelay = 0.01f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState { SkillId = "war_cry", Type = SkillType.WarCry, CooldownRemaining = 10f },
                new SkillSlotState { SkillId = "shield", Type = SkillType.Shield, CooldownRemaining = 5f }
            };

            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            GameSimulation.SpawnHeadhunterTokens(state, 0);

            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsDead, "Player should have respawned");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining,
                "Skill cooldowns should reset to 0 on respawn");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[1].CooldownRemaining,
                "All skill cooldowns should reset on respawn");
        }
    }
}
