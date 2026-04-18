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
    }
}
