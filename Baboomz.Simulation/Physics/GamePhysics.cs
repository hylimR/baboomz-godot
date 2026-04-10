using System;

namespace Baboomz.Simulation
{
    public static class GamePhysics
    {
        public const float DefaultGravity = 9.81f;

        public static void ApplyGravity(ref Vec2 velocity, float dt, float gravity = DefaultGravity)
        {
            velocity.y -= gravity * dt;
        }

        public static void ApplyWind(ref Vec2 velocity, float windForce, float dt)
        {
            velocity.x += windForce * dt;
        }

        /// <summary>
        /// Checks if a position is grounded by testing pixels below the position.
        /// Tests multiple pixels across the character's foot width for stability.
        /// </summary>
        public static bool IsGrounded(TerrainState terrain, Vec2 position, float feetWidth = 0.3f)
        {
            // Check center and edges at y - small offset
            float checkY = position.y - 0.1f;
            int py = terrain.WorldToPixelY(checkY);

            int cx = terrain.WorldToPixelX(position.x);
            if (terrain.IsSolid(cx, py)) return true;

            // Check left and right edges
            int lx = terrain.WorldToPixelX(position.x - feetWidth);
            if (terrain.IsSolid(lx, py)) return true;

            int rx = terrain.WorldToPixelX(position.x + feetWidth);
            if (terrain.IsSolid(rx, py)) return true;

            return false;
        }

        /// <summary>
        /// Finds the ground Y position by scanning downward from startY.
        /// Returns the world Y where the surface is, plus a small padding above.
        /// </summary>
        public static float FindGroundY(TerrainState terrain, float worldX, float startY, float padding = 0.5f)
        {
            int px = terrain.WorldToPixelX(worldX);
            int startPy = terrain.WorldToPixelY(startY);

            // Scan downward
            for (int py = startPy; py >= 0; py--)
            {
                if (terrain.IsSolid(px, py))
                {
                    // Found ground — return position just above
                    return terrain.PixelToWorldY(py + 1) + padding;
                }
            }

            // No ground found
            return startY;
        }

        /// <summary>
        /// Raycast through terrain pixels from start to end.
        /// Returns true if a solid pixel is hit, with hitPoint set to the collision location.
        /// Uses Bresenham-style stepping for accuracy.
        /// </summary>
        public static bool RaycastTerrain(TerrainState terrain, Vec2 from, Vec2 to, out Vec2 hitPoint)
        {
            hitPoint = to;

            int x0 = terrain.WorldToPixelX(from.x);
            int y0 = terrain.WorldToPixelY(from.y);
            int x1 = terrain.WorldToPixelX(to.x);
            int y1 = terrain.WorldToPixelY(to.y);

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int steps = Math.Max(dx, dy) + 1;
            for (int i = 0; i < steps; i++)
            {
                if (terrain.IsSolid(x0, y0))
                {
                    hitPoint = new Vec2(terrain.PixelToWorldX(x0), terrain.PixelToWorldY(y0));
                    return true;
                }

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                bool stepX = e2 > -dy;
                bool stepY = e2 < dx;

                // On diagonal steps, check both intermediate pixels
                // to avoid skipping corner terrain
                if (stepX && stepY)
                {
                    if (terrain.IsSolid(x0 + sx, y0))
                    {
                        hitPoint = new Vec2(terrain.PixelToWorldX(x0 + sx), terrain.PixelToWorldY(y0));
                        return true;
                    }
                    if (terrain.IsSolid(x0, y0 + sy))
                    {
                        hitPoint = new Vec2(terrain.PixelToWorldX(x0), terrain.PixelToWorldY(y0 + sy));
                        return true;
                    }
                }

                if (stepX) { err -= dy; x0 += sx; }
                if (stepY) { err += dx; y0 += sy; }
            }

            return false;
        }

        /// <summary>
        /// Clamps position to map bounds.
        /// </summary>
        public static void ClampToMapBounds(ref Vec2 position, float mapMinX, float mapMaxX, float minY)
        {
            position.x = Math.Clamp(position.x, mapMinX, mapMaxX);
            if (position.y < minY)
                position.y = minY;
        }

        /// <summary>
        /// Estimates terrain surface normal at a pixel position by sampling nearby pixels.
        /// Returns a unit vector pointing away from the solid terrain surface.
        /// </summary>
        public static Vec2 EstimateTerrainNormal(TerrainState terrain, Vec2 worldPos, int radius = 3)
        {
            int cx = terrain.WorldToPixelX(worldPos.x);
            int cy = terrain.WorldToPixelY(worldPos.y);

            float nx = 0f, ny = 0f;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (!terrain.IsSolid(cx + dx, cy + dy))
                    {
                        nx += dx;
                        ny += dy;
                    }
                }
            }

            float len = MathF.Sqrt(nx * nx + ny * ny);
            if (len < 0.001f) return new Vec2(0f, 1f); // default: up
            return new Vec2(nx / len, ny / len);
        }

        /// <summary>
        /// Reflects a velocity vector around a surface normal.
        /// </summary>
        public static Vec2 Reflect(Vec2 velocity, Vec2 normal)
        {
            float dot = velocity.x * normal.x + velocity.y * normal.y;
            return new Vec2(velocity.x - 2f * dot * normal.x, velocity.y - 2f * dot * normal.y);
        }

        /// <summary>
        /// Resolves terrain penetration by pushing the entity upward until not inside terrain.
        /// </summary>
        public static void ResolveTerrainPenetration(TerrainState terrain, ref Vec2 position, float maxPush = 2f)
        {
            int px = terrain.WorldToPixelX(position.x);
            int py = terrain.WorldToPixelY(position.y);

            if (!terrain.IsSolid(px, py)) return;

            // Push upward until we find air
            int maxPixels = (int)(maxPush * terrain.PixelsPerUnit);
            for (int i = 1; i <= maxPixels; i++)
            {
                if (!terrain.IsSolid(px, py + i))
                {
                    position.y = terrain.PixelToWorldY(py + i);
                    return;
                }
            }

            // Fallback: scan remaining pixels up to terrain height
            int maxScan = terrain.Height - py;
            for (int i = maxPixels + 1; i <= maxScan; i++)
            {
                if (!terrain.IsSolid(px, py + i))
                {
                    position.y = terrain.PixelToWorldY(py + i);
                    return;
                }
            }

            // Last resort: place above terrain entirely
            position.y = terrain.PixelToWorldY(terrain.Height) + 0.1f;
        }
    }
}
