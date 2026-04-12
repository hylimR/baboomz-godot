using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public partial class RankSystemTests
    {
        // --- Rank Thresholds ---

        [Test]
        public void GetRankForXP_ZeroXP_ReturnsRank0()
        {
            Assert.AreEqual(0, RankSystem.GetRankForXP(0));
        }

        [Test]
        public void GetRankForXP_99XP_ReturnsRank0()
        {
            Assert.AreEqual(0, RankSystem.GetRankForXP(99));
        }

        [Test]
        public void GetRankForXP_100XP_ReturnsRank1()
        {
            Assert.AreEqual(1, RankSystem.GetRankForXP(100));
        }

        [Test]
        public void GetRankForXP_36000XP_ReturnsMaxRank()
        {
            Assert.AreEqual(19, RankSystem.GetRankForXP(36000));
        }

        [Test]
        public void GetRankForXP_999999XP_ReturnsMaxRank()
        {
            Assert.AreEqual(19, RankSystem.GetRankForXP(999999));
        }

        [Test]
        public void GetRankForXP_1000XP_ReturnsRank4_Cannoneer()
        {
            Assert.AreEqual(4, RankSystem.GetRankForXP(1000));
        }

        // --- Rank Titles ---

        [Test]
        public void GetRankTitle_ZeroXP_ReturnsRecruit()
        {
            Assert.AreEqual("Recruit", RankSystem.GetRankTitle(0));
        }

        [Test]
        public void GetRankTitle_100XP_ReturnsCadet()
        {
            Assert.AreEqual("Cadet", RankSystem.GetRankTitle(100));
        }

        [Test]
        public void GetRankTitle_36000XP_ReturnsSupremeCommander()
        {
            Assert.AreEqual("Supreme Commander", RankSystem.GetRankTitle(36000));
        }

        [Test]
        public void GetRankTitle_10000XP_ReturnsWarlord()
        {
            Assert.AreEqual("Warlord", RankSystem.GetRankTitle(10000));
        }

        // --- XP For Next Rank ---

        [Test]
        public void GetXPForNextRank_ZeroXP_Returns100()
        {
            Assert.AreEqual(100, RankSystem.GetXPForNextRank(0));
        }

        [Test]
        public void GetXPForNextRank_50XP_Returns50()
        {
            Assert.AreEqual(50, RankSystem.GetXPForNextRank(50));
        }

        [Test]
        public void GetXPForNextRank_MaxRank_ReturnsZero()
        {
            Assert.AreEqual(0, RankSystem.GetXPForNextRank(36000));
        }

        [Test]
        public void GetXPForNextRank_BeyondMax_ReturnsZero()
        {
            Assert.AreEqual(0, RankSystem.GetXPForNextRank(100000));
        }

        // --- Match XP Calculation: Base ---

        [Test]
        public void CalculateMatchXP_Win_Returns100Base()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { Won = true });
            Assert.AreEqual(100, result.BaseXP);
        }

        [Test]
        public void CalculateMatchXP_Loss_Returns30Base()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats());
            Assert.AreEqual(30, result.BaseXP);
        }

        [Test]
        public void CalculateMatchXP_Draw_Returns50Base()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { Draw = true });
            Assert.AreEqual(50, result.BaseXP);
        }

        // --- Sharpshooter Bonus ---

        [Test]
        public void CalculateMatchXP_Sharpshooter_50PercentAccuracy()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats
            {
                ShotsFired = 10, DirectHits = 5, DamageTaken = 100f
            });
            Assert.AreEqual(20, result.BonusXP);
            Assert.Contains("Sharpshooter", result.Bonuses);
        }

        [Test]
        public void CalculateMatchXP_NoSharpshooter_Below50Percent()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats
            {
                ShotsFired = 10, DirectHits = 4
            });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Sharpshooter") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoSharpshooter_ZeroShots()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats());
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Sharpshooter") >= 0);
        }

        // --- Demolisher Bonus ---

        [Test]
        public void CalculateMatchXP_Demolisher_150Damage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { TotalDamage = 150f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Demolisher") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoDemolisher_149Damage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { TotalDamage = 149f });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Demolisher") >= 0);
        }

        // --- Untouchable Bonus ---

        [Test]
        public void CalculateMatchXP_Untouchable_ZeroDamageTaken()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { DamageTaken = 0f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Untouchable") >= 0);
        }

        [Test]
        public void CalculateMatchXP_Untouchable_30DamageTaken()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { DamageTaken = 30f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Untouchable") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoUntouchable_31DamageTaken()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { DamageTaken = 31f });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Untouchable") >= 0);
        }

        // --- First Blood Bonus ---

        [Test]
        public void CalculateMatchXP_FirstBlood_Grants10()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { LandedFirstBlood = true, DamageTaken = 100f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "First Blood") >= 0);
            // Base 30 (loss) + 10 (first blood) = 40
            Assert.AreEqual(10, result.BonusXP - 0); // isolate: only first blood, damage taken > 30 removes untouchable
        }

        // --- Combo King Bonus ---

        [Test]
        public void CalculateMatchXP_ComboKing_60MaxDamage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { MaxSingleDamage = 60f, DamageTaken = 100f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Combo King") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoComboKing_59MaxDamage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { MaxSingleDamage = 59f, DamageTaken = 100f });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Combo King") >= 0);
        }

    }
}
