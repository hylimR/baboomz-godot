using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class TerrainGeneratorTests
    {
        [Test]
        public void Generate_ProducesSolidTerrain()
        {
            var config = new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f
            };

            var terrain = TerrainGenerator.Generate(config, 42);

            // Should have some solid pixels
            int solidCount = 0;
            for (int x = 0; x < terrain.Width; x++)
                for (int y = 0; y < terrain.Height; y++)
                    if (terrain.IsSolid(x, y)) solidCount++;

            Assert.Greater(solidCount, 0, "Terrain should have solid pixels");
            Assert.Less(solidCount, terrain.Width * terrain.Height, "Terrain should not be entirely solid");
        }

        [Test]
        public void Generate_DeterministicWithSameSeed()
        {
            var config = new GameConfig
            {
                TerrainWidth = 160,
                TerrainHeight = 80,
                TerrainPPU = 8f,
                MapWidth = 20f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f
            };

            var t1 = TerrainGenerator.Generate(config, 123);
            var t2 = TerrainGenerator.Generate(config, 123);

            // Should produce identical terrain
            for (int i = 0; i < t1.Pixels.Length; i++)
                Assert.AreEqual(t1.Pixels[i], t2.Pixels[i], $"Pixel mismatch at index {i}");
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentTerrain()
        {
            var config = new GameConfig
            {
                TerrainWidth = 160,
                TerrainHeight = 80,
                TerrainPPU = 8f,
                MapWidth = 20f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f
            };

            var t1 = TerrainGenerator.Generate(config, 111);
            var t2 = TerrainGenerator.Generate(config, 222);

            bool anyDifference = false;
            for (int i = 0; i < t1.Pixels.Length; i++)
            {
                if (t1.Pixels[i] != t2.Pixels[i]) { anyDifference = true; break; }
            }
            Assert.IsTrue(anyDifference, "Different seeds should produce different terrain");
        }
    }
}
