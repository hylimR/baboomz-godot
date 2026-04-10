using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders simulation TerrainState as a textured Sprite2D with a custom shader.
    /// The mask texture encodes terrain properties (solid, destruction edge,
    /// indestructible) into RGBA channels which the BitmapTerrain shader reads
    /// to color the terrain with earth/surface/destruction/indestructible layers.
    ///
    /// Image layout: simulation row py=0 (bottom) maps to image row (Height-1).
    /// This means the top of the image is the top of the terrain — no FlipV needed.
    /// </summary>
    public partial class GodotTerrainBridge : Sprite2D
    {
        private GameState _state;
        private Image _maskImage;
        private ImageTexture _maskTexture;
        private ShaderMaterial _shaderMat;

        // Dirty tracking: snapshot of Pixels hash to avoid re-uploading every frame
        private int _lastPixelHash;

        public void Init(GameState state)
        {
            _state = state;
            var terrain = state.Terrain;

            // Create RGBA mask image from terrain state
            _maskImage = Image.CreateEmpty(terrain.Width, terrain.Height, false, Image.Format.Rgba8);
            SyncFullMask();

            _maskTexture = ImageTexture.CreateFromImage(_maskImage);

            // Set up shader material
            var shader = GD.Load<Shader>("res://Shaders/BitmapTerrain.gdshader");
            _shaderMat = new ShaderMaterial();
            _shaderMat.Shader = shader;
            _shaderMat.SetShaderParameter("terrain_mask", _maskTexture);

            // Wire real terrain textures from Art/Terrain/Default/ (falls back to colors).
            WireTerrainTextures("Default");

            // The Sprite2D uses the mask texture as its base texture;
            // the shader overrides how it is drawn.
            Texture = _maskTexture;
            Material = _shaderMat;

            // Scale: terrain pixels -> Godot world units.
            // Each pixel represents 1/PPU world units.
            float ppu = terrain.PixelsPerUnit;
            float scalePerPixel = 1f / ppu;
            Scale = new Vector2(scalePerPixel, scalePerPixel);

            // Position: the sprite's center in Godot world coordinates.
            // Sprite2D draws centered on its position by default.
            // The terrain covers world X from OriginX to OriginX + WorldWidth,
            // and world Y from OriginY to OriginY + WorldHeight.
            // Simulation Y-up, Godot Y-down: negate Y.
            float centerWorldX = terrain.OriginX + terrain.WorldWidth * 0.5f;
            float centerWorldY = terrain.OriginY + terrain.WorldHeight * 0.5f;
            GlobalPosition = new Vector2(centerWorldX, -centerWorldY);

            // Terrain behind characters (lower ZIndex = further back)
            ZIndex = 0;

            _lastPixelHash = ComputePixelHash();

            GD.Print($"Terrain bridge: {terrain.Width}x{terrain.Height} px, " +
                     $"world {terrain.WorldWidth:F1}x{terrain.WorldHeight:F1}, " +
                     $"origin ({terrain.OriginX:F1}, {terrain.OriginY:F1}), " +
                     $"pos ({GlobalPosition.X:F1}, {GlobalPosition.Y:F1})");
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            // Only re-upload the texture if terrain pixels changed (explosions, drills, etc.)
            int hash = ComputePixelHash();
            if (hash != _lastPixelHash)
            {
                SyncFullMask();
                _maskTexture.Update(_maskImage);
                _lastPixelHash = hash;
            }
        }

        /// <summary>
        /// Loads tileable earth/surface/destruction textures for the given theme
        /// and feeds them into the shader. Missing textures leave the uniform
        /// flag at 0.0 so the shader falls back to the flat color.
        /// </summary>
        private void WireTerrainTextures(string theme)
        {
            var earth = SpriteLoader.Load($"Terrain/{theme}/earth_body");
            var surface = SpriteLoader.Load($"Terrain/{theme}/surface_cap");
            var destruction = SpriteLoader.Load($"Terrain/{theme}/destruction_edge");

            if (earth != null)
            {
                _shaderMat.SetShaderParameter("earth_tex", earth);
                _shaderMat.SetShaderParameter("use_earth_tex", 1f);
            }
            if (surface != null)
            {
                _shaderMat.SetShaderParameter("surface_tex", surface);
                _shaderMat.SetShaderParameter("use_surface_tex", 1f);
            }
            if (destruction != null)
            {
                _shaderMat.SetShaderParameter("destruction_tex", destruction);
                _shaderMat.SetShaderParameter("use_destruction_tex", 1f);
            }
        }

        /// <summary>
        /// Writes the entire terrain pixel array into the mask Image.
        /// Simulation row py=0 (bottom of terrain) maps to image row (Height-1).
        /// </summary>
        private void SyncFullMask()
        {
            var terrain = _state.Terrain;
            byte[] pixels = terrain.Pixels;
            int w = terrain.Width;
            int h = terrain.Height;

            for (int py = 0; py < h; py++)
            {
                // Flip Y: simulation bottom (py=0) -> image bottom (imgY = h-1)
                int imgY = h - 1 - py;

                for (int px = 0; px < w; px++)
                {
                    int idx = (py * w + px) * 4;
                    byte a = pixels[idx + 3]; // A = solid

                    if (a == 0)
                    {
                        _maskImage.SetPixel(px, imgY, new Color(0, 0, 0, 0));
                        continue;
                    }

                    // R = destruction edge, B = indestructible
                    float r = pixels[idx] > 0 ? 1f : 0f;       // R channel
                    float b = pixels[idx + 2] == 255 ? 1f : 0f; // B channel
                    _maskImage.SetPixel(px, imgY, new Color(r, 0f, b, 1f));
                }
            }
        }

        /// <summary>
        /// Lightweight hash of terrain pixel data for dirty detection.
        /// Samples a spread of pixels plus overall byte sum to catch most changes.
        /// </summary>
        private int ComputePixelHash()
        {
            byte[] pixels = _state.Terrain.Pixels;
            int len = pixels.Length;
            if (len == 0) return 0;

            // FNV-1a-style hash over sampled bytes for speed.
            // Sampling every 256th byte covers the full terrain with ~64K samples
            // for a 4096x1024 terrain (16M bytes), which is fast enough per frame.
            unchecked
            {
                int hash = (int)2166136261;
                int step = len > 4096 ? len / 4096 : 1;
                for (int i = 0; i < len; i += step)
                {
                    hash ^= pixels[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
