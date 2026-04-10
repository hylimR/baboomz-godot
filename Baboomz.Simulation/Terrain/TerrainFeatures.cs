using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Stamps terrain features (caves, bridges, plateaus, floating islands) onto generated terrain.
    /// Called after TerrainGenerator.Generate(), before spawn point calculation.
    /// </summary>
    public static class TerrainFeatures
    {
        public static void StampFeatures(TerrainState terrain, GameConfig config, int seed)
        {
            var rng = new Random(seed ^ 0x5A5A5A5A);
            float halfMap = config.MapWidth / 2f;

            // Cave: 50% chance, on hills > 10 units tall
            if (rng.NextDouble() < 0.5)
                TryStampCave(terrain, config, rng, halfMap);

            // Bridge: 50% chance
            if (rng.NextDouble() < 0.5)
                TryStampBridge(terrain, config, rng, halfMap);

            // Plateaus: 0-2
            int plateauCount = rng.Next(0, 3);
            for (int i = 0; i < plateauCount; i++)
                TryStampPlateau(terrain, config, rng, halfMap);

            // Floating island: 30% chance, one per map
            if (rng.NextDouble() < 0.3)
                TryStampFloatingIsland(terrain, config, rng, halfMap);
        }

        static void TryStampCave(TerrainState terrain, GameConfig config, Random rng, float halfMap)
        {
            // Find a location with sufficient terrain depth to carve a cave
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float x = (float)(rng.NextDouble() * (config.MapWidth - 20f) - halfMap + 10f);
                float groundY = GamePhysics.FindGroundY(terrain, x, config.SpawnProbeY, 0.1f);

                // Cave should be below ground surface with at least 4 units of terrain above
                float caveY = groundY - 4f - (float)rng.NextDouble() * 3f; // 4-7 units below surface
                if (caveY < config.TerrainMinHeight + 2f) continue; // too close to bottom
                float caveWidth = 8f + (float)rng.NextDouble() * 4f;  // 8-12 units
                float caveHeight = 3f + (float)rng.NextDouble() * 1f; // 3-4 units

                int px = terrain.WorldToPixelX(x - caveWidth / 2f);
                int py = terrain.WorldToPixelY(caveY);
                int pw = (int)(caveWidth * terrain.PixelsPerUnit);
                int ph = (int)(caveHeight * terrain.PixelsPerUnit);

                // Clear the cave interior
                for (int cy = py; cy < py + ph && cy < terrain.Height; cy++)
                    for (int cx = px; cx < px + pw && cx < terrain.Width; cx++)
                        if (cx >= 0 && cy >= 0)
                            terrain.SetSolid(cx, cy, false);

                return; // one cave max
            }
        }

        static void TryStampBridge(TerrainState terrain, GameConfig config, Random rng, float halfMap)
        {
            // Find two adjacent peaks with a gap between them
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float x1 = (float)(rng.NextDouble() * (config.MapWidth - 20f) - halfMap + 10f);
                float x2 = x1 + 6f + (float)rng.NextDouble() * 4f; // 6-10 unit gap
                if (x2 > halfMap - 5f) continue;

                float y1 = GamePhysics.FindGroundY(terrain, x1, config.SpawnProbeY, 0.1f);
                float y2 = GamePhysics.FindGroundY(terrain, x2, config.SpawnProbeY, 0.1f);

                // Both sides need reasonable height
                if (y1 < config.TerrainMinHeight + 3f || y2 < config.TerrainMinHeight + 3f) continue;

                float bridgeY = Math.Min(y1, y2); // bridge at lower peak height
                float bridgeThickness = 2f + (float)rng.NextDouble() * 1f; // 2-3 units thick

                int px1 = terrain.WorldToPixelX(x1);
                int px2 = terrain.WorldToPixelX(x2);
                int py = terrain.WorldToPixelY(bridgeY);
                int ph = (int)(bridgeThickness * terrain.PixelsPerUnit);

                // Fill bridge pixels
                for (int cy = py; cy < py + ph && cy < terrain.Height; cy++)
                    for (int cx = px1; cx <= px2 && cx < terrain.Width; cx++)
                        if (cx >= 0 && cy >= 0)
                            terrain.SetSolid(cx, cy, true);

                return; // one bridge max
            }
        }

        static void TryStampPlateau(TerrainState terrain, GameConfig config, Random rng, float halfMap)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float x = (float)(rng.NextDouble() * (config.MapWidth - 16f) - halfMap + 8f);
                float groundY = GamePhysics.FindGroundY(terrain, x, config.SpawnProbeY, 0.1f);

                if (groundY < config.TerrainMinHeight + 2f) continue;

                float plateauWidth = 6f + (float)rng.NextDouble() * 4f; // 6-10 units
                float plateauHeight = groundY + 2f + (float)rng.NextDouble() * 2f; // 2-4 units above ground

                int px = terrain.WorldToPixelX(x - plateauWidth / 2f);
                int py = terrain.WorldToPixelY(groundY);
                int pyTop = terrain.WorldToPixelY(plateauHeight);
                int pw = (int)(plateauWidth * terrain.PixelsPerUnit);

                // Fill from ground level up to plateau height
                for (int cy = py; cy <= pyTop && cy < terrain.Height; cy++)
                    for (int cx = px; cx < px + pw && cx < terrain.Width; cx++)
                        if (cx >= 0 && cy >= 0)
                            terrain.SetSolid(cx, cy, true);

                return; // one plateau per attempt
            }
        }

        static void TryStampFloatingIsland(TerrainState terrain, GameConfig config, Random rng, float halfMap)
        {
            float edgeMargin = 10f;
            float spawnMargin = 8f;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Center-biased X: within edge margins
                float x = (float)(rng.NextDouble() * (config.MapWidth - edgeMargin * 2f) - halfMap + edgeMargin);

                // Must be away from both spawn points
                if (Math.Abs(x - config.Player1SpawnX) < spawnMargin) continue;
                if (Math.Abs(x - config.Player2SpawnX) < spawnMargin) continue;

                float groundY = GamePhysics.FindGroundY(terrain, x, config.SpawnProbeY, 0.1f);
                if (groundY < config.TerrainMinHeight + 2f) continue;

                // Island dimensions: 8-14 wide, 2-3 thick, 8-12 above local ground
                float islandWidth = 8f + (float)rng.NextDouble() * 6f;
                float islandThickness = 2f + (float)rng.NextDouble() * 1f;
                float islandY = groundY + 8f + (float)rng.NextDouble() * 4f;

                // Ensure island fits within terrain bounds
                float terrainTop = terrain.OriginY + terrain.Height / terrain.PixelsPerUnit;
                if (islandY + islandThickness > terrainTop) continue;

                int px = terrain.WorldToPixelX(x - islandWidth / 2f);
                int py = terrain.WorldToPixelY(islandY);
                int pw = (int)(islandWidth * terrain.PixelsPerUnit);
                int ph = (int)(islandThickness * terrain.PixelsPerUnit);

                // Fill island pixels (standard destructible terrain)
                for (int cy = py; cy < py + ph && cy < terrain.Height; cy++)
                    for (int cx = px; cx < px + pw && cx < terrain.Width; cx++)
                        if (cx >= 0 && cy >= 0)
                            terrain.SetSolid(cx, cy, true);

                return; // one island max
            }
        }
    }
}
