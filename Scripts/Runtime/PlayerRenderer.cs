using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders a player: sprite, name label, aim line.
    /// Reads from GameState.Players[index] each frame.
    /// </summary>
    public partial class PlayerRenderer : Node2D
    {
        private Sprite2D _body;
        private Line2D _aimLine;
        private Label _nameLabel;
        private int _playerIndex;
        private GameState _state;

        // Damage flash
        private float _lastHealth;
        private float _flashTimer;
        private const float FlashDuration = 0.15f;
        private Color _baseColor;

        // Death
        private bool _deathAnimPlayed;

        public void Init(int index, GameState state)
        {
            _playerIndex = index;
            _state = state;
            ref var p = ref state.Players[index];

            _baseColor = index == 0
                ? new Color(0.2f, 0.4f, 0.9f)
                : new Color(0.9f, 0.2f, 0.2f);
            _lastHealth = p.Health;

            // Body sprite (32x48 colored square for now)
            _body = new Sprite2D();
            _body.Texture = ProceduralSprites.CreateColorRect(32, 48, _baseColor);
            _body.ZIndex = 5;
            AddChild(_body);

            // Aim line
            _aimLine = new Line2D();
            _aimLine.Width = 2f;
            _aimLine.DefaultColor = new Color(1f, 1f, 0f, 0.6f);
            _aimLine.ZIndex = 6;
            _aimLine.AddPoint(Vector2.Zero);
            _aimLine.AddPoint(Vector2.Right * 50f);
            AddChild(_aimLine);

            // Name label
            _nameLabel = new Label();
            _nameLabel.Text = p.Name;
            _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _nameLabel.AddThemeFontSizeOverride("font_size", 11);
            _nameLabel.AddThemeColorOverride("font_color", Colors.White);
            _nameLabel.Position = new Vector2(-30, -45);
            _nameLabel.ZIndex = 10;
            AddChild(_nameLabel);

            // Run after simulation tick (default priority 0)
            ProcessPriority = 50;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;
            ref var p = ref _state.Players[_playerIndex];

            // Hide dead players
            if (p.IsDead)
            {
                if (!_deathAnimPlayed)
                {
                    _deathAnimPlayed = true;
                    // Simple death: just hide. Could add fade/fall later.
                }
                Visible = false;
                return;
            }

            Visible = true;

            // Position (Y-flipped from sim to Godot)
            GlobalPosition = p.Position.ToGodot();

            // Face direction: FacingDirection is 1 (right) or -1 (left)
            _body.FlipH = p.FacingDirection < 0;

            // Aim line direction
            // AimAngle is in radians, 0 = right, positive = up in sim space
            // In Godot Y-down, negate the angle
            float aimLen = 60f;
            float godotAngle = -p.AimAngle;
            Vector2 aimDir = new Vector2(
                Mathf.Cos(godotAngle),
                Mathf.Sin(godotAngle)
            );
            if (p.FacingDirection < 0)
                aimDir = new Vector2(-aimDir.X, aimDir.Y);

            _aimLine.SetPointPosition(0, Vector2.Zero);
            _aimLine.SetPointPosition(1, aimDir * aimLen);

            // Only show aim line for human players
            _aimLine.Visible = !p.IsAI;

            // Damage flash
            if (p.Health < _lastHealth)
            {
                _flashTimer = FlashDuration;
            }
            _lastHealth = p.Health;

            if (_flashTimer > 0)
            {
                _flashTimer -= (float)delta;
                _body.Modulate = Colors.White;
            }
            else
            {
                _body.Modulate = _baseColor;
            }

            // Freeze effect overrides normal color
            if (p.FreezeTimer > 0f)
            {
                _body.Modulate = new Color(0.5f, 0.8f, 1f);
            }
        }
    }
}
