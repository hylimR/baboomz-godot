using Godot;

namespace Baboomz
{
    /// <summary>
    /// Full-screen settings overlay — volume sliders, fullscreen toggle,
    /// combat feedback toggles. Opened from MainMenu or PauseMenu.
    /// </summary>
    public partial class SettingsPanel : Control
    {
        private GameSettings _settings;
        private HSlider _masterSlider;
        private HSlider _sfxSlider;
        private HSlider _musicSlider;
        private CheckButton _fullscreenToggle;
        private CheckButton _hitMarkersToggle;
        private CheckButton _lowHealthToggle;
        private CheckButton _comboToggle;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
            MouseFilter = MouseFilterEnum.Stop;
            _settings = GameAutoload.Instance.Settings;
            BuildUI();
        }

        private void BuildUI()
        {
            // Full-screen dark overlay
            var bg = new ColorRect();
            bg.Name = "SettingsBg";
            bg.Color = new Color(0f, 0f, 0f, 0.7f);
            UIBuilder.SetAnchors(bg, Vector2.Zero, Vector2.One);
            AddChild(bg);

            // Center panel
            var panel = UIBuilder.CreatePanel("SettingsCenter",
                new Color(0.15f, 0.1f, 0.05f, 0.95f), bg,
                new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f));
            panel.MouseFilter = MouseFilterEnum.Stop;

            // Title
            var title = UIBuilder.CreateLabel("SETTINGS", 36, UIBuilder.UiGold,
                panel, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.10f),
                HorizontalAlignment.Center);
            title.VerticalAlignment = VerticalAlignment.Center;

            float y = 0.12f;

            // --- Audio section ---
            UIBuilder.CreateLabel("AUDIO", 18, new Color(0.8f, 0.8f, 0.8f),
                panel, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.05f),
                HorizontalAlignment.Left);
            y += 0.06f;

            _masterSlider = CreateVolumeRow(panel, "Master Volume", _settings.MasterVolume, ref y);
            _masterSlider.ValueChanged += v =>
            {
                _settings.MasterVolume = (float)v;
                GameAutoload.Instance.ApplyAudioSettings();
            };

            _sfxSlider = CreateVolumeRow(panel, "SFX Volume", _settings.SfxVolume, ref y);
            _sfxSlider.ValueChanged += v =>
            {
                _settings.SfxVolume = (float)v;
                GameAutoload.Instance.ApplyAudioSettings();
            };

            _musicSlider = CreateVolumeRow(panel, "Music Volume", _settings.MusicVolume, ref y);
            _musicSlider.ValueChanged += v =>
            {
                _settings.MusicVolume = (float)v;
                GameAutoload.Instance.ApplyAudioSettings();
            };

            // --- Display section ---
            y += 0.02f;
            UIBuilder.CreateLabel("DISPLAY", 18, new Color(0.8f, 0.8f, 0.8f),
                panel, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.05f),
                HorizontalAlignment.Left);
            y += 0.06f;

            _fullscreenToggle = CreateToggleRow(panel, "Fullscreen", _settings.Fullscreen, ref y);
            _fullscreenToggle.Toggled += on =>
            {
                _settings.Fullscreen = on;
                GameAutoload.Instance.ApplyFullscreen();
            };

            // --- Combat feedback section ---
            y += 0.02f;
            UIBuilder.CreateLabel("COMBAT FEEDBACK", 18, new Color(0.8f, 0.8f, 0.8f),
                panel, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.05f),
                HorizontalAlignment.Left);
            y += 0.06f;

            _hitMarkersToggle = CreateToggleRow(panel, "Hit Markers", _settings.HitMarkersEnabled, ref y);
            _hitMarkersToggle.Toggled += on => _settings.HitMarkersEnabled = on;

            _lowHealthToggle = CreateToggleRow(panel, "Low Health Overlay", _settings.LowHealthOverlayEnabled, ref y);
            _lowHealthToggle.Toggled += on => _settings.LowHealthOverlayEnabled = on;

            _comboToggle = CreateToggleRow(panel, "Combo Effects", _settings.ComboEffectsEnabled, ref y);
            _comboToggle.Toggled += on => _settings.ComboEffectsEnabled = on;

            // --- Back button ---
            y += 0.04f;
            var backBtn = UIBuilder.CreateButton("BackBtn", "BACK", 24,
                new Color(0.5f, 0.3f, 0.3f), panel);
            UIBuilder.SetAnchors(backBtn, new Vector2(0.3f, y), new Vector2(0.7f, y + 0.07f));
            backBtn.Pressed += OnBackPressed;
        }

        private static HSlider CreateVolumeRow(Control parent, string label, float value, ref float y)
        {
            UIBuilder.CreateLabel(label, 14, Colors.White,
                parent, new Vector2(0.1f, y), new Vector2(0.4f, y + 0.05f),
                HorizontalAlignment.Left);

            var slider = new HSlider();
            slider.MinValue = 0.0;
            slider.MaxValue = 1.0;
            slider.Step = 0.05;
            slider.Value = value;
            UIBuilder.SetAnchors(slider, new Vector2(0.42f, y + 0.01f), new Vector2(0.85f, y + 0.04f));
            parent.AddChild(slider);

            y += 0.06f;
            return slider;
        }

        private static CheckButton CreateToggleRow(Control parent, string label, bool value, ref float y)
        {
            var toggle = new CheckButton();
            toggle.Text = label;
            toggle.ButtonPressed = value;
            toggle.AddThemeFontSizeOverride("font_size", 14);
            toggle.AddThemeColorOverride("font_color", Colors.White);
            UIBuilder.SetAnchors(toggle, new Vector2(0.1f, y), new Vector2(0.85f, y + 0.05f));
            parent.AddChild(toggle);

            y += 0.06f;
            return toggle;
        }

        private void OnBackPressed()
        {
            _settings.Save();
            Hide();
        }

        public new void Show()
        {
            // Sync UI with current settings before showing
            _masterSlider.Value = _settings.MasterVolume;
            _sfxSlider.Value = _settings.SfxVolume;
            _musicSlider.Value = _settings.MusicVolume;
            _fullscreenToggle.ButtonPressed = _settings.Fullscreen;
            _hitMarkersToggle.ButtonPressed = _settings.HitMarkersEnabled;
            _lowHealthToggle.ButtonPressed = _settings.LowHealthOverlayEnabled;
            _comboToggle.ButtonPressed = _settings.ComboEffectsEnabled;
            Visible = true;
        }
    }
}
