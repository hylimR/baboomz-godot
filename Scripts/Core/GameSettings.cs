using Godot;

namespace Baboomz
{
    /// <summary>
    /// Persistent game settings using Godot's ConfigFile.
    /// Replaces Unity's PlayerPrefs.
    /// </summary>
    public class GameSettings
    {
        private const string SettingsPath = "user://settings.cfg";
        private readonly ConfigFile _config = new();

        public float MasterVolume { get; set; } = 1.0f;
        public float SfxVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public bool Fullscreen { get; set; } = false;

        public void Load()
        {
            if (_config.Load(SettingsPath) != Error.Ok) return;
            MasterVolume = (float)_config.GetValue("audio", "master", 1.0f);
            SfxVolume = (float)_config.GetValue("audio", "sfx", 1.0f);
            MusicVolume = (float)_config.GetValue("audio", "music", 0.8f);
            Fullscreen = (bool)_config.GetValue("display", "fullscreen", false);
        }

        public void Save()
        {
            _config.SetValue("audio", "master", MasterVolume);
            _config.SetValue("audio", "sfx", SfxVolume);
            _config.SetValue("audio", "music", MusicVolume);
            _config.SetValue("display", "fullscreen", Fullscreen);
            _config.Save(SettingsPath);
        }
    }
}
