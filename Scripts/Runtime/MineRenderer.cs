using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>Renders land mines as red circles. Syncs with GameState.Mines each frame.</summary>
    public partial class MineRenderer : Node2D
    {
        private GameState _state;
        private readonly Dictionary<int, Sprite2D> _mineSprites = new();

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            var alive = new HashSet<int>();
            for (int i = 0; i < _state.Mines.Count; i++)
            {
                var mine = _state.Mines[i];
                if (!mine.Active) continue;

                alive.Add(i);
                if (!_mineSprites.ContainsKey(i))
                {
                    var color = mine.IsHoming
                        ? new Color(0.7f, 0.1f, 0.8f) // purple for magnetic mines
                        : new Color(0.8f, 0.1f, 0.1f); // red for regular mines
                    var sprite = new Sprite2D();
                    sprite.Texture = ProceduralSprites.CreateCircle(16, color);
                    sprite.ZIndex = 3;
                    AddChild(sprite);
                    _mineSprites[i] = sprite;
                }
                _mineSprites[i].GlobalPosition = mine.Position.ToGodot();
            }

            // Remove dead mines
            var toRemove = new List<int>();
            foreach (var kvp in _mineSprites)
                if (!alive.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            foreach (int id in toRemove)
            {
                _mineSprites[id].QueueFree();
                _mineSprites.Remove(id);
            }
        }
    }
}
