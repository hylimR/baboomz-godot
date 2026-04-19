using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class EncyclopediaDataTests
    {
        private GameConfig config;

        [SetUp]
        public void SetUp()
        {
            config = new GameConfig();
        }

        [Test]
        public void GetWeaponEntries_Returns22Weapons()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            Assert.AreEqual(22, entries.Length);
        }

        [Test]
        public void GetWeaponEntries_AllHaveNames()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            foreach (var e in entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.Name), $"Weapon {e.Id} has no name");
            }
        }

        [Test]
        public void GetWeaponEntries_AllHaveDescriptions()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            foreach (var e in entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.Description), $"Weapon {e.Id} has no description");
                Assert.AreNotEqual("Unknown weapon.", e.Description, $"Weapon {e.Id} missing description");
            }
        }

        [Test]
        public void GetWeaponEntries_AllHaveDamageAndAmmoStats()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            foreach (var e in entries)
            {
                Assert.IsTrue(e.Stats.ContainsKey("Damage"), $"Weapon {e.Id} missing Damage stat");
                Assert.IsTrue(e.Stats.ContainsKey("Ammo"), $"Weapon {e.Id} missing Ammo stat");
            }
        }

        [Test]
        public void GetWeaponEntries_UniqueIds()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in entries)
            {
                Assert.IsTrue(ids.Add(e.Id), $"Duplicate weapon ID: {e.Id}");
            }
        }

        [Test]
        public void GetSkillEntries_Returns19Skills()
        {
            var entries = EncyclopediaData.GetSkillEntries(config);
            Assert.AreEqual(21, entries.Length);
        }

        [Test]
        public void GetSkillEntries_AllHaveNames()
        {
            var entries = EncyclopediaData.GetSkillEntries(config);
            foreach (var e in entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.Name), $"Skill {e.Id} has no name");
            }
        }

        [Test]
        public void GetSkillEntries_AllHaveDescriptions()
        {
            var entries = EncyclopediaData.GetSkillEntries(config);
            foreach (var e in entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.Description), $"Skill {e.Id} has no description");
                Assert.AreNotEqual("Unknown skill.", e.Description, $"Skill {e.Id} missing description");
            }
        }

        [Test]
        public void GetSkillEntries_AllHaveEnergyStat()
        {
            var entries = EncyclopediaData.GetSkillEntries(config);
            foreach (var e in entries)
            {
                Assert.IsTrue(e.Stats.ContainsKey("Energy"), $"Skill {e.Id} missing Energy stat");
            }
        }

        [Test]
        public void GetMobEntries_Returns4Mobs()
        {
            var entries = EncyclopediaData.GetMobEntries();
            Assert.AreEqual(4, entries.Length);
        }

        [Test]
        public void GetMobEntries_HasExpectedTypes()
        {
            var entries = EncyclopediaData.GetMobEntries();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in entries) ids.Add(e.Id);

            Assert.IsTrue(ids.Contains("bomber"), "Missing bomber");
            Assert.IsTrue(ids.Contains("shielder"), "Missing shielder");
            Assert.IsTrue(ids.Contains("flyer"), "Missing flyer");
            Assert.IsTrue(ids.Contains("healer"), "Missing healer");
        }

        [Test]
        public void GetBossEntries_Returns5Bosses()
        {
            var entries = EncyclopediaData.GetBossEntries();
            Assert.AreEqual(5, entries.Length);
        }

        [Test]
        public void GetBossEntries_HasExpectedTypes()
        {
            var entries = EncyclopediaData.GetBossEntries();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in entries) ids.Add(e.Id);

            Assert.IsTrue(ids.Contains("iron_sentinel"), "Missing iron_sentinel");
            Assert.IsTrue(ids.Contains("sand_wyrm"), "Missing sand_wyrm");
            Assert.IsTrue(ids.Contains("glacial_cannon"), "Missing glacial_cannon");
            Assert.IsTrue(ids.Contains("forge_colossus"), "Missing forge_colossus");
            Assert.IsTrue(ids.Contains("baron_cogsworth"), "Missing baron_cogsworth");
        }

        [Test]
        public void GetBiomeEntries_Returns8Biomes()
        {
            var entries = EncyclopediaData.GetBiomeEntries();
            Assert.AreEqual(8, entries.Length);
        }

        [Test]
        public void GetBiomeEntries_HasExpectedTypes()
        {
            var entries = EncyclopediaData.GetBiomeEntries();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in entries) ids.Add(e.Id);

            Assert.IsTrue(ids.Contains("grasslands"), "Missing grasslands");
            Assert.IsTrue(ids.Contains("desert"), "Missing desert");
            Assert.IsTrue(ids.Contains("arctic"), "Missing arctic");
            Assert.IsTrue(ids.Contains("volcanic"), "Missing volcanic");
            Assert.IsTrue(ids.Contains("candy"), "Missing candy");
            Assert.IsTrue(ids.Contains("chinatown"), "Missing chinatown");
            Assert.IsTrue(ids.Contains("clockwork_foundry"), "Missing clockwork_foundry");
            Assert.IsTrue(ids.Contains("sunken_ruins"), "Missing sunken_ruins");
        }

        [Test]
        public void GetWeaponDescription_HarpoonAndFlakAreNotUnknown()
        {
            Assert.AreNotEqual("Unknown weapon.", EncyclopediaContent.GetWeaponDescription("harpoon"));
            Assert.AreNotEqual("Unknown weapon.", EncyclopediaContent.GetWeaponDescription("flak_cannon"));
            Assert.IsFalse(string.IsNullOrEmpty(EncyclopediaContent.GetWeaponDescription("harpoon")));
            Assert.IsFalse(string.IsNullOrEmpty(EncyclopediaContent.GetWeaponDescription("flak_cannon")));
        }

        [Test]
        public void GetWeaponEntries_HarpoonHasPiercingSpecial()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            EncyclopediaEntry harpoon = default;
            foreach (var e in entries) { if (e.Id == "harpoon") { harpoon = e; break; } }
            Assert.IsTrue(harpoon.Stats.ContainsKey("Special"), "Harpoon missing Special stat");
            StringAssert.Contains("Piercing", harpoon.Stats["Special"]);
        }

        [Test]
        public void GetWeaponEntries_FlakCannonHasBurstSpecial()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            EncyclopediaEntry flak = default;
            foreach (var e in entries) { if (e.Id == "flak_cannon") { flak = e; break; } }
            Assert.IsTrue(flak.Stats.ContainsKey("Special"), "Flak Cannon missing Special stat");
            StringAssert.Contains("sub-projectiles", flak.Stats["Special"]);
        }

        [Test]
        public void FormatName_ConvertsUnderscoresToSpaces()
        {
            Assert.AreEqual("Holy Hand Grenade", EncyclopediaData.FormatName("holy_hand_grenade"));
        }

        [Test]
        public void FormatName_CapitalizesFirstLetterOfEachWord()
        {
            Assert.AreEqual("Cannon", EncyclopediaData.FormatName("cannon"));
            Assert.AreEqual("Sticky Bomb", EncyclopediaData.FormatName("sticky_bomb"));
        }

        [Test]
        public void FormatName_HandlesEmptyString()
        {
            Assert.AreEqual("", EncyclopediaData.FormatName(""));
            Assert.IsNull(EncyclopediaData.FormatName(null));
        }

        [Test]
        public void WeaponEntries_MatchConfigCount()
        {
            var entries = EncyclopediaData.GetWeaponEntries(config);
            Assert.AreEqual(config.Weapons.Length, entries.Length);
        }

        [Test]
        public void SkillEntries_MatchConfigCount()
        {
            var entries = EncyclopediaData.GetSkillEntries(config);
            Assert.AreEqual(config.Skills.Length, entries.Length);
        }

        [Test]
        public void GetFactionEntries_Returns5Factions()
        {
            var entries = EncyclopediaData.GetFactionEntries();
            Assert.AreEqual(5, entries.Length);
        }

        [Test]
        public void GetFactionEntries_HasExpectedFactions()
        {
            var entries = EncyclopediaData.GetFactionEntries();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in entries) ids.Add(e.Id);

            Assert.IsTrue(ids.Contains("aethermoor_council"), "Missing aethermoor_council");
            Assert.IsTrue(ids.Contains("verdant_militia"), "Missing verdant_militia");
            Assert.IsTrue(ids.Contains("desert_scavengers"), "Missing desert_scavengers");
            Assert.IsTrue(ids.Contains("frostspire_order"), "Missing frostspire_order");
            Assert.IsTrue(ids.Contains("automata_corps"), "Missing automata_corps");
        }

        [Test]
        public void GetFactionEntries_AllHaveDescriptionsAndStats()
        {
            var entries = EncyclopediaData.GetFactionEntries();
            foreach (var e in entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.Name), $"Faction {e.Id} has no name");
                Assert.IsFalse(string.IsNullOrEmpty(e.Description), $"Faction {e.Id} has no description");
                Assert.IsTrue(e.Stats.ContainsKey("Status"), $"Faction {e.Id} missing Status stat");
            }
        }

        [Test]
        public void GetHistoryEntries_Returns2Events()
        {
            var entries = EncyclopediaData.GetHistoryEntries();
            Assert.AreEqual(2, entries.Length);
        }

        [Test]
        public void GetHistoryEntries_HasExpectedEvents()
        {
            var entries = EncyclopediaData.GetHistoryEntries();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in entries) ids.Add(e.Id);

            Assert.IsTrue(ids.Contains("gear_wars"), "Missing gear_wars");
            Assert.IsTrue(ids.Contains("cogsworth_coup"), "Missing cogsworth_coup");
        }

        [Test]
        public void GetHistoryEntries_AllHaveDescriptionsAndStats()
        {
            var entries = EncyclopediaData.GetHistoryEntries();
            foreach (var e in entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.Name), $"History {e.Id} has no name");
                Assert.IsFalse(string.IsNullOrEmpty(e.Description), $"History {e.Id} has no description");
                Assert.IsTrue(e.Stats.ContainsKey("Era"), $"History {e.Id} missing Era stat");
            }
        }
    }
}
