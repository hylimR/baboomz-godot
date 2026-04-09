using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Renders mob-type players as colored squares. Mobs are identified by
    /// PlayerState.IsMob == true in the Players array.
    /// Only creates visuals for mobs; regular players are handled by PlayerRenderer.
    /// </summary>
    public partial class MobRenderer : Node2D
    {
        private GameState _state;
        private readonly Dictionary<int, Sprite2D> _mobSprites = new();

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            var aliveMobs = new HashSet<int>();
            for (int i = 0; i < _state.Players.Length; i++)
            {
                ref PlayerState p = ref _state.Players[i];
                if (!p.IsMob || p.IsDead) continue;

                aliveMobs.Add(i);
                if (!_mobSprites.ContainsKey(i))
                {
                    var sprite = new Sprite2D();
                    int size = MobSize(p.MobType);
                    sprite.Texture = ProceduralSprites.CreateColorRect(size, size, MobColor(p.MobType));
                    sprite.ZIndex = 5;
                    AddChild(sprite);
                    _mobSprites[i] = sprite;
                }
                _mobSprites[i].GlobalPosition = p.Position.ToGodot();
            }

            // Remove dead mob sprites
            var toRemove = new List<int>();
            foreach (var kvp in _mobSprites)
                if (!aliveMobs.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            foreach (int id in toRemove)
            {
                _mobSprites[id].QueueFree();
                _mobSprites.Remove(id);
            }
        }

        private static Color MobColor(string mobType)
        {
            return mobType switch
            {
                "Bomber" => new Color(0.9f, 0.4f, 0.1f),
                "Shielder" => new Color(0.3f, 0.5f, 0.9f),
                "Flyer" => new Color(0.7f, 0.2f, 0.8f),
                "Healer" => new Color(0.2f, 0.85f, 0.3f),
                _ => new Color(0.6f, 0.6f, 0.6f),
            };
        }

        private static int MobSize(string mobType)
        {
            return mobType switch
            {
                "Bomber" => 20,
                "Shielder" => 26,
                "Flyer" => 18,
                "Healer" => 20,
                _ => 22,
            };
        }
    }
}
