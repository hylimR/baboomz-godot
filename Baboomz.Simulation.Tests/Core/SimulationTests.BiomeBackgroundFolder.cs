using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- BackgroundFolder regression tests for #142 ---
        //
        // Before #142, ParallaxBackgroundRenderer hardcoded "Backgrounds/Default/" so every
        // biome shared the same green-hills/blue-sky parallax. The renderer now reads
        // state.Biome.BackgroundFolder, so every biome MUST set one — otherwise the fallback
        // to Default silently swallows a biome's unique look.

        [Test]
        public void Biome_AllBiomesHaveNonEmptyBackgroundFolder()
        {
            foreach (var biome in TerrainBiome.All)
            {
                Assert.IsNotNull(biome.BackgroundFolder,
                    $"Biome '{biome.Name}' must set BackgroundFolder (null would load nothing).");
                Assert.IsNotEmpty(biome.BackgroundFolder,
                    $"Biome '{biome.Name}' must set BackgroundFolder (empty would load nothing).");
            }
        }

        [Test]
        public void Biome_BackgroundFolderAssignmentsMatchExpected()
        {
            // Lock in the folder names so future renames are flagged by tests rather than
            // silently routing a biome to the Default fallback.
            AssertBiomeFolder("Grasslands", "Default");
            AssertBiomeFolder("Desert", "Desert");
            AssertBiomeFolder("Arctic", "Arctic");
            AssertBiomeFolder("Volcanic", "Volcanic");
            AssertBiomeFolder("Candy", "Candy");
            AssertBiomeFolder("Chinatown", "Chinatown");
            AssertBiomeFolder("Clockwork Foundry", "Steampunk");
            AssertBiomeFolder("Sunken Ruins", "Sunken");
            AssertBiomeFolder("Storm at Sea", "Storm");
        }

        static void AssertBiomeFolder(string biomeName, string expectedFolder)
        {
            foreach (var biome in TerrainBiome.All)
            {
                if (biome.Name == biomeName)
                {
                    Assert.AreEqual(expectedFolder, biome.BackgroundFolder,
                        $"Biome '{biomeName}' should use folder '{expectedFolder}'.");
                    return;
                }
            }
            Assert.Fail($"Biome '{biomeName}' not found in TerrainBiome.All");
        }
    }
}
