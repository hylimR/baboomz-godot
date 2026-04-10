using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders a single in-flight projectile with a trailing line.
    /// Created when a projectile spawns, freed when it dies.
    /// </summary>
    public partial class ProjectileRenderer : Node2D
    {
        private int _projId;
        private GameState _state;
        private Sprite2D _sprite;
        private static Texture2D _sharedProjectileTex;

        // Trail
        private Line2D _trail;
        private const int MaxTrailPoints = 15;

        public void Init(int id, GameState state)
        {
            _projId = id;
            _state = state;

            // Look up the projectile once to pick visuals by weapon type.
            float explosionRadius = 2.5f;
            foreach (var proj in state.Projectiles)
            {
                if (proj.Id == id)
                {
                    explosionRadius = proj.ExplosionRadius;
                    break;
                }
            }

            var (sprColor, trailColor, scaleMul) = GetWeaponVisuals(explosionRadius);

            _sprite = new Sprite2D();
            if (_sharedProjectileTex == null)
                _sharedProjectileTex = SpriteLoader.Load("VFX/Default/projectile");

            if (_sharedProjectileTex != null)
            {
                _sprite.Texture = _sharedProjectileTex;
                _sprite.Modulate = sprColor;
                // Real sprite is large; scale to produce ~12-48 px diameter matching weapon scale.
                float srcW = _sharedProjectileTex.GetWidth();
                float baseScale = srcW > 0f ? 48f / srcW : 1f;
                _sprite.Scale = new Vector2(baseScale * scaleMul, baseScale * scaleMul);
            }
            else
            {
                // Procedural fallback
                _sprite.Texture = ProceduralSprites.CreateCircle(12, sprColor);
            }
            _sprite.ZIndex = 10;
            AddChild(_sprite);

            // Trail line
            _trail = new Line2D();
            _trail.Width = 3f;
            _trail.DefaultColor = new Color(trailColor.R, trailColor.G, trailColor.B, 0.5f);
            _trail.ZIndex = 9;
            AddChild(_trail);

            ProcessPriority = 55; // After simulation, before camera
        }

        /// <summary>
        /// Weapon-type visuals keyed off explosion radius.
        /// Rockets (radius ≥ 3.5): red/orange. Shotgun pellets (≤ 1.3): gold, tiny.
        /// Everything else (cannon/drill): gray with yellow trail.
        /// Scale multiplier: Clamp(radius * 0.15, 0.2, 0.6); shotgun forced to 0.15.
        /// </summary>
        private static (Color sprite, Color trail, float scale) GetWeaponVisuals(float radius)
        {
            if (radius >= 3.5f)
            {
                // Rocket: red body, orange trail
                return (new Color(0.90f, 0.30f, 0.20f), new Color(1.0f, 0.40f, 0.20f),
                    Mathf.Clamp(radius * 0.15f, 0.2f, 0.6f));
            }
            if (radius <= 1.3f)
            {
                // Shotgun pellet: gold, small
                return (new Color(1.0f, 0.80f, 0.40f), new Color(1.0f, 1.0f, 0.50f), 0.15f);
            }
            // Cannon / drill: gray body, yellow trail
            return (new Color(0.30f, 0.30f, 0.30f), new Color(1.0f, 1.0f, 0.50f),
                Mathf.Clamp(radius * 0.15f, 0.2f, 0.6f));
        }

        public override void _Process(double delta)
        {
            if (_state == null) { QueueFree(); return; }

            bool found = false;
            foreach (var proj in _state.Projectiles)
            {
                if (proj.Id == _projId)
                {
                    if (!proj.Alive)
                    {
                        QueueFree();
                        return;
                    }

                    GlobalPosition = proj.Position.ToGodot();

                    // Update trail (points in local space relative to parent)
                    _trail.AddPoint(GlobalPosition);
                    while (_trail.GetPointCount() > MaxTrailPoints)
                        _trail.RemovePoint(0);

                    // Rotate sprite to face velocity direction
                    if (proj.Velocity.SqrMagnitude > 0.01f)
                    {
                        var vel = proj.Velocity.ToGodot();
                        Rotation = vel.Angle();
                    }

                    found = true;
                    break;
                }
            }

            if (!found) QueueFree();
        }
    }
}
