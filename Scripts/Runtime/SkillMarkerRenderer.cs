using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Renders active skill markers: shield bubbles, smoke clouds, grapple lines,
    /// shadow step markers, dash trails. Reads SkillSlotState from each player each frame.
    /// </summary>
    public partial class SkillMarkerRenderer : Node2D
    {
        private GameState _state;
        private readonly Dictionary<int, Sprite2D> _shieldSprites = new();
        private readonly Dictionary<int, Sprite2D> _shadowStepSprites = new();
        private readonly Dictionary<int, Line2D> _grappleLines = new();
        private readonly Dictionary<int, Sprite2D> _smokeSprites = new();
        private readonly Dictionary<int, Line2D> _dashTrails = new();
        private readonly Dictionary<int, Line2D> _sprintTrails = new();

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 55;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            var activeShields = new HashSet<int>();
            var activeGrapples = new HashSet<int>();
            var activeShadowSteps = new HashSet<int>();
            var activeDashes = new HashSet<int>();
            var activeSprints = new HashSet<int>();

            for (int i = 0; i < _state.Players.Length; i++)
            {
                ref PlayerState p = ref _state.Players[i];
                if (p.IsDead || p.SkillSlots == null) continue;

                for (int s = 0; s < p.SkillSlots.Length; s++)
                {
                    ref SkillSlotState skill = ref p.SkillSlots[s];
                    if (!skill.IsActive) continue;

                    switch (skill.Type)
                    {
                        case SkillType.Shield:
                            activeShields.Add(i);
                            UpdateShield(i, p.Position);
                            break;

                        case SkillType.GrapplingHook:
                            activeGrapples.Add(i);
                            UpdateGrappleLine(i, p.Position, p.SkillTargetPosition);
                            break;

                        case SkillType.ShadowStep:
                            activeShadowSteps.Add(i);
                            UpdateShadowStep(i, p.SkillTargetPosition, skill.DurationRemaining);
                            break;

                        case SkillType.Dash:
                            activeDashes.Add(i);
                            UpdateDashTrail(i, p.Position, skill.DurationRemaining, skill.Duration);
                            break;

                        case SkillType.Sprint:
                            activeSprints.Add(i);
                            UpdateSprintTrail(i, p.Position, skill.DurationRemaining, skill.Duration);
                            break;
                    }
                }
            }

            // Sync smoke zones
            SyncSmokeZones();

            // Cleanup expired visuals
            CleanupMap(_shieldSprites, activeShields);
            CleanupMap(_grappleLines, activeGrapples);
            CleanupMap(_shadowStepSprites, activeShadowSteps);
            CleanupMap(_dashTrails, activeDashes);
            CleanupMap(_sprintTrails, activeSprints);
        }

        private void UpdateShield(int playerIndex, Vec2 pos)
        {
            if (!_shieldSprites.TryGetValue(playerIndex, out var sprite))
            {
                sprite = new Sprite2D();
                sprite.Texture = ProceduralSprites.CreateCircle(48, new Color(0.3f, 0.6f, 1f, 0.3f));
                sprite.ZIndex = 15;
                AddChild(sprite);
                _shieldSprites[playerIndex] = sprite;
            }
            sprite.GlobalPosition = pos.ToGodot() + new Vector2(0, -8);
        }

        private void UpdateGrappleLine(int playerIndex, Vec2 from, Vec2 anchor)
        {
            if (!_grappleLines.TryGetValue(playerIndex, out var line))
            {
                line = new Line2D();
                line.Width = 2f;
                line.DefaultColor = new Color(0.6f, 0.4f, 0.1f, 0.9f);
                line.ZIndex = 12;
                AddChild(line);
                _grappleLines[playerIndex] = line;
            }
            line.ClearPoints();
            line.AddPoint(from.ToGodot());
            line.AddPoint(anchor.ToGodot());
        }

        private void UpdateShadowStep(int playerIndex, Vec2 markPos, float remaining)
        {
            if (!_shadowStepSprites.TryGetValue(playerIndex, out var sprite))
            {
                sprite = new Sprite2D();
                sprite.Texture = ProceduralSprites.CreateCircle(32, new Color(0.6f, 0.2f, 1f, 0.5f));
                sprite.ZIndex = 14;
                AddChild(sprite);
                _shadowStepSprites[playerIndex] = sprite;
            }
            sprite.GlobalPosition = markPos.ToGodot();
            float pulse = 1f + 0.2f * Mathf.Sin((float)Time.GetTicksMsec() / 1000f * 6f);
            sprite.Scale = Vector2.One * 1.5f * pulse;
            float alpha = 0.3f + 0.3f * Mathf.Clamp(remaining / 3f, 0f, 1f);
            sprite.Modulate = new Color(1f, 1f, 1f, alpha);
        }

        private void UpdateDashTrail(int playerIndex, Vec2 pos, float remaining, float total)
        {
            if (!_dashTrails.TryGetValue(playerIndex, out var trail))
            {
                trail = new Line2D();
                trail.Width = 4f;
                trail.DefaultColor = new Color(0.8f, 0.9f, 1f, 0.7f);
                trail.ZIndex = 13;
                trail.BeginCapMode = Line2D.LineCapMode.Round;
                trail.EndCapMode = Line2D.LineCapMode.Round;
                AddChild(trail);
                _dashTrails[playerIndex] = trail;
            }

            // Append current position as a trail point
            var godotPos = pos.ToGodot();
            trail.AddPoint(godotPos);

            // Cap trail length to prevent unbounded growth
            while (trail.GetPointCount() > 12)
                trail.RemovePoint(0);

            // Fade alpha based on remaining duration
            float t = total > 0f ? remaining / total : 0f;
            trail.DefaultColor = new Color(0.8f, 0.9f, 1f, 0.7f * t);
        }

        private void UpdateSprintTrail(int playerIndex, Vec2 pos, float remaining, float total)
        {
            if (!_sprintTrails.TryGetValue(playerIndex, out var trail))
            {
                trail = new Line2D();
                trail.Width = 3f;
                trail.DefaultColor = new Color(1f, 0.85f, 0.3f, 0.6f);
                trail.ZIndex = 13;
                trail.BeginCapMode = Line2D.LineCapMode.Round;
                trail.EndCapMode = Line2D.LineCapMode.Round;
                AddChild(trail);
                _sprintTrails[playerIndex] = trail;
            }

            var godotPos = pos.ToGodot();
            trail.AddPoint(godotPos);

            // Longer trail than dash (sprint is sustained)
            while (trail.GetPointCount() > 20)
                trail.RemovePoint(0);

            float t = total > 0f ? remaining / total : 0f;
            trail.DefaultColor = new Color(1f, 0.85f, 0.3f, 0.6f * t);
        }

        private void SyncSmokeZones()
        {
            // Simple approach: recreate smoke sprites each frame for active zones
            var activeSmokeIds = new HashSet<int>();
            for (int i = 0; i < _state.SmokeZones.Count; i++)
            {
                var zone = _state.SmokeZones[i];
                if (zone.RemainingTime <= 0f) continue;

                activeSmokeIds.Add(i);
                if (!_smokeSprites.TryGetValue(i, out var sprite))
                {
                    int diameter = Mathf.Max(16, (int)(zone.Radius * 6));
                    sprite = new Sprite2D();
                    sprite.Texture = ProceduralSprites.CreateCircle(diameter, new Color(0.3f, 0.3f, 0.3f, 0.35f));
                    sprite.ZIndex = 10;
                    AddChild(sprite);
                    _smokeSprites[i] = sprite;
                }
                sprite.GlobalPosition = zone.Position.ToGodot();
            }
            CleanupMap(_smokeSprites, activeSmokeIds);
        }

        private static void CleanupMap<T>(Dictionary<int, T> map, HashSet<int> alive) where T : Node
        {
            var toRemove = new List<int>();
            foreach (var kvp in map)
                if (!alive.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            foreach (int id in toRemove)
            {
                map[id].QueueFree();
                map.Remove(id);
            }
        }
    }
}
