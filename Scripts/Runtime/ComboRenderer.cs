using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Renders combo event indicators (Double Kill, Multi Kill, hit streaks)
    /// as floating text that rises and fades. ProcessPriority = 55.
    /// </summary>
    public partial class ComboRenderer : Node2D
    {
        private GameState _state;
        private readonly List<ComboLabel> _activeLabels = new();

        private struct ComboLabel
        {
            public Label Node;
            public float Lifetime;
            public float MaxLifetime;
            public Vector2 StartPos;
        }

        private const float FloatSpeed = 60f;  // pixels per second upward
        private const float DefaultDuration = 1.5f;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 55;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;
            float dt = (float)delta;

            // Spawn new combo labels
            foreach (var evt in _state.ComboEvents)
            {
                SpawnComboLabel(evt);
            }

            // Update existing labels (float up + fade)
            for (int i = _activeLabels.Count - 1; i >= 0; i--)
            {
                var lbl = _activeLabels[i];
                lbl.Lifetime -= dt;

                if (lbl.Lifetime <= 0f)
                {
                    lbl.Node.QueueFree();
                    _activeLabels.RemoveAt(i);
                    continue;
                }

                float progress = 1f - (lbl.Lifetime / lbl.MaxLifetime);
                float yOffset = FloatSpeed * lbl.MaxLifetime * progress;
                lbl.Node.Position = lbl.StartPos + new Vector2(0f, -yOffset);

                // Fade out in the last 40%
                float alpha = lbl.Lifetime < lbl.MaxLifetime * 0.4f
                    ? lbl.Lifetime / (lbl.MaxLifetime * 0.4f)
                    : 1f;
                lbl.Node.Modulate = new Color(1f, 1f, 1f, alpha);

                _activeLabels[i] = lbl;
            }
        }

        private void SpawnComboLabel(ComboEvent evt)
        {
            string text;
            int fontSize;
            Color color;

            switch (evt.Type)
            {
                case ComboType.DoubleHit:
                    text = "x2 COMBO!";
                    fontSize = 28;
                    color = new Color(1f, 0.9f, 0.3f); // yellow
                    break;
                case ComboType.TripleHit:
                    text = "x3 COMBO!";
                    fontSize = 32;
                    color = new Color(1f, 0.7f, 0.2f); // orange
                    break;
                case ComboType.QuadHit:
                    text = "x4 COMBO!";
                    fontSize = 34;
                    color = new Color(1f, 0.5f, 0.1f); // deep orange
                    break;
                case ComboType.Unstoppable:
                    text = "UNSTOPPABLE!";
                    fontSize = 38;
                    color = new Color(1f, 0.2f, 0.2f); // red
                    break;
                case ComboType.DoubleKill:
                    text = "DOUBLE KILL!";
                    fontSize = 36;
                    color = new Color(1f, 0.6f, 0.1f); // orange
                    break;
                case ComboType.MultiKill:
                    text = "MULTI KILL!";
                    fontSize = 42;
                    color = new Color(1f, 0.15f, 0.15f); // bright red
                    break;
                default:
                    return;
            }

            // Position at the player who earned the combo
            Vector2 worldPos = Vector2.Zero;
            if (evt.PlayerIndex >= 0 && evt.PlayerIndex < _state.Players.Length)
            {
                var p = _state.Players[evt.PlayerIndex].Position;
                worldPos = p.ToGodot() + new Vector2(0f, -40f); // above player head
            }

            var label = new Label();
            label.Text = text;
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
            label.AddThemeConstantOverride("outline_size", 3);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.Position = worldPos;
            label.ZIndex = 100;
            AddChild(label);

            _activeLabels.Add(new ComboLabel
            {
                Node = label,
                Lifetime = DefaultDuration,
                MaxLifetime = DefaultDuration,
                StartPos = worldPos
            });
        }
    }
}
