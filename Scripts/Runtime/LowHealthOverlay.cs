using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Edge vignette that pulses when the local player drops below
    /// LowHealthFraction of max HP. Only darkens the screen edges — the
    /// center of the screen stays clear so the player can still aim.
    /// Also triggers a one-shot heartbeat audio cue on threshold entry.
    /// Lives on a CanvasLayer above the game world but below the HUD.
    /// </summary>
    public partial class LowHealthOverlay : CanvasLayer
    {
        private const float LowHealthFraction = 0.25f;

        // Shader draws a radial vignette that only darkens the edges of the screen.
        // Center (uv=0.5) is fully transparent; corners (uv>=0.85) reach max alpha.
        private const string VignetteShader = @"
shader_type canvas_item;

uniform vec4 edge_color : source_color = vec4(0.85, 0.05, 0.05, 1.0);
uniform float strength : hint_range(0.0, 1.0) = 0.0;
uniform float inner_radius : hint_range(0.0, 1.0) = 0.35;
uniform float outer_radius : hint_range(0.0, 1.0) = 0.95;

void fragment()
{
    vec2 centered = UV - vec2(0.5);
    // Correct for aspect ratio so the vignette is circular even on 16:9.
    float d = length(centered * vec2(1.78, 1.0));
    float v = smoothstep(inner_radius, outer_radius, d);
    COLOR = vec4(edge_color.rgb, v * strength);
}
";

        private GameState _state;
        private ColorRect _rect;
        private ShaderMaterial _material;
        private AudioBridge _audio;
        private bool _wasLowHp;

        public void Init(GameState state, AudioBridge audio = null)
        {
            _state = state;
            _audio = audio;
            // Layer just below the HUD layer (HUD = 10).
            Layer = 9;
            ProcessPriority = 60;

            var shader = new Shader();
            shader.Code = VignetteShader;

            _material = new ShaderMaterial();
            _material.Shader = shader;
            _material.SetShaderParameter("edge_color", new Color(0.85f, 0.05f, 0.05f, 1f));
            _material.SetShaderParameter("strength", 0f);
            _material.SetShaderParameter("inner_radius", 0.35f);
            _material.SetShaderParameter("outer_radius", 0.95f);

            _rect = new ColorRect();
            _rect.Name = "LowHpVignette";
            _rect.Color = Colors.White; // shader controls output
            _rect.Material = _material;
            _rect.MouseFilter = Control.MouseFilterEnum.Ignore;
            _rect.AnchorLeft = 0f;
            _rect.AnchorTop = 0f;
            _rect.AnchorRight = 1f;
            _rect.AnchorBottom = 1f;
            AddChild(_rect);
        }

        public override void _Process(double delta)
        {
            if (_state == null || _rect == null || _material == null) return;
            if (_state.Players == null || _state.Players.Length == 0)
            {
                SetStrength(0f);
                _wasLowHp = false;
                return;
            }

            ref var p = ref _state.Players[0];
            if (p.IsDead || p.MaxHealth <= 0f || _state.Phase != MatchPhase.Playing)
            {
                SetStrength(0f);
                _wasLowHp = false;
                return;
            }

            float hpFrac = p.Health / p.MaxHealth;
            bool lowHp = hpFrac < LowHealthFraction;

            if (!lowHp)
            {
                SetStrength(0f);
                _wasLowHp = false;
                return;
            }

            // First-frame crossing into low HP: play the one-shot heartbeat cue.
            if (!_wasLowHp && _audio != null)
            {
                _audio.PlayLowHealthCue();
            }
            _wasLowHp = true;

            // Stronger pulse the lower the HP. Range: ~0.25..0.75 vignette strength.
            float severity = 1f - (hpFrac / LowHealthFraction);
            float pulse = (Mathf.Sin(_state.Time * 6f) + 1f) * 0.5f;
            float strength = 0.25f + 0.5f * Mathf.Clamp(severity, 0f, 1f) * pulse;
            SetStrength(strength);
        }

        private void SetStrength(float strength)
        {
            _material.SetShaderParameter("strength", strength);
        }
    }
}
