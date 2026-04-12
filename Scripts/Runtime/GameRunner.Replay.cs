using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Replay recording and playback support for GameRunner.
    /// Records input frames during normal play, saves to user://replays/,
    /// and can play back a match using ReplayPlayer.
    /// </summary>
    public partial class GameRunner
    {
        private ReplayData _currentReplay;
        private ReplayPlayer _replayPlayer;
        private bool _isReplayMode;

        /// <summary>
        /// Shared storage for last replay data — set on match end,
        /// read by MatchResultPanel to enable "Watch Replay" button.
        /// </summary>
        public static ReplayData LastReplayData { get; set; }

        private void StartRecording()
        {
            _currentReplay = ReplaySystem.StartRecording(State);
        }

        private void StopAndSaveRecording()
        {
            if (State.ReplayRecording == null) return;
            _currentReplay = ReplaySystem.StopRecording(State);
            LastReplayData = _currentReplay;
            SaveReplayToFile(_currentReplay);
        }

        private static void SaveReplayToFile(ReplayData data)
        {
            if (data == null || data.Frames.Count == 0) return;

            // Ensure replay directory exists
            DirAccess.MakeDirRecursiveAbsolute("user://replays");

            // Save as JSON — rotate last 3 replays
            string basePath = "user://replays/replay";
            // Shift existing replays: 2→3(delete), 1→2, 0→1
            if (FileAccess.FileExists($"{basePath}_2.json"))
                DirAccess.RemoveAbsolute($"{basePath}_2.json");
            if (FileAccess.FileExists($"{basePath}_1.json"))
                DirAccess.RenameAbsolute($"{basePath}_1.json", $"{basePath}_2.json");
            if (FileAccess.FileExists($"{basePath}_0.json"))
                DirAccess.RenameAbsolute($"{basePath}_0.json", $"{basePath}_1.json");

            // Save current replay as replay_0
            string json = System.Text.Json.JsonSerializer.Serialize(data);
            using var file = FileAccess.Open($"{basePath}_0.json", FileAccess.ModeFlags.Write);
            file?.StoreString(json);
        }

        /// <summary>
        /// Start a replay playback. Called from MatchResultPanel "Watch Replay" button.
        /// </summary>
        public void StartReplayPlayback(ReplayData replayData)
        {
            if (replayData == null || replayData.Frames.Count == 0) return;

            _isReplayMode = true;
            _replayPlayer = new ReplayPlayer(replayData);
            _replayPlayer.Speed = 1f;

            // Replace state with replay state
            State = _replayPlayer.State;

            // Clear and rebuild all renderers with replay state
            foreach (var child in GetChildren())
            {
                if (child is Node n && n.Name != "ReplayHUD")
                    n.QueueFree();
            }
            _projectileRenderers.Clear();
            _knownProjectileIds.Clear();
            _matchResultShown = false;
            _matchEndTimer = -1f;

            SetupAll();
            BuildReplayHUD();
        }

        private void ProcessReplayTick(float delta)
        {
            if (_replayPlayer == null || _replayPlayer.IsFinished) return;

            _replayPlayer.Tick(delta);
            // Keep GameRunner's State reference in sync
            State = _replayPlayer.State;
            SyncProjectileRenderers();
        }

        private Label _replaySpeedLabel;
        private Label _replayProgressLabel;

        private void BuildReplayHUD()
        {
            var hudLayer = new CanvasLayer();
            hudLayer.Name = "ReplayHUD";
            hudLayer.Layer = 200;
            AddChild(hudLayer);

            var root = new Control();
            root.Name = "ReplayRoot";
            UIBuilder.SetAnchors(root, Vector2.Zero, Vector2.One);
            hudLayer.AddChild(root);

            var bar = UIBuilder.CreatePanel("ReplayBar",
                new Color(0f, 0f, 0f, 0.7f), root,
                new Vector2(0f, 0f), new Vector2(1f, 0.06f));

            UIBuilder.CreateLabel("REPLAY", 20, UIBuilder.UiGold,
                bar, new Vector2(0.01f, 0f), new Vector2(0.12f, 1f),
                HorizontalAlignment.Left);

            _replaySpeedLabel = UIBuilder.CreateLabel("1x", 16, Colors.White,
                bar, new Vector2(0.13f, 0f), new Vector2(0.2f, 1f),
                HorizontalAlignment.Center);

            _replayProgressLabel = UIBuilder.CreateLabel("0 / 0", 14,
                new Color(0.7f, 0.7f, 0.7f),
                bar, new Vector2(0.7f, 0f), new Vector2(0.99f, 1f),
                HorizontalAlignment.Right);

            // Speed buttons
            var speed1 = UIBuilder.CreateButton("Speed1x", "1x", 14,
                new Color(0.3f, 0.5f, 0.3f), bar);
            UIBuilder.SetAnchors(speed1, new Vector2(0.22f, 0.1f), new Vector2(0.30f, 0.9f));
            speed1.Pressed += () => SetReplaySpeed(1f);

            var speed2 = UIBuilder.CreateButton("Speed2x", "2x", 14,
                new Color(0.4f, 0.5f, 0.3f), bar);
            UIBuilder.SetAnchors(speed2, new Vector2(0.31f, 0.1f), new Vector2(0.39f, 0.9f));
            speed2.Pressed += () => SetReplaySpeed(2f);

            var speed4 = UIBuilder.CreateButton("Speed4x", "4x", 14,
                new Color(0.5f, 0.5f, 0.2f), bar);
            UIBuilder.SetAnchors(speed4, new Vector2(0.40f, 0.1f), new Vector2(0.48f, 0.9f));
            speed4.Pressed += () => SetReplaySpeed(4f);

            var pauseBtn = UIBuilder.CreateButton("PauseReplay", "||", 14,
                new Color(0.5f, 0.3f, 0.3f), bar);
            UIBuilder.SetAnchors(pauseBtn, new Vector2(0.50f, 0.1f), new Vector2(0.58f, 0.9f));
            pauseBtn.Pressed += () => _replayPlayer?.TogglePause();

            var exitBtn = UIBuilder.CreateButton("ExitReplay", "Exit", 14,
                new Color(0.5f, 0.2f, 0.2f), bar);
            UIBuilder.SetAnchors(exitBtn, new Vector2(0.60f, 0.1f), new Vector2(0.68f, 0.9f));
            exitBtn.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        }

        private void SetReplaySpeed(float speed)
        {
            if (_replayPlayer == null) return;
            _replayPlayer.Speed = speed;
            if (_replaySpeedLabel != null)
                _replaySpeedLabel.Text = $"{speed}x";
        }

        private void UpdateReplayHUD()
        {
            if (_replayPlayer == null || _replayProgressLabel == null) return;
            _replayProgressLabel.Text =
                $"Frame {_replayPlayer.FrameIndex} / {_replayPlayer.TotalFrames}";
        }
    }
}
