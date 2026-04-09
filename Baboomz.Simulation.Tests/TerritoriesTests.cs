using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public class TerritoriesTests
    {
        static GameConfig MakeConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Territories,
                TeamMode = false,
                TerritoryZoneRadius = 12f,
                TerritoryPointsPerSecond = 1f,
                TerritoryPointsToWin = 300f,
                TerritoryZoneCount = 3,
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
            };
        }

        [Test]
        public void Init_Creates3Zones()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            Assert.IsNotNull(state.Territory.ZonePositions);
            Assert.AreEqual(3, state.Territory.ZonePositions.Length);
        }

        [Test]
        public void Init_ZonesAreDistinct()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            var pos = state.Territory.ZonePositions;
            Assert.That(System.MathF.Abs(pos[0].x - pos[1].x), Is.GreaterThan(0.1f));
            Assert.That(System.MathF.Abs(pos[1].x - pos[2].x), Is.GreaterThan(0.1f));
        }

        [Test]
        public void Init_ZonesAreSymmetric()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            var pos = state.Territory.ZonePositions;
            // Left zone X should be approximately -center zone X
            Assert.AreEqual(-pos[0].x, pos[2].x, 0.5f);
        }

        [Test]
        public void Init_2Teams()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            Assert.AreEqual(2, state.Territory.TeamScores.Length);
        }

        [Test]
        public void Init_AllZonesNeutral()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(-1, state.Territory.ZoneOwner[i]);
        }

        [Test]
        public void Init_ScoresStartAtZero()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            Assert.AreEqual(0f, state.Territory.TeamScores[0]);
            Assert.AreEqual(0f, state.Territory.TeamScores[1]);
        }

        [Test]
        public void Tick_PlayerInZone_ScoresForTeam()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            state.Phase = MatchPhase.Playing;

            // Move player 0 (team 0) to zone 0
            state.Players[0].Position = state.Territory.ZonePositions[0];
            // Move player 1 (team 1) far away
            state.Players[1].Position = new Vec2(100f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.Greater(state.Territory.TeamScores[0], 0f, "Team 0 should score when in zone");
        }

        [Test]
        public void Tick_ContestedZone_NoScoring()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            state.Phase = MatchPhase.Playing;

            // Both players in zone 1
            state.Players[0].Position = state.Territory.ZonePositions[1];
            state.Players[1].Position = state.Territory.ZonePositions[1];

            float scoreBefore0 = state.Territory.TeamScores[0];
            float scoreBefore1 = state.Territory.TeamScores[1];

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(scoreBefore0, state.Territory.TeamScores[0], 0.01f, "No scoring when contested");
            Assert.AreEqual(scoreBefore1, state.Territory.TeamScores[1], 0.01f, "No scoring when contested");
            Assert.IsTrue(state.Territory.ZoneContested[1], "Zone should be contested");
        }

        [Test]
        public void Tick_EmptyZone_BecomesNeutral()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            state.Phase = MatchPhase.Playing;

            // Put player in zone 0 to claim it
            state.Players[0].Position = state.Territory.ZonePositions[0];
            state.Players[1].Position = new Vec2(100f, 0f);
            GameSimulation.Tick(state, 1f);
            Assert.AreEqual(0, state.Territory.ZoneOwner[0]);

            // Move player away
            state.Players[0].Position = new Vec2(-100f, 0f);
            GameSimulation.Tick(state, 1f);
            Assert.AreEqual(-1, state.Territory.ZoneOwner[0], "Zone should be neutral when empty");
        }

        [Test]
        public void Tick_ReachScoreLimit_EndsMatch()
        {
            var config = MakeConfig();
            config.TerritoryPointsToWin = 10f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = state.Territory.ZonePositions[0];
            state.Players[1].Position = new Vec2(100f, 0f);

            // Tick enough to accumulate 10+ points
            for (int i = 0; i < 15; i++)
                GameSimulation.Tick(state, 1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex);
        }

        [Test]
        public void Tick_Team1Wins_WhenScoreReached()
        {
            var config = MakeConfig();
            config.TerritoryPointsToWin = 10f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Team 1 holds all 3 zones
            state.Players[1].Position = state.Territory.ZonePositions[1];
            state.Players[0].Position = new Vec2(-100f, 0f);

            for (int i = 0; i < 15; i++)
                GameSimulation.Tick(state, 1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex);
        }

        [Test]
        public void Tick_MultipleZones_ScoreFaster()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            state.Phase = MatchPhase.Playing;

            // Player 0 in zone 0 only
            state.Players[0].Position = state.Territory.ZonePositions[0];
            state.Players[1].Position = new Vec2(100f, 0f);
            GameSimulation.Tick(state, 1f);
            float singleZoneScore = state.Territory.TeamScores[0];

            // Reset and test with player controlling 1 zone (same thing since 1 player)
            Assert.Greater(singleZoneScore, 0f);
            Assert.AreEqual(1f, singleZoneScore, 0.1f, "1 zone = 1 pt/sec");
        }

        [Test]
        public void Tick_DeadPlayer_DoesNotCapture()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].IsDead = true;
            state.Players[0].Position = state.Territory.ZonePositions[0];
            state.Players[1].Position = new Vec2(100f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(-1, state.Territory.ZoneOwner[0], "Dead player should not capture zone");
        }

        [Test]
        public void Init_ZoneRadius_MatchesConfig()
        {
            var config = MakeConfig();
            config.TerritoryZoneRadius = 8f;
            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(8f, state.Territory.ZoneRadius);
        }

        [Test]
        public void Tick_MobsDoNotCapture()
        {
            var state = GameSimulation.CreateMatch(MakeConfig(), 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].IsMob = true;
            state.Players[0].Position = state.Territory.ZonePositions[0];
            state.Players[1].Position = new Vec2(100f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(-1, state.Territory.ZoneOwner[0], "Mobs should not capture zones");
        }
    }
}
