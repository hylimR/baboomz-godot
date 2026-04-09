using System;
using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders a trajectory arc preview with crosshair at the projected impact point.
    /// Handles bouncing projectile preview for grenades.
    /// </summary>
    public partial class TrajectoryPreview : Node2D
    {
        private GameState _state;
        private Line2D _line;

        private const int Points = 50;
        private const float TimeStep = 0.05f;

        public void Init(GameState state)
        {
            _state = state;

            _line = new Line2D();
            _line.Width = 2f;
            _line.DefaultColor = new Color(1f, 1f, 1f, 0.4f);
            _line.ZIndex = 15;
            AddChild(_line);

            ProcessPriority = 45;
        }

        public void Hide()
        {
            if (_line != null) _line.ClearPoints();
        }

        public override void _Process(double delta)
        {
            if (_state == null || _line == null || _state.Players.Length == 0)
                return;

            _line.ClearPoints();

            ref PlayerState p = ref _state.Players[0];

            if (p.IsDead || p.IsAI)
                return;

            var weapon = p.WeaponSlots[p.ActiveWeaponSlot];
            if (weapon.WeaponId == null)
                return;

            float power = p.IsCharging
                ? Math.Max(p.AimPower, weapon.MinPower)
                : weapon.MinPower;

            // AimAngle is in degrees in the simulation
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 velocity = new Vec2(
                MathF.Cos(rad) * p.FacingDirection,
                MathF.Sin(rad)
            ) * power;
            Vec2 pos = p.Position + new Vec2(0f, 0.5f);

            int bouncesLeft = weapon.Bounces;

            // Weapon-specific line color
            Color c = weapon.WeaponId switch
            {
                "shotgun" => new Color(1f, 0.8f, 0.3f, 0.4f),
                "rocket" => new Color(1f, 0.3f, 0.2f, 0.4f),
                "cluster" => new Color(0.9f, 0.5f, 0.9f, 0.4f),
                _ => new Color(1f, 1f, 1f, 0.4f)
            };
            _line.DefaultColor = c;

            for (int i = 0; i < Points; i++)
            {
                _line.AddPoint(pos.ToGodot());

                // Drill is wind/gravity-immune (matches ProjectileSimulation)
                if (!weapon.IsDrill)
                {
                    GamePhysics.ApplyGravity(ref velocity, TimeStep, _state.Config.Gravity);
                    GamePhysics.ApplyWind(ref velocity, _state.WindForce, TimeStep);
                }
                pos = pos + velocity * TimeStep;

                // Check terrain collision
                int px = _state.Terrain.WorldToPixelX(pos.x);
                int py = _state.Terrain.WorldToPixelY(pos.y);
                if (_state.Terrain.IsSolid(px, py))
                {
                    if (bouncesLeft > 0)
                    {
                        bouncesLeft--;
                        pos.y = _state.Terrain.PixelToWorldY(py + 1) + 0.1f;
                        velocity = new Vec2(velocity.x * 0.8f, -velocity.y * 0.5f);
                    }
                    else
                    {
                        // Fill remaining points at impact position
                        for (int j = i + 1; j < Points; j++)
                            _line.AddPoint(pos.ToGodot());
                        break;
                    }
                }
            }
        }
    }
}
