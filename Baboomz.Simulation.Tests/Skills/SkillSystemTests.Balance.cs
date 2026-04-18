using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        [Test]
        public void Mend_Config_CheaperAndFasterForUtilityNiche_Issue211()
        {
            var cfg = new GameConfig();
            SkillDef? mend = null;
            foreach (var s in cfg.Skills)
                if (s.SkillId == "mend") { mend = s; break; }

            Assert.NotNull(mend, "Mend skill missing from GameConfig.Skills");
            Assert.AreEqual(20f, mend!.Value.EnergyCost, 0.001f,
                "Mend EnergyCost should be 20 (reduced from 30)");
            Assert.AreEqual(10f, mend!.Value.Cooldown, 0.001f,
                "Mend Cooldown should be 10s (reduced from 14s)");
            Assert.AreEqual(12f, mend!.Value.Range, 0.001f, "Mend Range unchanged");
            Assert.AreEqual(3f, mend!.Value.Value, 0.001f, "Mend repair radius unchanged");
        }

        [Test]
        public void Jetpack_Config_BuffedForVerticalSpecialist_Issue212()
        {
            var cfg = new GameConfig();
            SkillDef? jetpack = null;
            foreach (var s in cfg.Skills)
                if (s.SkillId == "jetpack") { jetpack = s; break; }

            Assert.NotNull(jetpack, "Jetpack skill missing from GameConfig.Skills");
            Assert.AreEqual(30f, jetpack!.Value.EnergyCost, 0.001f, "Jetpack EnergyCost unchanged");
            Assert.AreEqual(5f, jetpack!.Value.Cooldown, 0.001f,
                "Jetpack Cooldown should be 5s (reduced from 6s)");
            Assert.AreEqual(2f, jetpack!.Value.Duration, 0.001f, "Jetpack Duration unchanged");
            Assert.AreEqual(15f, jetpack!.Value.Value, 0.001f,
                "Jetpack upward force should be 15 (buffed from 12)");
        }

        [Test]
        public void Girder_Config_CheaperAndFasterForUtilityTier_Issue216()
        {
            var cfg = new GameConfig();
            SkillDef? girder = null;
            foreach (var s in cfg.Skills)
                if (s.SkillId == "girder") { girder = s; break; }

            Assert.NotNull(girder, "Girder skill missing from GameConfig.Skills");
            Assert.AreEqual(25f, girder!.Value.EnergyCost, 0.001f,
                "Girder EnergyCost should be 25 (reduced from 30)");
            Assert.AreEqual(12f, girder!.Value.Cooldown, 0.001f,
                "Girder Cooldown should be 12s (reduced from 15s)");
            Assert.AreEqual(12f, girder!.Value.Range, 0.001f, "Girder Range unchanged");
            Assert.AreEqual(4f, girder!.Value.Value, 0.001f, "Girder width unchanged");
        }
    }
}
