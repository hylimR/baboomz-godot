using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>Renders oil barrels as brown/orange squares. Syncs with GameState.Barrels each frame.</summary>
    public partial class BarrelRenderer : Node2D
    {
        private GameState _state;
        private readonly Dictionary<int, Sprite2D> _barrelSprites = new();

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            var alive = new HashSet<int>();
            for (int i = 0; i < _state.Barrels.Count; i++)
            {
                var barrel = _state.Barrels[i];
                if (!barrel.Active) continue;

                alive.Add(i);
                if (!_barrelSprites.ContainsKey(i))
                {
                    var sprite = new Sprite2D();
                    sprite.Texture = ProceduralSprites.CreateColorRect(20, 24, new Color(0.55f, 0.35f, 0.1f));
                    sprite.ZIndex = 3;
                    AddChild(sprite);
                    _barrelSprites[i] = sprite;
                }
                _barrelSprites[i].GlobalPosition = barrel.Position.ToGodot();
            }

            // Remove destroyed barrels
            var toRemove = new List<int>();
            foreach (var kvp in _barrelSprites)
                if (!alive.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            foreach (int id in toRemove)
            {
                _barrelSprites[id].QueueFree();
                _barrelSprites.Remove(id);
            }
        }
    }
}
