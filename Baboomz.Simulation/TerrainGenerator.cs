using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Pure C# terrain generator. No Unity dependency.
    /// Uses a simple hash-based noise instead of Mathf.PerlinNoise.
    /// </summary>
    public static class TerrainGenerator
    {
        public static TerrainState Generate(GameConfig config, int seed, TerrainBiome? biome = null)
        {
            float worldWidth = config.TerrainWidth / config.TerrainPPU;
            float halfWorldWidth = worldWidth / 2f;
            float originX = -halfWorldWidth;
            float originY = config.TerrainFloorDepth;

            var terrain = new TerrainState(
                config.TerrainWidth, config.TerrainHeight,
                config.TerrainPPU, originX, originY);

            float ppu = config.TerrainPPU;

            // Fill main terrain columns
            for (int px = 0; px < terrain.Width; px++)
            {
                float worldX = (px / ppu) - halfWorldWidth;
                float height = CalculateHeight(worldX, (int)config.MapWidth,
                    config.TerrainHillFrequency, seed,
                    config.TerrainMinHeight, config.TerrainMaxHeight);

                int pixelHeight = (int)MathF.Round((height - config.TerrainFloorDepth) * ppu);
                pixelHeight = Math.Clamp(pixelHeight, 0, terrain.Height);

                for (int py = 0; py < pixelHeight; py++)
                {
                    terrain.SetSolid(px, py, true);
                }
            }

            // Island mode: cut gaps between terrain segments
            if (biome.HasValue && biome.Value.IslandMode && biome.Value.IslandCount >= 2)
            {
                CutIslandGaps(terrain, config, biome.Value, seed);
            }

            return terrain;
        }

        /// <summary>
        /// Cuts horizontal gaps into the terrain bitmap to create disconnected island segments.
        /// Gap positions are deterministic based on seed.
        /// </summary>
        static void CutIslandGaps(TerrainState terrain, GameConfig config, TerrainBiome biome, int seed)
        {
            int islandCount = biome.IslandCount;
            float gapWidthWorld = biome.IslandGapWidth;
            float ppu = config.TerrainPPU;
            float worldWidth = config.TerrainWidth / ppu;

            // Each island zone is (worldWidth / islandCount) wide
            // Gaps are placed between zones, offset slightly using seed for variety
            for (int gap = 1; gap < islandCount; gap++)
            {
                // Base gap center at zone boundary
                float gapCenterWorld = worldWidth * gap / islandCount;
                // Small seed-based offset to avoid symmetric-looking gaps
                float offset = (Hash(seed + gap * 7, gap) - 0.5f) * (worldWidth / islandCount * 0.3f);
                gapCenterWorld += offset;

                float gapLeft = gapCenterWorld - gapWidthWorld / 2f;
                float gapRight = gapCenterWorld + gapWidthWorld / 2f;

                int pxLeft = Math.Max(0, (int)MathF.Round(gapLeft * ppu));
                int pxRight = Math.Min(terrain.Width - 1, (int)MathF.Round(gapRight * ppu));

                // Clear all pixels in the gap column range
                for (int px = pxLeft; px <= pxRight; px++)
                {
                    for (int py = 0; py < terrain.Height; py++)
                    {
                        terrain.SetSolid(px, py, false);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates terrain height at a world X position using hash-based noise with octave layering.
        /// </summary>
        public static float CalculateHeight(float x, int mapWidth, float hillFrequency, int seed, float minHeight, float maxHeight)
        {
            float halfWidth = mapWidth / 2f;
            float normalizedX = (x + halfWidth) / mapWidth;

            float height = 0f;
            float amplitude = 1f;
            float frequency = hillFrequency;
            float maxValue = 0f;

            for (int i = 0; i < 4; i++)
            {
                height += PerlinNoise(normalizedX * frequency * 10f + seed, seed) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            height /= maxValue;

            return minHeight + (maxHeight - minHeight) * height;
        }

        /// <summary>
        /// Simple value noise approximation. Pure C# replacement for Mathf.PerlinNoise.
        /// Returns 0..1 range. Not identical to Unity's Perlin but produces similar terrain.
        /// </summary>
        static float PerlinNoise(float x, float y)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);
            float xf = x - xi;
            float yf = y - yi;

            // Smoothstep
            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);

            // Hash corners
            float n00 = Hash(xi, yi);
            float n10 = Hash(xi + 1, yi);
            float n01 = Hash(xi, yi + 1);
            float n11 = Hash(xi + 1, yi + 1);

            // Bilinear interpolation
            float nx0 = n00 + (n10 - n00) * u;
            float nx1 = n01 + (n11 - n01) * u;
            return nx0 + (nx1 - nx0) * v;
        }

        /// <summary>
        /// Integer hash that returns 0..1. Based on a simple bit-mixing hash.
        /// </summary>
        static float Hash(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h = h ^ (h >> 16);
            return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }
    }
}
