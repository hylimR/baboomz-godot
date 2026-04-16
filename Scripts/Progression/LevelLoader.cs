using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Godot-side bridge that reads a level JSON from <c>res://Resources/Levels/{id}.json</c>
    /// and applies it to a <see cref="GameConfig"/> via the pure-C# <see cref="LevelDataApplier"/>.
    ///
    /// Keeps the JSON I/O in the Godot layer (FileAccess) while leaving the
    /// actual field mapping pure and unit-testable. Called from GameRunner
    /// when <c>GameModeContext.SelectedLevelId</c> is non-empty.
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>
        /// Returns <c>(loaded, seed)</c>. If the level file is missing or
        /// invalid, <paramref name="cfg"/> is unchanged and loaded = false.
        /// </summary>
        public static (bool loaded, int? seed) TryApply(GameConfig cfg, string levelId)
        {
            if (cfg == null || string.IsNullOrEmpty(levelId))
                return (false, null);

            string path = $"res://Resources/Levels/{levelId}.json";
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushWarning($"LevelLoader: could not open {path}");
                return (false, null);
            }

            string json = file.GetAsText();
            var result = LevelDataApplier.Apply(json, cfg);
            if (!result.Applied)
            {
                GD.PushWarning($"LevelLoader: invalid JSON in {path}");
                return (false, null);
            }

            GD.Print($"LevelLoader: applied '{result.LevelId}' (seed={result.TerrainSeed})");
            return (true, result.TerrainSeed);
        }
    }
}
