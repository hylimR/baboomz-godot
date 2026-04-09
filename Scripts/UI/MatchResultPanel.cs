using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Shows after match ends — winner announcement, per-player stats,
    /// play-again and main-menu buttons. Created by GameRunner.
    /// </summary>
    public partial class MatchResultPanel : Control
    {
        private Control _overlay;
        private Label _resultLabel;
        private Label _statsLabel;

        public override void _Ready()
        {
            // Keep processing while paused so buttons remain clickable
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;

            BuildUI();
        }

        private void BuildUI()
        {
            // Full-screen dark overlay
            _overlay = new ColorRect();
            _overlay.Name = "Overlay";
            ((ColorRect)_overlay).Color = new Color(0f, 0f, 0f, 0.7f);
            UIBuilder.SetAnchors(_overlay, Vector2.Zero, Vector2.One);
            AddChild(_overlay);

            // Center panel
            var panel = UIBuilder.CreatePanel("CenterPanel",
                new Color(0.15f, 0.1f, 0.05f, 0.95f), _overlay,
                new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f));

            // Result text
            _resultLabel = UIBuilder.CreateLabel("MATCH OVER", 42, UIBuilder.UiGold,
                panel, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.18f),
                HorizontalAlignment.Center);
            _resultLabel.VerticalAlignment = VerticalAlignment.Center;

            // Stats text
            _statsLabel = UIBuilder.CreateLabel("", 16, new Color(0.85f, 0.85f, 0.85f),
                panel, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.72f),
                HorizontalAlignment.Center);
            _statsLabel.VerticalAlignment = VerticalAlignment.Top;
            _statsLabel.AddThemeConstantOverride("outline_size", 0);

            // Play Again button
            var playAgainBtn = UIBuilder.CreateButton("PlayAgainBtn", "Play Again", 24,
                new Color(0.3f, 0.6f, 0.3f), panel);
            UIBuilder.SetAnchors(playAgainBtn,
                new Vector2(0.55f, 0.78f), new Vector2(0.95f, 0.95f));
            playAgainBtn.Pressed += OnPlayAgain;

            // Main Menu button
            var menuBtn = UIBuilder.CreateButton("MainMenuBtn", "Main Menu", 20,
                new Color(0.5f, 0.3f, 0.3f), panel);
            UIBuilder.SetAnchors(menuBtn,
                new Vector2(0.05f, 0.78f), new Vector2(0.45f, 0.95f));
            menuBtn.Pressed += OnMainMenu;
        }

        /// <summary>
        /// Show result with winner name and per-player stats from state.
        /// </summary>
        public void ShowResult(GameState state)
        {
            if (state == null) return;

            // Winner text
            if (state.WinnerIndex >= 0 && state.WinnerIndex < state.Players.Length)
            {
                string winner = state.Players[state.WinnerIndex].Name ?? "Player";
                _resultLabel.Text = $"{winner} Wins!";
                _resultLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.2f));
            }
            else
            {
                _resultLabel.Text = "Draw!";
                _resultLabel.AddThemeColorOverride("font_color", UIBuilder.UiGold);
            }

            // Build stats string
            string stats = "";
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.IsMob) continue;

                float accuracy = p.ShotsFired > 0
                    ? (p.DirectHits / (float)p.ShotsFired) * 100f
                    : 0f;

                stats += $"--- {p.Name ?? $"Player {i + 1}"} ---\n";
                stats += $"  Damage Dealt: {p.TotalDamageDealt:F0}\n";
                stats += $"  Shots Fired: {p.ShotsFired}\n";
                stats += $"  Direct Hits: {p.DirectHits}\n";
                stats += $"  Accuracy: {accuracy:F1}%\n";
                stats += $"  Best Hit: {p.MaxSingleDamage:F0}\n\n";
            }
            _statsLabel.Text = stats;

            Visible = true;
        }

        private void OnPlayAgain()
        {
            Visible = false;
            GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
        }

        private void OnMainMenu()
        {
            Visible = false;
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        }
    }
}
