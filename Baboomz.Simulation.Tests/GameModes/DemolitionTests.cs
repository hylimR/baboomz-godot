using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public class DemolitionTests
    {
        static GameConfig DemoConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Demolition,
                TeamMode = true,
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
                DemolitionCrystalHP = 300f,
                DemolitionCrystalWidth = 3f,
                DemolitionCrystalHeight = 5f,
                DemolitionCrystalOffset = 10f,
                DemolitionLivesPerPlayer = 3,
                DemolitionRespawnDelay = 3f
            };
        }

        [Test]
        public void CreateMatch_Demolition_InitializesCrystals()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            Assert.IsNotNull(state.Demolition.Crystals);
            Assert.AreEqual(2, state.Demolition.Crystals.Length);
            Assert.AreEqual(300f, state.Demolition.Crystals[0].HP);
            Assert.AreEqual(300f, state.Demolition.Crystals[1].HP);
        }

        [Test]
        public void CheckDemolitionEnd_CrystalDestroyed_WinnerIsPlayerIndex_Issue109()
        {
            // Issue #109: WinnerIndex was set to team index (0 or 1) instead of
            // a player index. In a 4-player match, team 1 winning set WinnerIndex=1
            // which could be a player on team 0.
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            // Assign teams: P0=team 0, P1=team 1
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Destroy team 0's crystal (team 1 should win)
            state.Demolition.Crystals[0].HP = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerTeamIndex,
                "Winning team should be team 1");
            // WinnerIndex should be a player on team 1 (player 1), not the raw team index
            Assert.AreEqual(1, state.Players[state.WinnerIndex].TeamIndex,
                "WinnerIndex should point to a player on the winning team (issue #109)");
        }

        [Test]
        public void CheckDemolitionEnd_Team0CrystalDestroyed_WinnerIsAlivePlayerOnTeam1_Issue109()
        {
            // Regression: with 2 players where P0=team0 and P1=team1,
            // when team 0's crystal is destroyed, WinnerIndex must be P1 (index 1),
            // not the team index (also 1 by coincidence). Verify by checking the actual
            // player object.
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Destroy team 0's crystal
            state.Demolition.Crystals[0].HP = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            int winner = state.WinnerIndex;
            Assert.GreaterOrEqual(winner, 0, "WinnerIndex should be a valid player index");
            Assert.Less(winner, state.Players.Length, "WinnerIndex should be within player array bounds");
            Assert.AreEqual(1, state.Players[winner].TeamIndex,
                "Winner player should be on team 1 (issue #109)");
        }

        [Test]
        public void CheckDemolitionEnd_Team1CrystalDestroyed_WinnerOnTeam0_Issue109()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Destroy team 1's crystal (team 0 wins)
            state.Demolition.Crystals[1].HP = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerTeamIndex);
            int winner = state.WinnerIndex;
            Assert.GreaterOrEqual(winner, 0);
            Assert.Less(winner, state.Players.Length);
            Assert.AreEqual(0, state.Players[winner].TeamIndex,
                "Winner player should be on team 0 (issue #109)");
        }

        [Test]
        public void CheckDemolitionEnd_Tiebreaker_WinnerIsPlayerIndex_Issue109()
        {
            // When all lives are exhausted, tiebreaker uses crystal HP.
            // WinnerIndex should still be a player index, not a team index.
            var config = DemoConfig();
            config.DemolitionLivesPerPlayer = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Kill both players (no lives left)
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            // Team 1's crystal has more HP -> team 1 wins
            state.Demolition.Crystals[0].HP = 100f;
            state.Demolition.Crystals[1].HP = 200f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerTeamIndex,
                "Team 1 should win tiebreaker (higher crystal HP)");
            int winner = state.WinnerIndex;
            Assert.GreaterOrEqual(winner, 0);
            Assert.AreEqual(1, state.Players[winner].TeamIndex,
                "Tiebreaker winner should be a player on team 1 (issue #109)");
        }

        [Test]
        public void CheckDemolitionEnd_Tiebreaker_Draw()
        {
            var config = DemoConfig();
            config.DemolitionLivesPerPlayer = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            // Equal crystal HP = draw
            state.Demolition.Crystals[0].HP = 150f;
            state.Demolition.Crystals[1].HP = 150f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex, "Equal HP should be a draw");
        }

        [Test]
        public void CheckDemolitionEnd_WinnerPrefersAlivePlayer_Issue109()
        {
            // When the winning team has both alive and dead players,
            // WinnerIndex should prefer an alive player.
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            // P0=team0, P1=team1 (standard 2-player)
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Destroy team 0's crystal (team 1 wins)
            // P1 is alive
            state.Demolition.Crystals[0].HP = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.IsFalse(state.Players[state.WinnerIndex].IsDead,
                "WinnerIndex should prefer an alive player (issue #109)");
        }
    }
}
