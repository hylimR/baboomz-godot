using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class SeriesStateTests
    {
        [Test]
        public void Create_BestOf3_TargetWinsIs2()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            Assert.AreEqual(2, series.TargetWins);
            Assert.AreEqual(0, series.RoundsPlayed);
            Assert.AreEqual(2, series.WinsPerPlayer.Length);
            Assert.AreEqual(3, series.RoundWinners.Length);
        }

        [Test]
        public void Create_BestOf5_TargetWinsIs3()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf5, 2);
            Assert.AreEqual(3, series.TargetWins);
            Assert.AreEqual(5, series.RoundWinners.Length);
        }

        [Test]
        public void Create_Single_TargetWinsIs1()
        {
            var series = SeriesState.Create(SeriesFormat.Single, 2);
            Assert.AreEqual(1, series.TargetWins);
            Assert.IsFalse(series.IsActive);
        }

        [Test]
        public void IsActive_BestOf3_ReturnsTrue()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            Assert.IsTrue(series.IsActive);
        }

        [Test]
        public void RecordRound_IncrementsWinsAndRoundsPlayed()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(0);
            Assert.AreEqual(1, series.RoundsPlayed);
            Assert.AreEqual(1, series.WinsPerPlayer[0]);
            Assert.AreEqual(0, series.WinsPerPlayer[1]);
            Assert.AreEqual(0, series.RoundWinners[0]);
        }

        [Test]
        public void RecordRound_Draw_NoWinsIncremented()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(-1);
            Assert.AreEqual(1, series.RoundsPlayed);
            Assert.AreEqual(0, series.WinsPerPlayer[0]);
            Assert.AreEqual(0, series.WinsPerPlayer[1]);
        }

        [Test]
        public void IsSeriesOver_Player0Reaches2Wins_ReturnsTrue()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(0);
            Assert.IsFalse(series.IsSeriesOver());
            series.RecordRound(0);
            Assert.IsTrue(series.IsSeriesOver());
        }

        [Test]
        public void GetSeriesWinner_Player1Wins_Returns1()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(1);
            series.RecordRound(0);
            series.RecordRound(1);
            Assert.AreEqual(1, series.GetSeriesWinner());
        }

        [Test]
        public void GetSeriesWinner_NotOver_ReturnsMinus1()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(0);
            Assert.AreEqual(-1, series.GetSeriesWinner());
        }

        [Test]
        public void BestOf5_FullSeries_Player0WinsIn4()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf5, 2);
            series.RecordRound(0);
            series.RecordRound(1);
            series.RecordRound(0);
            Assert.IsFalse(series.IsSeriesOver());
            series.RecordRound(0);
            Assert.IsTrue(series.IsSeriesOver());
            Assert.AreEqual(0, series.GetSeriesWinner());
            Assert.AreEqual(4, series.RoundsPlayed);
        }

        [Test]
        public void RoundWinners_TracksCorrectly()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(0);
            series.RecordRound(1);
            series.RecordRound(0);
            Assert.AreEqual(0, series.RoundWinners[0]);
            Assert.AreEqual(1, series.RoundWinners[1]);
            Assert.AreEqual(0, series.RoundWinners[2]);
        }

        [Test]
        public void RecordRound_AllDraws_RoundsPlayedClamped()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(-1);
            series.RecordRound(-1);
            series.RecordRound(-1);
            // Extra draw should not overflow
            series.RecordRound(-1);

            Assert.AreEqual(3, series.RoundsPlayed, "RoundsPlayed must not exceed RoundWinners.Length");
            Assert.AreEqual(0, series.WinsPerPlayer[0]);
            Assert.AreEqual(0, series.WinsPerPlayer[1]);
        }

        [Test]
        public void IsSeriesOver_AllDraws_ReturnsTrueWhenRoundsExhausted()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(-1);
            series.RecordRound(-1);
            Assert.IsFalse(series.IsSeriesOver());
            series.RecordRound(-1);
            Assert.IsTrue(series.IsSeriesOver(), "Series should end when all rounds are exhausted");
            Assert.AreEqual(-1, series.GetSeriesWinner(), "No winner when all rounds are draws");
        }

        [Test]
        public void RecordRound_MixedDrawsAndWins_NoOverflow()
        {
            var series = SeriesState.Create(SeriesFormat.BestOf3, 2);
            series.RecordRound(-1); // draw
            series.RecordRound(0);  // player 0 wins
            series.RecordRound(-1); // draw — fills all 3 slots

            Assert.AreEqual(3, series.RoundsPlayed);
            Assert.AreEqual(1, series.WinsPerPlayer[0]);
            Assert.IsTrue(series.IsSeriesOver(), "Rounds exhausted, series should be over");
        }
    }
}
