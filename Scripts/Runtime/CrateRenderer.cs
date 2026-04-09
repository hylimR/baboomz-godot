using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>Renders weapon crates with type-based colors. Syncs with GameState.Crates each frame.</summary>
    public partial class CrateRenderer : Node2D
    {
        private GameState _state;
        private readonly Dictionary<int, Sprite2D> _crateSprites = new();

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            var alive = new HashSet<int>();
            for (int i = 0; i < _state.Crates.Count; i++)
            {
                var crate = _state.Crates[i];
                if (!crate.Active) continue;

                alive.Add(i);
                if (!_crateSprites.ContainsKey(i))
                {
                    var sprite = new Sprite2D();
                    sprite.Texture = ProceduralSprites.CreateColorRect(18, 18, CrateColor(crate.Type));
                    sprite.ZIndex = 4;
                    AddChild(sprite);
                    _crateSprites[i] = sprite;
                }
                _crateSprites[i].GlobalPosition = crate.Position.ToGodot();
            }

            // Remove collected crates
            var toRemove = new List<int>();
            foreach (var kvp in _crateSprites)
                if (!alive.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            foreach (int id in toRemove)
            {
                _crateSprites[id].QueueFree();
                _crateSprites.Remove(id);
            }
        }

        private static Color CrateColor(CrateType type)
        {
            return type switch
            {
                CrateType.Health => new Color(0.2f, 0.8f, 0.2f),       // green
                CrateType.Energy => new Color(0.2f, 0.5f, 0.9f),       // blue
                CrateType.AmmoRefill => new Color(0.9f, 0.8f, 0.1f),   // yellow
                CrateType.DoubleDamage => new Color(0.9f, 0.15f, 0.15f), // red
                _ => new Color(0.7f, 0.7f, 0.7f),                       // gray fallback
            };
        }
    }
}
