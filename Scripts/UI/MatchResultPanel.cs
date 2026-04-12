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
        private Control _centerPanel;
        private Label _resultLabel;
        private Label _statsLabel;
        private Label _progressionLabel;
        private ColorRect _rankBarBg;
        private ColorRect _rankBarFill;
        private Label _rankLabel;

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

            // Center panel — taller to fit progression section
            var panel = UIBuilder.CreatePanel("CenterPanel",
                new Color(0.15f, 0.1f, 0.05f, 0.95f), _overlay,
                new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.95f));
            _centerPanel = panel;

            // Result text
            _resultLabel = UIBuilder.CreateLabel("MATCH OVER", 42, UIBuilder.UiGold,
                panel, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.18f),
                HorizontalAlignment.Center);
            _resultLabel.VerticalAlignment = VerticalAlignment.Center;

            // Stats text (left half)
            _statsLabel = UIBuilder.CreateLabel("", 14, new Color(0.85f, 0.85f, 0.85f),
                panel, new Vector2(0.02f, 0.14f), new Vector2(0.48f, 0.72f),
                HorizontalAlignment.Left);
            _statsLabel.VerticalAlignment = VerticalAlignment.Top;
            _statsLabel.AddThemeConstantOverride("outline_size", 0);

            // Progression text (right half)
            _progressionLabel = UIBuilder.CreateLabel("", 14, new Color(0.9f, 0.85f, 0.7f),
                panel, new Vector2(0.52f, 0.14f), new Vector2(0.98f, 0.72f),
                HorizontalAlignment.Left);
            _progressionLabel.VerticalAlignment = VerticalAlignment.Top;
            _progressionLabel.AddThemeConstantOverride("outline_size", 0);

            // Rank progress bar (bottom-center, above buttons)
            _rankBarBg = UIBuilder.CreatePanel("RankBarBg",
                new Color(0.2f, 0.2f, 0.2f, 0.8f), panel,
                new Vector2(0.15f, 0.74f), new Vector2(0.85f, 0.78f));
            _rankBarFill = new ColorRect();
            _rankBarFill.Name = "RankBarFill";
            _rankBarFill.Color = UIBuilder.UiGold;
            UIBuilder.SetAnchors(_rankBarFill, new Vector2(0f, 0f), new Vector2(0f, 1f));
            _rankBarBg.AddChild(_rankBarFill);

            _rankLabel = UIBuilder.CreateLabel("", 12, Colors.White,
                panel, new Vector2(0.15f, 0.78f), new Vector2(0.85f, 0.82f),
                HorizontalAlignment.Center);

            // Watch Replay button (center)
            var replayBtn = UIBuilder.CreateButton("ReplayBtn", "Watch Replay", 18,
                new Color(0.3f, 0.3f, 0.6f), panel);
            UIBuilder.SetAnchors(replayBtn,
                new Vector2(0.3f, 0.83f), new Vector2(0.7f, 0.9f));
            replayBtn.Pressed += OnWatchReplay;

            // Play Again button
            var playAgainBtn = UIBuilder.CreateButton("PlayAgainBtn", "Play Again", 20,
                new Color(0.3f, 0.6f, 0.3f), panel);
            UIBuilder.SetAnchors(playAgainBtn,
                new Vector2(0.55f, 0.91f), new Vector2(0.95f, 0.98f));
            playAgainBtn.Pressed += OnPlayAgain;

            // Main Menu button
            var menuBtn = UIBuilder.CreateButton("MainMenuBtn", "Main Menu", 18,
                new Color(0.5f, 0.3f, 0.3f), panel);
            UIBuilder.SetAnchors(menuBtn,
                new Vector2(0.05f, 0.91f), new Vector2(0.45f, 0.98f));
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

            // Compute and display progression for player 0 (local player)
            ShowProgression(state);

            Visible = true;
        }

        private void OnWatchReplay()
        {
            var replayData = GameRunner.LastReplayData;
            if (replayData == null || replayData.Frames.Count == 0) return;

            Visible = false;
            var runner = GetParent()?.GetParent() as GameRunner;
            runner?.StartReplayPlayback(replayData);
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
