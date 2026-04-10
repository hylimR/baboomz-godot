using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Renders mob-type players. Mobs are identified by PlayerState.IsMob == true
    /// in the Players array. Tries to load a real sprite from Art/Characters/...
    /// and falls back to a procedural colored rectangle if the texture is missing.
    /// Only creates visuals for mobs; regular players are handled by PlayerRenderer.
    /// </summary>
    public partial class MobRenderer : Node2D
    {
        // Target on-screen height in pixels. Boss sprites scale proportionally larger.
        private const float MobTargetHeight = 48f;
        private const float BossTargetHeight = 96f;

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
                    _mobSprites[i] = CreateMobSprite(p.MobType, p.BossType);
                    AddChild(_mobSprites[i]);
                }

                var sprite = _mobSprites[i];
                sprite.GlobalPosition = p.Position.ToGodot();
                // Mirror sprite to match facing direction for non-turret, non-boss mobs.
                sprite.FlipH = p.FacingDirection < 0;
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

        private static Sprite2D CreateMobSprite(string mobType, string bossType)
        {
            var sprite = new Sprite2D();
            sprite.ZIndex = 5;

            // Try to load a real sprite first (issue #35).
            Texture2D tex = LoadMobTexture(mobType, bossType);
            if (tex != null)
            {
                sprite.Texture = tex;
                float target = mobType == "boss" ? BossTargetHeight : MobTargetHeight;
                float srcHeight = tex.GetHeight();
                if (srcHeight > 0f)
                {
                    float s = target / srcHeight;
                    sprite.Scale = new Vector2(s, s);
                }
                return sprite;
            }

            // Fallback: procedural colored rectangle (original behavior).
            int size = MobSize(mobType);
            sprite.Texture = ProceduralSprites.CreateColorRect(size, size, MobColor(mobType));
            return sprite;
        }

        /// <summary>
        /// Resolves a mob type (and optional boss type) to a texture loaded from
        /// Art/Characters/.../&lt;file&gt;.png. Returns null if no matching asset.
        /// </summary>
        private static Texture2D LoadMobTexture(string mobType, string bossType)
        {
            // Non-boss mob types map to Art/Characters/<Capitalized>/<mobtype>_idle.png
            // (e.g. "bomber" -> Characters/Bomber/bomber_idle)
            switch (mobType)
            {
                case "bomber":
                    return SpriteLoader.Load("Characters/Bomber/bomber_idle");
                case "shielder":
                    return SpriteLoader.Load("Characters/Shielder/shielder_idle");
                case "flyer":
                    return SpriteLoader.Load("Characters/Flyer/flyer_idle");
                case "healer":
                    return SpriteLoader.Load("Characters/Healer/healer_idle");
                case "boss":
                    // Bosses live in Art/Characters/Bosses/boss_<type>.png
                    if (string.IsNullOrEmpty(bossType)) return null;
                    return SpriteLoader.Load($"Characters/Bosses/boss_{bossType}");
                default:
                    // walker / turret / unknown — no dedicated art yet
                    return null;
            }
        }

        private static Color MobColor(string mobType)
        {
            return mobType switch
            {
                "bomber" => new Color(0.9f, 0.4f, 0.1f),
                "shielder" => new Color(0.3f, 0.5f, 0.9f),
                "flyer" => new Color(0.7f, 0.2f, 0.8f),
                "healer" => new Color(0.2f, 0.85f, 0.3f),
                "boss" => new Color(0.85f, 0.1f, 0.1f),
                _ => new Color(0.6f, 0.6f, 0.6f),
            };
        }

        private static int MobSize(string mobType)
        {
            return mobType switch
            {
                "bomber" => 20,
                "shielder" => 26,
                "flyer" => 18,
                "healer" => 20,
                "boss" => 48,
                _ => 22,
            };
        }
    }
}
