using System.Collections.Generic;
using Godot;

namespace Baboomz
{
    /// <summary>
    /// Loads Texture2D assets from res://Art/ at runtime.
    /// Caches loaded textures to avoid redundant disk reads.
    /// Returns null for missing files so callers can fall back to ProceduralSprites.
    /// </summary>
    public static class SpriteLoader
    {
        private const string ArtRoot = "res://Art/";
        private static readonly Dictionary<string, Texture2D> cache = new();

        /// <summary>
        /// Loads Art/&lt;path&gt;.png as a Texture2D. Returns null if missing.
        /// </summary>
        public static Texture2D Load(string path)
        {
            if (cache.TryGetValue(path, out var cached))
                return cached;

            string fullPath = $"{ArtRoot}{path}.png";
            if (!ResourceLoader.Exists(fullPath))
            {
                cache[path] = null;
                return null;
            }

            var tex = GD.Load<Texture2D>(fullPath);
            cache[path] = tex;
            return tex;
        }

        /// <summary>
        /// Loads character poses for a group (e.g. "Player", "Opponent", "Swordman").
        /// Tries walk_01..walk_08 individually; falls back to walk.png if no numbered frames.
        /// Any missing pose returns null — callers must handle fallback.
        /// </summary>
        public static (Texture2D idle, Texture2D[] walk, Texture2D jump, Texture2D aim, Texture2D portrait)
            LoadCharacter(string group)
        {
            var idle = Load($"Characters/{group}/idle");
            var jump = Load($"Characters/{group}/jump");
            var aim = Load($"Characters/{group}/aim");
            var portrait = Load($"Characters/{group}/portrait");

            var walkFrames = new List<Texture2D>();
            for (int i = 1; i <= 8; i++)
            {
                var frame = Load($"Characters/{group}/walk_0{i}");
                if (frame != null) walkFrames.Add(frame);
                else break;
            }

            if (walkFrames.Count == 0)
            {
                var single = Load($"Characters/{group}/walk");
                if (single != null) walkFrames.Add(single);
            }

            var walk = walkFrames.Count > 0 ? walkFrames.ToArray() : null;
            return (idle, walk, jump, aim, portrait);
        }

        public static void ClearCache() => cache.Clear();
    }
}
