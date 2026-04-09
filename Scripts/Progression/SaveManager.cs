using Godot;
using System.Text.Json;

namespace Baboomz
{
    /// <summary>
    /// Handles JSON serialization of PlayerSaveData to user:// path.
    /// Uses Godot.FileAccess for user:// path resolution and System.Text.Json for serialization.
    /// Static Serialize/Deserialize methods enable unit testing without file I/O.
    /// Replaces Unity's JsonUtility + Application.persistentDataPath approach.
    /// </summary>
    public static class SaveManager
    {
        private const string SaveFilePath = "user://baboomz_save.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes save data to a JSON string.
        /// </summary>
        public static string Serialize(PlayerSaveData data)
        {
            return JsonSerializer.Serialize(data, JsonOptions);
        }

        /// <summary>
        /// Deserializes a JSON string into PlayerSaveData.
        /// Returns a new default instance if json is null or empty.
        /// </summary>
        public static PlayerSaveData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new PlayerSaveData();
            return JsonSerializer.Deserialize<PlayerSaveData>(json, JsonOptions)
                   ?? new PlayerSaveData();
        }

        /// <summary>
        /// Saves player data to disk at user://baboomz_save.json.
        /// </summary>
        public static void Save(PlayerSaveData data)
        {
            string json = Serialize(data);
            using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"SaveManager: Failed to open {SaveFilePath} for writing — {FileAccess.GetOpenError()}");
                return;
            }
            file.StoreString(json);
            GD.Print($"SaveManager: Saved to {SaveFilePath}");
        }

        /// <summary>
        /// Loads player data from disk. Returns a new default instance if no save exists.
        /// </summary>
        public static PlayerSaveData Load()
        {
            if (!FileAccess.FileExists(SaveFilePath))
            {
                GD.Print("SaveManager: No save file found, returning defaults");
                return new PlayerSaveData();
            }

            using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"SaveManager: Failed to open {SaveFilePath} for reading — {FileAccess.GetOpenError()}");
                return new PlayerSaveData();
            }

            string json = file.GetAsText();
            GD.Print($"SaveManager: Loaded from {SaveFilePath}");
            return Deserialize(json);
        }

        /// <summary>
        /// Deletes the save file if it exists.
        /// </summary>
        public static void Delete()
        {
            if (FileAccess.FileExists(SaveFilePath))
            {
                DirAccess.RemoveAbsolute(SaveFilePath);
                GD.Print($"SaveManager: Deleted {SaveFilePath}");
            }
        }

        /// <summary>
        /// Returns true if a save file exists on disk.
        /// </summary>
        public static bool SaveExists()
        {
            return FileAccess.FileExists(SaveFilePath);
        }
    }
}
