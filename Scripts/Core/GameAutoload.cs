using Godot;

namespace Baboomz
{
    /// <summary>
    /// Autoload singleton — global state that persists across scenes.
    /// Replaces Unity's VContainer GameLifetimeScope + DontDestroyOnLoad.
    /// </summary>
    public partial class GameAutoload : Node
    {
        public static GameAutoload Instance { get; private set; }

        public GameSettings Settings { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            Settings = new GameSettings();
            Settings.Load();

            SetupAudioBuses();
            ApplyAudioSettings();
            ApplyFullscreen();

            GD.Print("GameAutoload ready");
        }

        /// <summary>
        /// Creates SFX and Music buses as children of Master (if not already present).
        /// </summary>
        private void SetupAudioBuses()
        {
            if (AudioServer.GetBusIndex("SFX") == -1)
            {
                AudioServer.AddBus();
                AudioServer.SetBusName(AudioServer.BusCount - 1, "SFX");
                AudioServer.SetBusSend(AudioServer.GetBusIndex("SFX"), "Master");
            }
            if (AudioServer.GetBusIndex("Music") == -1)
            {
                AudioServer.AddBus();
                AudioServer.SetBusName(AudioServer.BusCount - 1, "Music");
                AudioServer.SetBusSend(AudioServer.GetBusIndex("Music"), "Master");
            }
        }

        /// <summary>
        /// Applies current volume settings to audio buses. Called on startup
        /// and whenever the user changes volume sliders in SettingsPanel.
        /// </summary>
        public void ApplyAudioSettings()
        {
            int master = AudioServer.GetBusIndex("Master");
            int sfx = AudioServer.GetBusIndex("SFX");
            int music = AudioServer.GetBusIndex("Music");

            AudioServer.SetBusVolumeDb(master, Mathf.LinearToDb(Settings.MasterVolume));
            if (sfx >= 0) AudioServer.SetBusVolumeDb(sfx, Mathf.LinearToDb(Settings.SfxVolume));
            if (music >= 0) AudioServer.SetBusVolumeDb(music, Mathf.LinearToDb(Settings.MusicVolume));
        }

        /// <summary>
        /// Applies fullscreen setting. Called on startup and from SettingsPanel.
        /// </summary>
        public void ApplyFullscreen()
        {
            DisplayServer.WindowSetMode(
                Settings.Fullscreen
                    ? DisplayServer.WindowMode.Fullscreen
                    : DisplayServer.WindowMode.Windowed);
        }
    }
}
