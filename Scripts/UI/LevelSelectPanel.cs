using Godot;
using Baboomz.Simulation;
using System.Collections.Generic;

namespace Baboomz
{
    /// <summary>
    /// Campaign level select — scrollable list of 28 levels grouped by world.
    /// Shows star ratings, lock state, and level name. Selecting a level
    /// sets GameModeContext.SelectedLevelId and loads Main.tscn.
    /// </summary>
    public partial class LevelSelectPanel : Control
    {
        private Control _overlay;
        private VBoxContainer _levelList;
        private Label _detailTitle;
        private Label _detailDescription;
        private Label _detailStars;
        private Button _playButton;
        private string _selectedLevelId;
        private ProgressionService _progression;

        private static readonly string[] WorldNames =
            { "Verdant Fields", "Scorched Sands", "Frozen Peaks", "Molten Forge", "Sky Citadel" };
        private static readonly Color[] WorldColors =
        {
            new Color(0.3f, 0.6f, 0.3f),
            new Color(0.7f, 0.5f, 0.2f),
            new Color(0.4f, 0.6f, 0.8f),
            new Color(0.7f, 0.3f, 0.2f),
            new Color(0.5f, 0.4f, 0.7f)
        };

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
            _progression = new ProgressionService();
            BuildUI();
        }

        private void BuildUI()
        {
            _overlay = new ColorRect();
            _overlay.Name = "LevelOverlay";
            ((ColorRect)_overlay).Color = new Color(0.05f, 0.04f, 0.08f, 0.97f);
            UIBuilder.SetAnchors(_overlay, Vector2.Zero, Vector2.One);
            AddChild(_overlay);

            UIBuilder.CreateLabel("CAMPAIGN", 36, UIBuilder.UiGold,
                _overlay, new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.07f),
                HorizontalAlignment.Center);

            var backBtn = UIBuilder.CreateButton("LevelBack", "< Back", 18,
                new Color(0.5f, 0.3f, 0.3f), _overlay);
            UIBuilder.SetAnchors(backBtn, new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.06f));
            backBtn.Pressed += () => Visible = false;

            // Left panel: scrollable level list
            var leftPanel = UIBuilder.CreatePanel("LevelList",
                new Color(0.1f, 0.08f, 0.14f, 0.9f), _overlay,
                new Vector2(0.02f, 0.08f), new Vector2(0.4f, 0.96f));

            var scroll = new ScrollContainer();
            scroll.Name = "LevelScroll";
            UIBuilder.SetAnchors(scroll, Vector2.Zero, Vector2.One);
            leftPanel.AddChild(scroll);

            _levelList = new VBoxContainer();
            _levelList.Name = "LevelEntries";
            scroll.AddChild(_levelList);

            // Right panel: level details
            var rightPanel = UIBuilder.CreatePanel("LevelDetail",
                new Color(0.12f, 0.1f, 0.16f, 0.9f), _overlay,
                new Vector2(0.42f, 0.08f), new Vector2(0.98f, 0.96f));

            _detailTitle = UIBuilder.CreateLabel("Select a level", 28, UIBuilder.UiGold,
                rightPanel, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.12f),
                HorizontalAlignment.Left);

            _detailDescription = UIBuilder.CreateLabel("", 15,
                new Color(0.85f, 0.85f, 0.85f),
                rightPanel, new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.55f),
                HorizontalAlignment.Left);
            _detailDescription.VerticalAlignment = VerticalAlignment.Top;
            _detailDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;

            _detailStars = UIBuilder.CreateLabel("", 20,
                new Color(1f, 0.84f, 0f),
                rightPanel, new Vector2(0.03f, 0.57f), new Vector2(0.97f, 0.65f),
                HorizontalAlignment.Left);

            _playButton = UIBuilder.CreateButton("PlayLevel", "PLAY LEVEL", 24,
                new Color(0.2f, 0.6f, 0.3f), rightPanel);
            UIBuilder.SetAnchors(_playButton,
                new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.88f));
            _playButton.Pressed += OnPlayLevel;
            _playButton.Visible = false;

            PopulateLevels();
        }

        private void PopulateLevels()
        {
            // Load all level files from Resources/Levels/
            var levels = LoadLevelManifest();

            // Group by world
            for (int w = 0; w < 5; w++)
            {
                int worldIdx = w;
                string worldName = w < WorldNames.Length ? WorldNames[w] : $"World {w + 1}";
                Color worldColor = w < WorldColors.Length ? WorldColors[w] : Colors.Gray;

                // World header
                var header = new Label();
                header.Text = $"--- {worldName} ---";
                header.AddThemeFontSizeOverride("font_size", 16);
                header.AddThemeColorOverride("font_color", worldColor);
                header.CustomMinimumSize = new Vector2(0, 30);
                header.HorizontalAlignment = HorizontalAlignment.Center;
                _levelList.AddChild(header);

                // Levels for this world
                foreach (var level in levels)
                {
                    if (level.WorldIndex != worldIdx) continue;

                    string prevLevelId = level.LevelIndex > 0
                        ? $"w{worldIdx + 1}_l{level.LevelIndex}"
                        : null;
                    string[] required = prevLevelId != null
                        ? new[] { prevLevelId } : System.Array.Empty<string>();

                    bool unlocked = _progression.IsLevelUnlocked(level.Id, required);
                    int stars = _progression.GetLevelStars(level.Id);

                    string starStr = stars > 0
                        ? new string('*', stars) + new string('.', 3 - stars)
                        : "...";

                    var btn = new Button();
                    btn.Text = unlocked
                        ? $"[{starStr}] {level.Name}"
                        : $"[LOCKED] {level.Name}";
                    btn.AddThemeFontSizeOverride("font_size", 14);
                    btn.CustomMinimumSize = new Vector2(0, 36);
                    btn.Alignment = HorizontalAlignment.Left;
                    btn.Disabled = !unlocked;

                    var normalStyle = new StyleBoxFlat();
                    normalStyle.BgColor = unlocked
                        ? new Color(0.15f, 0.12f, 0.2f)
                        : new Color(0.1f, 0.1f, 0.1f, 0.5f);
                    normalStyle.SetCornerRadiusAll(2);
                    btn.AddThemeStyleboxOverride("normal", normalStyle);

                    if (unlocked)
                    {
                        var hover = new StyleBoxFlat();
                        hover.BgColor = new Color(0.22f, 0.18f, 0.28f);
                        hover.SetCornerRadiusAll(2);
                        btn.AddThemeStyleboxOverride("hover", hover);
                    }

                    var captured = level;
                    btn.Pressed += () => SelectLevel(captured);
                    _levelList.AddChild(btn);
                }
            }
        }

        private void SelectLevel(LevelInfo level)
        {
            _selectedLevelId = level.Id;
            _detailTitle.Text = level.Name;
            _detailDescription.Text = level.Intro ?? "";
            int stars = _progression.GetLevelStars(level.Id);
            _detailStars.Text = stars > 0
                ? $"Best: {new string('*', stars)}{new string('.', 3 - stars)}"
                : "Not yet completed";
            _playButton.Visible = true;
        }

        private void OnPlayLevel()
        {
            if (string.IsNullOrEmpty(_selectedLevelId)) return;
            GameModeContext.SelectedLevelId = _selectedLevelId;
            Visible = false;
            GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
        }

        public new void Show()
        {
            _progression.Reload();
            Visible = true;
        }

        private struct LevelInfo
        {
            public string Id;
            public string Name;
            public int WorldIndex;
            public int LevelIndex;
            public string Intro;
        }

        private static List<LevelInfo> LoadLevelManifest()
        {
            var levels = new List<LevelInfo>();
            var dir = DirAccess.Open("res://Resources/Levels");
            if (dir == null) return levels;

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (!string.IsNullOrEmpty(fileName))
            {
                if (fileName.EndsWith(".json") && fileName.StartsWith("w"))
                {
                    string path = $"res://Resources/Levels/{fileName}";
                    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                    if (file != null)
                    {
                        string json = file.GetAsText();
                        var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        levels.Add(new LevelInfo
                        {
                            Id = root.GetProperty("id").GetString(),
                            Name = root.GetProperty("name").GetString(),
                            WorldIndex = root.GetProperty("worldIndex").GetInt32(),
                            LevelIndex = root.GetProperty("levelIndex").GetInt32(),
                            Intro = root.TryGetProperty("introDialog", out var intro)
                                ? intro.GetString() : ""
                        });
                    }
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();

            levels.Sort((a, b) =>
                a.WorldIndex != b.WorldIndex
                    ? a.WorldIndex.CompareTo(b.WorldIndex)
                    : a.LevelIndex.CompareTo(b.LevelIndex));
            return levels;
        }
    }
}
