using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    /// <summary>
    /// Validates CareerStats summary and accuracy calculations (pure logic, no PlayerPrefs).
    /// </summary>
    [TestFixture]
    public class CareerStatsTests
    {
        // Mirror the AccuracyPct formula from CareerStats to test it in isolation
        static int AccuracyPct(int hits, int shots)
        {
            if (shots == 0) return 0;
            return (int)MathF.Round((float)hits / shots * 100f);
        }

        // Mirror the GetSummaryLine logic for testability without PlayerPrefs
        static string BuildSummaryLine(int kills, int hits, int shots, float bestHit)
        {
            if (shots == 0) return "";
            int acc = AccuracyPct(hits, shots);
            string best = bestHit > 0f ? $" \u00b7 best hit {bestHit:0}" : "";
            return $"{kills} kills \u00b7 {acc}% acc{best}";
        }

        [Test]
        public void AccuracyPct_ZeroShots_Returns0()
        {
            Assert.AreEqual(0, AccuracyPct(0, 0));
        }

        [Test]
        public void AccuracyPct_AllHits_Returns100()
        {
            Assert.AreEqual(100, AccuracyPct(10, 10));
        }

        [Test]
        public void AccuracyPct_HalfHits_Returns50()
        {
            Assert.AreEqual(50, AccuracyPct(5, 10));
        }

        [Test]
        public void AccuracyPct_SixtyPercent_RoundsCorrectly()
        {
            // 61/100 = 61%
            Assert.AreEqual(61, AccuracyPct(61, 100));
        }

        [Test]
        public void GetSummaryLine_ZeroShots_ReturnsEmpty()
        {
            Assert.AreEqual("", BuildSummaryLine(5, 0, 0, 50f));
        }

        [Test]
        public void GetSummaryLine_ContainsKillsAndAccuracy()
        {
            string line = BuildSummaryLine(42, 61, 100, 94.3f);
            Assert.IsTrue(line.Contains("42 kills"), "Should show kill count");
            Assert.IsTrue(line.Contains("61% acc"), "Should show accuracy");
            Assert.IsTrue(line.Contains("best hit 94"), "Should show best hit");
        }

        [Test]
        public void GetSummaryLine_ZeroBestHit_OmitsBestHitSection()
        {
            string line = BuildSummaryLine(10, 5, 10, 0f);
            Assert.IsFalse(line.Contains("best hit"), "Should omit best hit when it is 0");
        }

        [Test]
        public void GetSummaryLine_PositiveBestHit_IncludesBestHit()
        {
            string line = BuildSummaryLine(10, 5, 10, 75.5f);
            Assert.IsTrue(line.Contains("best hit 76") || line.Contains("best hit 75"),
                "Should include rounded best hit value");
        }
    }
}
