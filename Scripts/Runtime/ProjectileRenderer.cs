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

        // Trail
        private Line2D _trail;
        private const int MaxTrailPoints = 15;

        public void Init(int id, GameState state)
        {
            _projId = id;
            _state = state;

            // Projectile sprite (small dark circle)
            _sprite = new Sprite2D();
            _sprite.Texture = ProceduralSprites.CreateCircle(12, new Color(0.2f, 0.2f, 0.2f));
            _sprite.ZIndex = 10;
            AddChild(_sprite);

            // Trail line
            _trail = new Line2D();
            _trail.Width = 3f;
            _trail.DefaultColor = new Color(1f, 0.9f, 0.5f, 0.5f);
            _trail.ZIndex = 9;
            AddChild(_trail);

            ProcessPriority = 55; // After simulation, before camera
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
                    while (_trail.PointCount > MaxTrailPoints)
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
