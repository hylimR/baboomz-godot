using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Pure C# terrain data. No Unity dependency.
    /// Pixel format: R=destruction edge, G=earth, B=indestructible, A=solid.
    /// </summary>
    public class TerrainState
    {
        public int Width;
        public int Height;
        public float PixelsPerUnit;
        public float OriginX;  // world X of pixel (0,0)
        public float OriginY;  // world Y of pixel (0,0)

        // RGBA packed as 4 bytes per pixel. Same layout as Color32[].
        // Index = (y * Width + x) * 4; [R, G, B, A]
        public byte[] Pixels;

        public float WorldWidth => Width / PixelsPerUnit;
        public float WorldHeight => Height / PixelsPerUnit;

        public TerrainState(int width, int height, float pixelsPerUnit, float originX, float originY)
        {
            Width = width;
            Height = height;
            PixelsPerUnit = pixelsPerUnit;
            OriginX = originX;
            OriginY = originY;
            Pixels = new byte[width * height * 4];
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsSolid(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return Pixels[(y * Width + x) * 4 + 3] > 0; // A > 0
        }

        public bool IsIndestructible(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            int idx = (y * Width + x) * 4;
            return Pixels[idx + 2] == 255 && Pixels[idx + 3] > 0; // B=255 && A>0
        }

        public bool IsSurface(int x, int y)
        {
            return IsSolid(x, y) && !IsSolid(x, y + 1);
        }

        public void SetSolid(int x, int y, bool solid)
        {
            if (!InBounds(x, y)) return;
            int idx = (y * Width + x) * 4;
            if (solid)
            {
                Pixels[idx] = 0;       // R
                Pixels[idx + 1] = 255; // G (earth)
                Pixels[idx + 2] = 0;   // B
                Pixels[idx + 3] = 255; // A (solid)
            }
            else
            {
                Pixels[idx] = 0;
                Pixels[idx + 1] = 0;
                Pixels[idx + 2] = 0;
                Pixels[idx + 3] = 0;
            }
        }

        public void SetIndestructible(int x, int y, bool indestructible)
        {
            if (!InBounds(x, y)) return;
            int idx = (y * Width + x) * 4;
            if (indestructible)
            {
                Pixels[idx] = 0;
                Pixels[idx + 1] = 255;
                Pixels[idx + 2] = 255; // B=255 indestructible
                Pixels[idx + 3] = 255;
            }
            else
            {
                Pixels[idx + 2] = 0;
            }
        }

        // --- World ↔ Pixel conversion ---

        public int WorldToPixelX(float worldX)
        {
            return (int)MathF.Round((worldX - OriginX) * PixelsPerUnit);
        }

        public int WorldToPixelY(float worldY)
        {
            return (int)MathF.Round((worldY - OriginY) * PixelsPerUnit);
        }

        public float PixelToWorldX(int px) => OriginX + px / PixelsPerUnit;
        public float PixelToWorldY(int py) => OriginY + py / PixelsPerUnit;

        // --- Modification ---

        /// <summary>
        /// Clears a circle. Returns bounding rect (minX, minY, w, h).
        /// </summary>
        public (int minX, int minY, int w, int h) ClearCircle(int cx, int cy, int radius)
        {
            int minX = Math.Max(0, cx - radius);
            int maxX = Math.Min(Width - 1, cx + radius);
            int minY = Math.Max(0, cy - radius);
            int maxY = Math.Min(Height - 1, cy + radius);
            int rSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        int idx = (y * Width + x) * 4;
                        Pixels[idx] = 0;
                        Pixels[idx + 1] = 0;
                        Pixels[idx + 2] = 0;
                        Pixels[idx + 3] = 0;
                    }
                }
            }

            return (minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Clears a circle but skips indestructible pixels.
        /// </summary>
        public (int minX, int minY, int w, int h) ClearCircleDestructible(int cx, int cy, int radius)
        {
            int minX = Math.Max(0, cx - radius);
            int maxX = Math.Min(Width - 1, cx + radius);
            int minY = Math.Max(0, cy - radius);
            int maxY = Math.Min(Height - 1, cy + radius);
            int rSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        int idx = (y * Width + x) * 4;
                        if (Pixels[idx + 2] != 255) // skip indestructible
                        {
                            Pixels[idx] = 0;
                            Pixels[idx + 1] = 0;
                            Pixels[idx + 2] = 0;
                            Pixels[idx + 3] = 0;
                        }
                    }
                }
            }

            return (minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Marks pixels in an annular ring around a crater as destruction edges (R=255).
        /// Only marks solid pixels that weren't destroyed.
        /// </summary>
        public void MarkDestructionEdge(int cx, int cy, int craterRadius, int edgeWidth)
        {
            int outerRadius = craterRadius + edgeWidth;
            int minX = Math.Max(0, cx - outerRadius);
            int maxX = Math.Min(Width - 1, cx + outerRadius);
            int minY = Math.Max(0, cy - outerRadius);
            int maxY = Math.Min(Height - 1, cy + outerRadius);
            int craterSq = craterRadius * craterRadius;
            int outerSq = outerRadius * outerRadius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = (y * Width + x) * 4;
                    if (Pixels[idx + 3] == 0) continue; // skip empty

                    int dx = x - cx, dy = y - cy;
                    int distSq = dx * dx + dy * dy;

                    if (distSq > craterSq && distSq <= outerSq)
                    {
                        Pixels[idx] = 255; // R=255 marks destruction edge
                    }
                }
            }
        }

        /// <summary>
        /// Fills a circle with destructible earth, but only for currently non-solid pixels.
        /// Returns bounding rect (minX, minY, w, h) of the affected area.
        /// Does not overwrite existing solid pixels (preserves indestructible markings).
        /// </summary>
        public (int minX, int minY, int w, int h) FillCircleDestructible(int cx, int cy, int radius)
        {
            int minX = Math.Max(0, cx - radius);
            int maxX = Math.Min(Width - 1, cx + radius);
            int minY = Math.Max(0, cy - radius);
            int maxY = Math.Min(Height - 1, cy + radius);
            int rSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        int idx = (y * Width + x) * 4;
                        if (Pixels[idx + 3] == 0) // only fill currently empty pixels
                        {
                            Pixels[idx] = 0;
                            Pixels[idx + 1] = 255; // G (earth)
                            Pixels[idx + 2] = 0;
                            Pixels[idx + 3] = 255; // A (solid)
                        }
                    }
                }
            }

            return (minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        public void FillRect(int x, int y, int w, int h)
        {
            int minX = Math.Max(0, x);
            int maxX = Math.Min(Width - 1, x + w - 1);
            int minY = Math.Max(0, y);
            int maxY = Math.Min(Height - 1, y + h - 1);

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    int idx = (py * Width + px) * 4;
                    Pixels[idx] = 0;
                    Pixels[idx + 1] = 255;
                    Pixels[idx + 2] = 0;
                    Pixels[idx + 3] = 255;
                }
            }
        }

        public void ClearRectDestructible(int x, int y, int w, int h)
        {
            int minX = Math.Max(0, x);
            int maxX = Math.Min(Width - 1, x + w - 1);
            int minY = Math.Max(0, y);
            int maxY = Math.Min(Height - 1, y + h - 1);

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    int idx = (py * Width + px) * 4;
                    if (Pixels[idx + 2] == 255 && Pixels[idx + 3] == 255) continue;
                    Pixels[idx] = 0;
                    Pixels[idx + 1] = 0;
                    Pixels[idx + 2] = 0;
                    Pixels[idx + 3] = 0;
                }
            }
        }

        public void FillRectIndestructible(int x, int y, int w, int h)
        {
            int minX = Math.Max(0, x);
            int maxX = Math.Min(Width - 1, x + w - 1);
            int minY = Math.Max(0, y);
            int maxY = Math.Min(Height - 1, y + h - 1);

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    int idx = (py * Width + px) * 4;
                    Pixels[idx] = 0;
                    Pixels[idx + 1] = 255;
                    Pixels[idx + 2] = 255;
                    Pixels[idx + 3] = 255;
                }
            }
        }
    }
}
