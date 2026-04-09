using Godot;

namespace Baboomz
{
    /// <summary>
    /// Main menu screen — code-driven UI (no .tscn UI elements).
    /// Extends Control, fills screen. Sets GameModeContext before scene change.
    /// </summary>
    public partial class MainMenuSetup : Control
    {
        private static readonly string[] PlayerNames =
            { "Player1", "Ace", "Blaze", "Storm", "Jinx", "Nova", "Spike", "Echo", "Bolt", "Fury" };

        private int _nameIndex;
        private Label _nameDisplayLabel;

        private Button _easyBtn;
        private Button _normalBtn;
        private Button _hardBtn;

        private readonly Color _easyColor = new Color(0.3f, 0.6f, 0.3f);
        private readonly Color _normalColor = new Color(0.5f, 0.5f, 0.2f);
        private readonly Color _hardColor = new Color(0.6f, 0.3f, 0.3f);

        public override void _Ready()
        {
            // Dark background
            RenderingServer.SetDefaultClearColor(new Color(0.08f, 0.06f, 0.12f));

            BuildUI();
        }

        private void BuildUI()
        {
            // Title
            var title = UIBuilder.CreateLabel("BABOOMZ", 72, UIBuilder.UiGold,
                this, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.15f),
                HorizontalAlignment.Center);
            title.VerticalAlignment = VerticalAlignment.Center;
            title.AddThemeConstantOverride("outline_size", 6);
            title.AddThemeColorOverride("font_outline_color", new Color(0.3f, 0.1f, 0f));

            // Subtitle
            var subtitle = UIBuilder.CreateLabel("Real-Time 2D Artillery Game", 22, Colors.White,
                this, new Vector2(0.25f, 0.14f), new Vector2(0.75f, 0.20f),
                HorizontalAlignment.Center);
            subtitle.VerticalAlignment = VerticalAlignment.Center;

            // --- Player name selector ---
            float y = 0.24f;
            UIBuilder.CreateLabel("NAME", 16, new Color(0.8f, 0.8f, 0.8f),
                this, new Vector2(0.3f, y), new Vector2(0.7f, y + 0.03f),
                HorizontalAlignment.Center);

            y += 0.03f;
            var leftBtn = UIBuilder.CreateButton("NameLeft", "<", 24,
                new Color(0.3f, 0.3f, 0.4f), this);
            UIBuilder.SetAnchors(leftBtn, new Vector2(0.32f, y), new Vector2(0.38f, y + 0.05f));
            leftBtn.Pressed += () => CycleName(-1);

            _nameDisplayLabel = UIBuilder.CreateLabel(PlayerNames[0], 24, UIBuilder.UiGold,
                this, new Vector2(0.38f, y), new Vector2(0.62f, y + 0.05f),
                HorizontalAlignment.Center);
            _nameDisplayLabel.VerticalAlignment = VerticalAlignment.Center;

            var rightBtn = UIBuilder.CreateButton("NameRight", ">", 24,
                new Color(0.3f, 0.3f, 0.4f), this);
            UIBuilder.SetAnchors(rightBtn, new Vector2(0.62f, y), new Vector2(0.68f, y + 0.05f));
            rightBtn.Pressed += () => CycleName(1);

            // --- Difficulty ---
            y += 0.07f;
            UIBuilder.CreateLabel("DIFFICULTY", 16, new Color(0.8f, 0.8f, 0.8f),
                this, new Vector2(0.3f, y), new Vector2(0.7f, y + 0.03f),
                HorizontalAlignment.Center);

            y += 0.03f;
            float btnW = 0.12f;
            float btnGap = 0.015f;
            float startX = 0.5f - (btnW * 1.5f + btnGap);

            _easyBtn = UIBuilder.CreateButton("EasyBtn", "Easy", 18, _easyColor, this);
            UIBuilder.SetAnchors(_easyBtn, new Vector2(startX, y), new Vector2(startX + btnW, y + 0.05f));

            _normalBtn = UIBuilder.CreateButton("NormalBtn", "Normal", 18, _normalColor, this);
            UIBuilder.SetAnchors(_normalBtn,
                new Vector2(startX + btnW + btnGap, y),
                new Vector2(startX + btnW * 2 + btnGap, y + 0.05f));

            _hardBtn = UIBuilder.CreateButton("HardBtn", "Hard", 18, _hardColor, this);
            UIBuilder.SetAnchors(_hardBtn,
                new Vector2(startX + (btnW + btnGap) * 2, y),
                new Vector2(startX + btnW * 3 + btnGap * 2, y + 0.05f));

            _easyBtn.Pressed += () => SetDifficulty(Difficulty.Easy);
            _normalBtn.Pressed += () => SetDifficulty(Difficulty.Normal);
            _hardBtn.Pressed += () => SetDifficulty(Difficulty.Hard);

            // Highlight default (Normal)
            HighlightButton(_normalBtn, _normalColor);

            // --- PLAY button ---
            y += 0.08f;
            var playBtn = UIBuilder.CreateButton("PlayBtn", "PLAY", 32,
                new Color(0.2f, 0.6f, 0.3f), this);
            UIBuilder.SetAnchors(playBtn, new Vector2(0.3f, y), new Vector2(0.7f, y + 0.07f));
            playBtn.Pressed += OnPlayPressed;

            // --- QUIT button ---
            y += 0.09f;
            var quitBtn = UIBuilder.CreateButton("QuitBtn", "QUIT", 24,
                new Color(0.5f, 0.3f, 0.3f), this);
            UIBuilder.SetAnchors(quitBtn, new Vector2(0.35f, y), new Vector2(0.65f, y + 0.06f));
            quitBtn.Pressed += OnQuitPressed;

            // --- Version ---
            var version = UIBuilder.CreateLabel("v0.1.0-alpha", 12,
                new Color(0.5f, 0.5f, 0.5f, 0.5f),
                this, new Vector2(0.8f, 0.96f), new Vector2(0.99f, 1.0f),
                HorizontalAlignment.Right);
            version.AddThemeConstantOverride("outline_size", 0);

            // Initialize context defaults
            GameModeContext.PlayerName = PlayerNames[0];
            GameModeContext.SelectedDifficulty = Difficulty.Normal;
        }

        private void CycleName(int direction)
        {
            _nameIndex = (_nameIndex + direction + PlayerNames.Length) % PlayerNames.Length;
            if (_nameDisplayLabel != null)
                _nameDisplayLabel.Text = PlayerNames[_nameIndex];
            GameModeContext.PlayerName = PlayerNames[_nameIndex];
        }

        private void SetDifficulty(Difficulty diff)
        {
            GameModeContext.SelectedDifficulty = diff;

            // Reset all to base style, then highlight selected
            ResetButtonStyle(_easyBtn, _easyColor);
            ResetButtonStyle(_normalBtn, _normalColor);
            ResetButtonStyle(_hardBtn, _hardColor);

            switch (diff)
            {
                case Difficulty.Easy: HighlightButton(_easyBtn, _easyColor); break;
                case Difficulty.Normal: HighlightButton(_normalBtn, _normalColor); break;
                case Difficulty.Hard: HighlightButton(_hardBtn, _hardColor); break;
            }
        }

        private static void HighlightButton(Button btn, Color baseColor)
        {
            var style = new StyleBoxFlat();
            style.BgColor = baseColor * 1.4f;
            style.SetCornerRadiusAll(4);
            style.SetBorderWidthAll(2);
            style.BorderColor = UIBuilder.UiGold;
            btn.AddThemeStyleboxOverride("normal", style);
        }

        private static void ResetButtonStyle(Button btn, Color baseColor)
        {
            var style = new StyleBoxFlat();
            style.BgColor = baseColor;
            style.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", style);

            var hover = new StyleBoxFlat();
            hover.BgColor = baseColor * 1.2f;
            hover.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("hover", hover);
        }

        private void OnPlayPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
        }

        private void OnQuitPressed()
        {
            GetTree().Quit();
        }
    }
}
