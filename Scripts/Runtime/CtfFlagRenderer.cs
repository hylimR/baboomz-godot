using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Visualises Capture the Flag state in world space:
    ///   - one flag-post Sprite at each team's HomePosition
    ///   - a triangular pennant drawn via _Draw that follows the carrier's head
    ///   - a pulsing dropped-flag indicator at the drop position
    /// Spawns nothing for non-CTF matches; safe to add unconditionally to GameRunner.
    /// </summary>
    public partial class CtfFlagRenderer : Node2D
    {
        private GameState _state;
        private FlagVisual[] _flags;
        private float _pulseTime;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50; // renderer tier
            ZIndex = 12;          // above terrain (0), below explosions (20)

            if (state == null || state.Config == null) return;
            if (state.Config.MatchType != MatchType.CaptureTheFlag) return;
            if (state.Ctf.Flags == null) return;

            _flags = new FlagVisual[state.Ctf.Flags.Length];
            for (int i = 0; i < state.Ctf.Flags.Length; i++)
            {
                var visual = new FlagVisual();
                visual.Name = $"Flag{i}";
                visual.Init(GetTeamColor(i));
                AddChild(visual);
                _flags[i] = visual;
            }
        }

        public override void _Process(double delta)
        {
            if (_flags == null || _state == null) return;
            _pulseTime += (float)delta;

            for (int i = 0; i < _flags.Length; i++)
            {
                var flagState = _state.Ctf.Flags[i];
                _flags[i].UpdateFromState(_state, flagState, _pulseTime);
            }
        }

        private static Color GetTeamColor(int teamIndex) => teamIndex switch
        {
            0 => new Color(0.30f, 0.55f, 1.00f), // blue
            1 => new Color(1.00f, 0.30f, 0.30f), // red
            _ => new Color(0.85f, 0.85f, 0.85f),
        };
    }

    /// <summary>
    /// Single-flag visual: home post (always visible), pennant drawn as a
    /// waving triangle via _Draw, and a pulsing ring overlay shown only when
    /// the flag is dropped. The pennant follows the carrier's head when held.
    /// </summary>
    public partial class FlagVisual : Node2D
    {
        private Sprite2D _post;
        private FlagPennant _pennant;
        private Node2D _dropIndicator;
        private Color _teamColor;
        private float _baseScale = 0.6f;

        public void Init(Color teamColor)
        {
            _teamColor = teamColor;

            // Home post: 3 world units tall, slim white pixel rect tinted brown.
            _post = new Sprite2D();
            _post.Name = "Post";
            _post.Texture = ProceduralSprites.WhitePixel;
            _post.Centered = false;
            _post.Scale = new Vector2(0.15f, 3f);
            _post.SelfModulate = new Color(0.6f, 0.45f, 0.25f, 1f);
            AddChild(_post);

            // Pennant: triangular flag drawn via _Draw with a subtle wave.
            _pennant = new FlagPennant { TeamColor = teamColor };
            _pennant.Name = "Pennant";
            AddChild(_pennant);

            // Drop indicator: pulsing dot rendered via _Draw on a child node.
            _dropIndicator = new DropPulseIndicator { TeamColor = teamColor };
            _dropIndicator.Name = "DropPulse";
            _dropIndicator.Visible = false;
            AddChild(_dropIndicator);
        }

        public void UpdateFromState(GameState state, FlagState flagState, float pulseTime)
        {
            // Home post stays at HomePosition.
            var home = flagState.HomePosition.ToGodot();
            _post.GlobalPosition = home + new Vector2(0f, -3f); // grow upward

            // Pennant: where to draw it depends on state.
            Vector2 flagWorld;
            if (flagState.CarrierIndex >= 0
                && flagState.CarrierIndex < state.Players.Length)
            {
                ref var carrier = ref state.Players[flagState.CarrierIndex];
                flagWorld = carrier.Position.ToGodot() + new Vector2(0f, -2.0f);
            }
            else
            {
                flagWorld = flagState.Position.ToGodot();
            }
            // Anchor pennant at the top of the post; the triangle extends to the right.
            _pennant.GlobalPosition = flagWorld + new Vector2(0.08f, -2.8f);
            _pennant.WaveTime = pulseTime;
            _pennant.QueueRedraw();

            // Drop indicator: pulse only while dropped (carrier == -1 and not at home).
            bool dropped = flagState.CarrierIndex < 0 && !flagState.IsHome;
            _dropIndicator.Visible = dropped;
            if (dropped)
            {
                _dropIndicator.GlobalPosition = flagState.Position.ToGodot();
                if (_dropIndicator is DropPulseIndicator pulser)
                    pulser.PulseTime = pulseTime;
            }
        }

        // Avoid CS0414 unused warning while keeping a tunable for future scale work.
        public float BaseScale { get => _baseScale; set => _baseScale = value; }
    }

    /// <summary>
    /// Triangular team-colored pennant drawn via _Draw. A subtle wave on the
    /// trailing tip sells it as a fabric flag rather than a flat rectangle.
    /// </summary>
    public partial class FlagPennant : Node2D
    {
        public Color TeamColor = Colors.White;
        public float WaveTime;

        // Pennant geometry (world units). Pole-side edge is vertical, tip waves.
        private const float Height = 1.2f;
        private const float Length = 1.8f;

        public override void _Draw()
        {
            // Pole-side top and bottom corners (fixed at the anchor).
            var top = new Vector2(0f, 0f);
            var bottom = new Vector2(0f, Height);

            // Trailing tip: the meeting point of the triangle, waved vertically.
            float wave = Mathf.Sin(WaveTime * 4f) * 0.15f;
            var tip = new Vector2(Length, Height * 0.5f + wave);

            var points = new Vector2[] { top, bottom, tip };
            DrawColoredPolygon(points, TeamColor);

            // Dark outline so the flag reads against any background.
            var outline = new Color(TeamColor.R * 0.4f, TeamColor.G * 0.4f,
                TeamColor.B * 0.4f, 1f);
            DrawLine(top, tip, outline, 0.08f);
            DrawLine(bottom, tip, outline, 0.08f);
            DrawLine(top, bottom, outline, 0.08f);
        }
    }

    /// <summary>Pulses a circle outline to mark a dropped flag.</summary>
    public partial class DropPulseIndicator : Node2D
    {
        public Color TeamColor = Colors.White;
        public float PulseTime;

        public override void _Process(double delta) => QueueRedraw();

        public override void _Draw()
        {
            // 0.5 .. 1.5 world-unit radius pulse, fades over the cycle.
            float t = (Mathf.Sin(PulseTime * 4f) + 1f) * 0.5f; // 0..1
            float radius = 0.5f + t * 1.0f;
            float alpha = 0.7f - 0.5f * t;
            var c = new Color(TeamColor.R, TeamColor.G, TeamColor.B, alpha);
            DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 24, c, 0.15f);
        }
    }
}
