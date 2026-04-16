using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    // Regression for #122: the HatRenderer color array and draw switch were
    // out of sync with the HatType enum — half the values (6-11) rendered
    // nothing. This test pins the enum's size, names and numeric ordering so
    // any future additions force the renderer to be updated in lockstep.
    [TestFixture]
    public class HatTypeTests
    {
        [Test]
        public void HatType_Has12Values_0ThroughGoldenCrown()
        {
            var values = (HatType[])System.Enum.GetValues(typeof(HatType));
            Assert.AreEqual(12, values.Length,
                "HatType must have exactly 12 values (None + 11 hats). " +
                "If you added a hat, update HatRenderer.HatColors and the _Draw switch too.");
        }

        [TestCase(HatType.None,           0)]
        [TestCase(HatType.TopHat,         1)]
        [TestCase(HatType.AviatorCap,     2)]
        [TestCase(HatType.Crown,          3)]
        [TestCase(HatType.PirateHat,      4)]
        [TestCase(HatType.ChefHat,        5)]
        [TestCase(HatType.VikingHelmet,   6)]
        [TestCase(HatType.WizardHat,      7)]
        [TestCase(HatType.SamuraiHelmet,  8)]
        [TestCase(HatType.DragonCrown,    9)]
        [TestCase(HatType.Halo,          10)]
        [TestCase(HatType.GoldenCrown,   11)]
        public void HatType_EnumValues_AreStable(HatType hat, int expectedIndex)
        {
            Assert.AreEqual(expectedIndex, (int)hat,
                $"HatType.{hat} numeric position must stay at {expectedIndex} — " +
                "the HatRenderer switch relies on this mapping.");
        }
    }
}
