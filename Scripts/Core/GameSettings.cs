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
        public int SkillSlot0 { get; set; } = 0; // default: teleport
        public int SkillSlot1 { get; set; } = 3; // default: dash

        // Combat feedback toggles (#127) — allow low-end devices to disable
        // the per-frame Draw / shader work while keeping gameplay intact.
        public bool HitMarkersEnabled { get; set; } = true;
        public bool LowHealthOverlayEnabled { get; set; } = true;
        public bool ComboEffectsEnabled { get; set; } = true;

        public void Load()
        {
            if (_config.Load(SettingsPath) != Error.Ok) return;
            MasterVolume = (float)_config.GetValue("audio", "master", 1.0f);
            SfxVolume = (float)_config.GetValue("audio", "sfx", 1.0f);
            MusicVolume = (float)_config.GetValue("audio", "music", 0.8f);
            Fullscreen = (bool)_config.GetValue("display", "fullscreen", false);
            SkillSlot0 = (int)_config.GetValue("loadout", "skill0", 0);
            SkillSlot1 = (int)_config.GetValue("loadout", "skill1", 3);
            HitMarkersEnabled = (bool)_config.GetValue("feedback", "hit_markers", true);
            LowHealthOverlayEnabled = (bool)_config.GetValue("feedback", "low_health_overlay", true);
            ComboEffectsEnabled = (bool)_config.GetValue("feedback", "combo_effects", true);
        }

        public void Save()
        {
            _config.SetValue("audio", "master", MasterVolume);
            _config.SetValue("audio", "sfx", SfxVolume);
            _config.SetValue("audio", "music", MusicVolume);
            _config.SetValue("display", "fullscreen", Fullscreen);
            _config.SetValue("loadout", "skill0", SkillSlot0);
            _config.SetValue("loadout", "skill1", SkillSlot1);
            _config.SetValue("feedback", "hit_markers", HitMarkersEnabled);
            _config.SetValue("feedback", "low_health_overlay", LowHealthOverlayEnabled);
            _config.SetValue("feedback", "combo_effects", ComboEffectsEnabled);
            _config.Save(SettingsPath);
        }
    }
}
