using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Builds layered parallax backgrounds (sky, far mountains, drifting clouds,
    /// near hills) behind the terrain. Tracks the camera manually so each layer
    /// scrolls at its own speed. Falls back to solid fill rectangles when any
    /// asset is missing so the scene never goes black.
    /// </summary>
    public partial class ParallaxBackgroundRenderer : Node2D
    {
        private const string DefaultFolder = "Default";
        // Scroll scales (0 = static, 1 = moves 1:1 with camera).
        private const float SkyScroll      = 0.00f;
        private const float MountainScroll = 0.10f;
        private const float CloudScroll    = 0.20f;
        private const float HillScroll     = 0.40f;

        // Clouds drift right at a constant speed (world units/sec).
        private const float CloudDriftSpeed = 1.5f;

        // Per-layer world-space size, in world units. Tuned for the default 4x camera zoom
        // (visible area ~480x270 world units centered on the player).
        private const float LayerWorldWidth = 480f;

        private ParallaxLayerNode _sky;
        private ParallaxLayerNode _mountains;
        private ParallaxLayerNode _clouds;
        private ParallaxLayerNode _hills;

        private Camera2D _cam;
        private float _cloudDriftAccum;

        /// <summary>
        /// Initialise the parallax stack. When <paramref name="state"/> is non-null we look up
        /// the current biome's BackgroundFolder (e.g. "Desert", "Arctic", "Steampunk") and load
        /// assets from Art/Backgrounds/&lt;folder&gt;/. Any missing per-biome layer silently falls
        /// back to Art/Backgrounds/Default/ so we never regress from the shipped look.
        /// </summary>
        public void Init(GameState state = null)
        {
            ZIndex = -50;
            ZAsRelative = false;

            string folder = ResolveBiomeFolder(state);

            // Y offsets in Godot Y-down space. Sky spans the whole screen; far layers sit
            // at upper/mid; hills sit just behind the terrain (~y = 0 world).
            _sky       = CreateLayer("Sky",       folder, "sky_gradient",  SkyScroll,      new Color(0.55f, 0.80f, 1.00f), -53, yOffset:   0f, heightWorld: 280f, stretchFill: true);
            _mountains = CreateLayer("Mountains", folder, "mountains_far", MountainScroll, new Color(0.40f, 0.45f, 0.60f), -52, yOffset: -35f, heightWorld:  60f, stretchFill: false);
            _clouds    = CreateLayer("Clouds",    folder, "clouds_layer",  CloudScroll,    new Color(1f, 1f, 1f, 0f),      -51, yOffset: -80f, heightWorld:  40f, stretchFill: false);
            _hills     = CreateLayer("Hills",     folder, "hills_near",    HillScroll,     new Color(0.35f, 0.55f, 0.30f), -50, yOffset:  20f, heightWorld:  50f, stretchFill: false);
        }

        internal static string ResolveBiomeFolder(GameState state)
        {
            if (state == null) return DefaultFolder;
            string f = state.Biome.BackgroundFolder;
            return string.IsNullOrEmpty(f) ? DefaultFolder : f;
        }

        public override void _Process(double delta)
        {
            _cam ??= GetViewport()?.GetCamera2D();
            if (_cam == null) return;

            Vector2 camPos = _cam.GlobalPosition;
            _cloudDriftAccum += CloudDriftSpeed * (float)delta;

            UpdateLayer(_sky,       camPos, 0f);
            UpdateLayer(_mountains, camPos, 0f);
            UpdateLayer(_clouds,    camPos, _cloudDriftAccum);
            UpdateLayer(_hills,     camPos, 0f);
        }

        private static void UpdateLayer(ParallaxLayerNode layer, Vector2 camPos, float driftX)
        {
            if (layer == null) return;

            // Layer position is camera * (1 - scroll_scale) — this produces the
            // classic parallax feel: scroll=0 sticks to camera, scroll=1 is world-fixed.
            float lockFactor = 1f - layer.ScrollScale;
            layer.Position = new Vector2(
                camPos.X * lockFactor + driftX,
                camPos.Y * lockFactor + layer.YOffset);
        }

        private ParallaxLayerNode CreateLayer(
            string name, string folder, string asset, float scrollScale, Color fallbackColor, int zIndex,
            float yOffset, float heightWorld, bool stretchFill)
        {
            var layer = new ParallaxLayerNode();
            layer.Name = name;
            layer.ScrollScale = scrollScale;
            layer.YOffset = yOffset;
            layer.ZIndex = zIndex;
            layer.ZAsRelative = false;
            AddChild(layer);

            // Try the biome-specific asset first, then fall back to Default before giving up.
            var tex = SpriteLoader.Load($"Backgrounds/{folder}/{asset}");
            if (tex == null && folder != DefaultFolder)
                tex = SpriteLoader.Load($"Backgrounds/{DefaultFolder}/{asset}");
            if (tex != null)
            {
                float texW = tex.GetWidth();
                float texH = tex.GetHeight();
                float scaleX = LayerWorldWidth / texW;
                // stretchFill = true: force the sprite to exactly heightWorld (used for the sky
                // so it fills the full visible area). false: keep aspect ratio.
                float scaleY = stretchFill ? heightWorld / texH : scaleX;

                // Three tiled copies (left / center / right) so panning never reveals an edge.
                for (int i = -1; i <= 1; i++)
                {
                    var sprite = new Sprite2D();
                    sprite.Name = $"Tile{i}";
                    sprite.Texture = tex;
                    sprite.Centered = true;
                    sprite.Scale = new Vector2(scaleX, scaleY);
                    sprite.Position = new Vector2(i * LayerWorldWidth, 0);
                    layer.AddChild(sprite);
                }

                layer.TileWidth = LayerWorldWidth;
            }
            else if (fallbackColor.A > 0f)
            {
                var rect = new ColorRect();
                rect.Name = "Fallback";
                rect.Color = fallbackColor;
                rect.Size = new Vector2(LayerWorldWidth * 3f, heightWorld);
                rect.Position = new Vector2(-LayerWorldWidth * 1.5f, -heightWorld * 0.5f);
                layer.AddChild(rect);
            }

            return layer;
        }
    }

    /// <summary>Simple container node that stores parallax parameters per layer.</summary>
    public partial class ParallaxLayerNode : Node2D
    {
        public float ScrollScale;
        public float YOffset;
        public float TileWidth;
    }
}
