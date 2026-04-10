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
        private const float FlashDuration = 0.2f;
        private Color _baseColor;

        // Death / victory animation
        private bool _deathAnimStarted;
        private float _deathTimer;
        private bool _victoryAnimActive;
        private float _victoryAnimTimer;

        // Animation sprites
        private Texture2D _idleTex;
        private Texture2D[] _walkTex;
        private Texture2D _jumpTex;
        private Texture2D _aimTex;
        private int _walkFrame;
        private float _walkTimer;
        private const float WalkFPS = 8f;
        private bool _hasRealSprites;

        // Target character height in pixels; real art is scaled to fit.
        private const float TargetHeight = 64f;

        public void Init(int index, GameState state)
        {
            _playerIndex = index;
            _state = state;
            ref var p = ref state.Players[index];
            _lastHealth = p.Health;

            _body = new Sprite2D();
            _body.ZIndex = 5;
            AddChild(_body);

            // Try real character art, fall back to colored rectangle
            string group = index == 0 ? "Player" : "Opponent";
            var character = SpriteLoader.LoadCharacter(group);

            if (character.idle != null)
            {
                _hasRealSprites = true;
                _idleTex = character.idle;
                _walkTex = character.walk;
                _jumpTex = character.jump;
                _aimTex = character.aim;
                _body.Texture = character.idle;
                _baseColor = Colors.White;

                // Scale oversized source art (e.g. 512px) down to target height.
                float srcHeight = character.idle.GetHeight();
                if (srcHeight > 0f)
                {
                    float s = TargetHeight / srcHeight;
                    _body.Scale = new Vector2(s, s);
                }
            }
            else
            {
                _hasRealSprites = false;
                _baseColor = index == 0
                    ? new Color(0.2f, 0.4f, 0.9f)
                    : new Color(0.9f, 0.2f, 0.2f);
                _body.Texture = ProceduralSprites.CreateColorRect(32, 48, _baseColor);
            }

            // Aim line
            _aimLine = new Line2D();
            _aimLine.Width = 2f;
            _aimLine.DefaultColor = new Color(1f, 1f, 0f, 0.6f);
            _aimLine.ZIndex = 6;
            _aimLine.AddPoint(Vector2.Zero);
            _aimLine.AddPoint(Vector2.Right * 50f);
            AddChild(_aimLine);

            // Name label — stagger vertically per-index so two players spawning
            // close together don't stack their labels on top of each other (#9).
            _nameLabel = new Label();
            _nameLabel.Text = p.Name;
            _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _nameLabel.AddThemeFontSizeOverride("font_size", 9);
            _nameLabel.AddThemeColorOverride("font_color", Colors.White);
            float labelOffsetY = -42f - (index * 14f);
            _nameLabel.Position = new Vector2(-24, labelOffsetY);
            _nameLabel.CustomMinimumSize = new Vector2(48, 0);
            _nameLabel.ZIndex = 10;
            AddChild(_nameLabel);

            // Run after simulation tick (default priority 0)
            ProcessPriority = 50;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;
            ref var p = ref _state.Players[_playerIndex];

            // Victory animation — winner bounces with golden glow
            if (_state.Phase == MatchPhase.Ended && !p.IsDead && _state.WinnerIndex == _playerIndex)
            {
                if (!_victoryAnimActive)
                {
                    _victoryAnimActive = true;
                    _victoryAnimTimer = 0f;
                    _aimLine.Visible = false;
                    if (_nameLabel != null) _nameLabel.Visible = false;
                }

                _victoryAnimTimer += (float)delta;
                GlobalPosition = p.Position.ToGodot();
                float bounce = Mathf.Sin(_victoryAnimTimer * 6f) * 0.15f;
                Scale = Vector2.One * (1f + bounce);
                _body.Modulate = new Color(1f, 1f, 1f).Lerp(
                    new Color(0.83f, 0.69f, 0.21f), // #D4AF37 golden glow
                    (Mathf.Sin(_victoryAnimTimer * 4f) + 1f) * 0.3f);
                Visible = true;
                return;
            }

            // Death animation: 1s fade+shrink to zero
            if (p.IsDead)
            {
                if (!_deathAnimStarted)
                {
                    _deathAnimStarted = true;
                    _deathTimer = 1f;
                    _aimLine.Visible = false;
                    if (_nameLabel != null) _nameLabel.Visible = false;
                }

                _deathTimer -= (float)delta;
                GlobalPosition = p.Position.ToGodot();

                if (_deathTimer > 0f)
                {
                    float t = _deathTimer;
                    Scale = Vector2.One * t;
                    var c = _body.Modulate;
                    c.A = t;
                    _body.Modulate = c;
                    Visible = true;
                }
                else
                {
                    Visible = false;
                }
                return;
            }

            Visible = true;
            Scale = Vector2.One;

            // Position (Y-flipped from sim to Godot)
            GlobalPosition = p.Position.ToGodot();

            // Face direction: FacingDirection is 1 (right) or -1 (left)
            _body.FlipH = p.FacingDirection < 0;

            // Sprite animation state machine: Aim > Jump > Walk > Idle
            if (_hasRealSprites)
            {
                if (p.IsCharging && _aimTex != null)
                {
                    _body.Texture = _aimTex;
                }
                else if (!p.IsGrounded && _jumpTex != null)
                {
                    _body.Texture = _jumpTex;
                }
                else if (Mathf.Abs(p.Velocity.x) > 0.5f && _walkTex != null && _walkTex.Length > 0)
                {
                    _walkTimer += (float)delta;
                    if (_walkTimer >= 1f / WalkFPS)
                    {
                        _walkTimer -= 1f / WalkFPS;
                        _walkFrame = (_walkFrame + 1) % _walkTex.Length;
                    }
                    _body.Texture = _walkTex[_walkFrame];
                }
                else
                {
                    _body.Texture = _idleTex;
                }
            }

            // Aim line direction
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
            _aimLine.Visible = !p.IsAI;

            // Damage flash: red tint alternating 20 Hz for FlashDuration
            if (p.Health < _lastHealth)
            {
                _flashTimer = FlashDuration;
            }
            _lastHealth = p.Health;

            Color tint = _baseColor;
            if (_flashTimer > 0f)
            {
                _flashTimer -= (float)delta;
                bool flashOn = (int)(_flashTimer * 20f) % 2 == 0;
                tint = flashOn ? new Color(1f, 0.2f, 0.2f) : Colors.White;
            }

            // Freeze effect overrides normal tint
            if (p.FreezeTimer > 0f)
            {
                tint = new Color(0.5f, 0.8f, 1f);
            }

            // Decoy invisibility — faint ghost outline
            if (p.IsInvisible)
            {
                tint.A = 0.15f;
            }

            _body.Modulate = tint;
        }
    }
}
