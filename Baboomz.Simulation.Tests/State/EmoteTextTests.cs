using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class EmoteTextTests
    {
        [Test]
        public void Get_None_ReturnsNull()
        {
            Assert.IsNull(EmoteText.Get(EmoteType.None));
        }

        // Regression for #123: Dance and Flex were missing from the renderer
        // switch and silently dropped. The mapping now lives in the simulation
        // layer so this test can pin all 8 non-None emotes to their bubble text.
        [TestCase(EmoteType.Taunt,    "Ha!")]
        [TestCase(EmoteType.Laugh,    "Haha!")]
        [TestCase(EmoteType.Cry,      "Nooo!")]
        [TestCase(EmoteType.ThumbsUp, "GG!")]
        [TestCase(EmoteType.Clap,     "Bravo!")]
        [TestCase(EmoteType.Salute,   "Sir!")]
        [TestCase(EmoteType.Dance,    "Woo!")]
        [TestCase(EmoteType.Flex,     "Flex!")]
        public void Get_AllNonNoneEmotes_ReturnExpectedText(EmoteType type, string expected)
        {
            Assert.AreEqual(expected, EmoteText.Get(type));
        }

        [Test]
        public void Get_EveryDefinedEmoteHasText_ExceptNone()
        {
            foreach (EmoteType type in System.Enum.GetValues(typeof(EmoteType)))
            {
                if (type == EmoteType.None)
                {
                    Assert.IsNull(EmoteText.Get(type), "None must return null");
                    continue;
                }

                string text = EmoteText.Get(type);
                Assert.IsFalse(string.IsNullOrEmpty(text),
                    $"EmoteType.{type} must have non-empty bubble text (missing case in EmoteText.Get)");
            }
        }
    }
}
