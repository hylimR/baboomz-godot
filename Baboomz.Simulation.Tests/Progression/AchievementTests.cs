using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class AchievementDefsTests
    {
        [Test]
        public void AllDefs_Has30Achievements()
        {
            Assert.AreEqual(30, AchievementDefs.All.Length);
        }

        [Test]
        public void AllDefs_UniqueIds()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var def in AchievementDefs.All)
            {
                Assert.IsFalse(ids.Contains(def.Id), $"Duplicate achievement ID: {def.Id}");
                ids.Add(def.Id);
            }
        }

        [Test]
        public void AllDefs_NonEmptyNamesAndDescriptions()
        {
            foreach (var def in AchievementDefs.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(def.Name), $"{def.Id} has empty Name");
                Assert.IsFalse(string.IsNullOrEmpty(def.Description), $"{def.Id} has empty Description");
            }
        }

        [Test]
        public void AllDefs_ValidCategories()
        {
            foreach (var def in AchievementDefs.All)
            {
                Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementCategory), def.Category),
                    $"{def.Id} has invalid category: {def.Category}");
            }
        }

        [Test]
        public void AllDefs_HiddenOnlyMiscCategory()
        {
            foreach (var def in AchievementDefs.All)
            {
                if (def.IsHidden)
                    Assert.AreEqual(AchievementCategory.Misc, def.Category,
                        $"Hidden achievement {def.Id} should be in Misc category");
            }
        }

        [Test]
        public void CategoryCounts_Match()
        {
            int combat = 0, skill = 0, campaign = 0, misc = 0;
            foreach (var def in AchievementDefs.All)
            {
                switch (def.Category)
                {
                    case AchievementCategory.Combat: combat++; break;
                    case AchievementCategory.Skill: skill++; break;
                    case AchievementCategory.Campaign: campaign++; break;
                    case AchievementCategory.Misc: misc++; break;
                }
            }
            Assert.AreEqual(10, combat);
            Assert.AreEqual(8, skill);
            Assert.AreEqual(7, campaign);
            Assert.AreEqual(5, misc);
        }
    }
}
