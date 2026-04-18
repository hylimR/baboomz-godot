using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class IslandModeTerrainTests
    {
        static GameConfig IslandConfig()
        {
            return new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 80,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -5f,
                TerrainMaxHeight = 6f,
                TerrainHillFrequency = 0.02f,
                TerrainFloorDepth = -15f
            };
        }

        [Test]
        public void Generate_IslandMode_CreatesGapsInTerrain()
        {
            var config = IslandConfig();
            var biome = new TerrainBiome
            {
                Name = "Storm at Sea",
                IslandMode = true,
                IslandCount = 3,
                IslandGapWidth = 5f,
                MinHeight = -5f, MaxHeight = 6f, HillFrequency = 0.02f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2
            };

            var terrainFlat = TerrainGenerator.Generate(config, 42);
            var terrainIsland = TerrainGenerator.Generate(config, 42, biome);

            // Island mode terrain should have fewer solid pixels (gaps cut out)
            int solidFlat = 0, solidIsland = 0;
            for (int x = 0; x < terrainFlat.Width; x++)
                for (int y = 0; y < terrainFlat.Height; y++)
                {
                    if (terrainFlat.IsSolid(x, y)) solidFlat++;
                    if (terrainIsland.IsSolid(x, y)) solidIsland++;
                }

            Assert.Less(solidIsland, solidFlat,
                "Island mode should remove terrain pixels (gaps) compared to flat generation");
        }

        [Test]
        public void Generate_IslandMode_IsDeterministic()
        {
            var config = IslandConfig();
            var biome = new TerrainBiome
            {
                Name = "Storm at Sea",
                IslandMode = true,
                IslandCount = 3,
                IslandGapWidth = 4f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2
            };

            var t1 = TerrainGenerator.Generate(config, 55, biome);
            var t2 = TerrainGenerator.Generate(config, 55, biome);

            for (int i = 0; i < t1.Pixels.Length; i++)
                Assert.AreEqual(t1.Pixels[i], t2.Pixels[i],
                    $"Island-mode terrain must be deterministic: pixel mismatch at index {i}");
        }

        [Test]
        public void Generate_IslandMode_RetainsTerrainBeyondGaps()
        {
            var config = IslandConfig();
            var biome = new TerrainBiome
            {
                Name = "Storm at Sea",
                IslandMode = true,
                IslandCount = 2,
                IslandGapWidth = 3f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2
            };

            var terrain = TerrainGenerator.Generate(config, 99, biome);

            // Left-most column and right-most column should still have some terrain
            bool leftHasTerrain = false, rightHasTerrain = false;
            for (int y = 0; y < terrain.Height; y++)
            {
                if (terrain.IsSolid(0, y)) leftHasTerrain = true;
                if (terrain.IsSolid(terrain.Width - 1, y)) rightHasTerrain = true;
            }
            Assert.IsTrue(leftHasTerrain, "Left island edge should retain terrain");
            Assert.IsTrue(rightHasTerrain, "Right island edge should retain terrain");
        }
    }
}
