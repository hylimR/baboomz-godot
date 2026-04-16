#if TOOLS
using Godot;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Baboomz.Editor
{
    /// <summary>
    /// EditorPlugin that adds a "Level Preview" bottom-panel dock.
    /// Lets content authors pick a level JSON and see a textual summary
    /// (terrain params, spawn, enemy list, structure list, objective) without
    /// launching a match. Pairs with LevelValidator for fast iteration.
    /// </summary>
    [Tool]
    public partial class LevelPreviewPlugin : EditorPlugin
    {
        private const string LevelsResourcePath = "res://Resources/Levels";
        private Control _panel;
        private ItemList _levelList;
        private RichTextLabel _detail;

        public override void _EnterTree()
        {
            _panel = BuildPanel();
            // TODO(godot-4.7): migrate to AddDock(EditorDock) once API stabilizes.
#pragma warning disable CS0618
            AddControlToBottomPanel(_panel, "Level Preview");
#pragma warning restore CS0618
            RefreshLevelList();
        }

        public override void _ExitTree()
        {
            if (_panel != null)
            {
#pragma warning disable CS0618
                RemoveControlFromBottomPanel(_panel);
#pragma warning restore CS0618
                _panel.QueueFree();
                _panel = null;
            }
        }

        private Control BuildPanel()
        {
            var root = new HSplitContainer
            {
                CustomMinimumSize = new Vector2(600, 260),
            };
            root.SplitOffsets = new[] { 220 };

            // Left: level list
            var left = new VBoxContainer();
            root.AddChild(left);

            var refreshBtn = new Button { Text = "Refresh" };
            refreshBtn.Pressed += RefreshLevelList;
            left.AddChild(refreshBtn);

            _levelList = new ItemList();
            _levelList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _levelList.ItemSelected += OnLevelSelected;
            left.AddChild(_levelList);

            // Right: detail
            _detail = new RichTextLabel
            {
                BbcodeEnabled = true,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                ScrollActive = true,
                FitContent = false,
            };
            _detail.Text = "Select a level to preview.";
            root.AddChild(_detail);

            return root;
        }

        private void RefreshLevelList()
        {
            if (_levelList == null) return;
            _levelList.Clear();
            string dir = ProjectSettings.GlobalizePath(LevelsResourcePath);
            if (!Directory.Exists(dir))
            {
                _detail.Text = $"[color=red]Directory not found:[/color] {dir}";
                return;
            }
            foreach (var path in Directory.GetFiles(dir, "*.json"))
            {
                _levelList.AddItem(Path.GetFileName(path));
                _levelList.SetItemMetadata(_levelList.ItemCount - 1, path);
            }
        }

        private void OnLevelSelected(long index)
        {
            var pathVar = _levelList.GetItemMetadata((int)index);
            string path = pathVar.AsString();
            if (string.IsNullOrEmpty(path)) return;
            _detail.Text = BuildSummary(path);
        }

        private static string BuildSummary(string path)
        {
            var sb = new StringBuilder();
            string fileName = Path.GetFileName(path);
            sb.AppendLine($"[b]{fileName}[/b]");
            sb.AppendLine();

            string json;
            try { json = File.ReadAllText(path); }
            catch (IOException ex)
            {
                sb.AppendLine($"[color=red]Could not read file:[/color] {ex.Message}");
                return sb.ToString();
            }

            // Schema summary
            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException ex)
            {
                sb.AppendLine($"[color=red]Malformed JSON:[/color] {ex.Message}");
                return sb.ToString();
            }

            using (doc)
            {
                var root = doc.RootElement;
                AppendField(sb, root, "id");
                AppendField(sb, root, "name");
                AppendField(sb, root, "worldId");
                AppendField(sb, root, "worldIndex");
                AppendField(sb, root, "levelIndex");
                AppendField(sb, root, "parTime");
                sb.AppendLine();

                if (root.TryGetProperty("terrain", out var terrain))
                {
                    sb.AppendLine("[b]Terrain[/b]");
                    AppendField(sb, terrain, "seed", "  ");
                    AppendField(sb, terrain, "mapWidth", "  ");
                    AppendField(sb, terrain, "minHeight", "  ");
                    AppendField(sb, terrain, "maxHeight", "  ");
                    AppendField(sb, terrain, "hillFrequency", "  ");
                    AppendField(sb, terrain, "islandCount", "  ");
                    AppendField(sb, terrain, "indestructibleFloorHeight", "  ");
                    sb.AppendLine();
                }

                if (root.TryGetProperty("playerSpawn", out var spawn))
                {
                    sb.Append("[b]Player Spawn[/b]  ");
                    AppendField(sb, spawn, "x", "");
                    AppendField(sb, spawn, "groundOffset", "");
                    sb.AppendLine();
                }

                if (root.TryGetProperty("objectives", out var obj))
                {
                    sb.AppendLine("[b]Objective[/b]");
                    AppendField(sb, obj, "type", "  ");
                    AppendField(sb, obj, "timeLimit", "  ");
                    AppendField(sb, obj, "waveCount", "  ");
                    AppendField(sb, obj, "targetCount", "  ");
                    AppendField(sb, obj, "bossType", "  ");
                    sb.AppendLine();
                }

                if (root.TryGetProperty("enemies", out var enemies) && enemies.ValueKind == JsonValueKind.Array)
                {
                    sb.AppendLine($"[b]Enemies[/b] ({enemies.GetArrayLength()})");
                    foreach (var e in enemies.EnumerateArray())
                        sb.AppendLine("  - " + SummarizeEnemy(e));
                    sb.AppendLine();
                }

                if (root.TryGetProperty("structures", out var structs) && structs.ValueKind == JsonValueKind.Array)
                {
                    sb.AppendLine($"[b]Structures[/b] ({structs.GetArrayLength()})");
                    foreach (var s in structs.EnumerateArray())
                        sb.AppendLine("  - " + SummarizeStructure(s));
                    sb.AppendLine();
                }

                // Run the validator and attach its issues
                var report = LevelValidator.Validate(json, path);
                sb.AppendLine($"[b]Validator[/b]  {report.ErrorCount} error(s), {report.WarningCount} warning(s)");
                foreach (var issue in report.Issues)
                {
                    string color = issue.Level == LevelValidator.Severity.Error ? "red" : "yellow";
                    sb.AppendLine($"  [color={color}]{issue}[/color]");
                }
            }
            return sb.ToString();
        }

        private static void AppendField(StringBuilder sb, JsonElement parent, string field, string prefix = "")
        {
            if (parent.ValueKind != JsonValueKind.Object) return;
            if (!parent.TryGetProperty(field, out var el)) return;
            string value = el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? "",
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => el.GetRawText(),
            };
            sb.AppendLine($"{prefix}{field}: {value}");
        }

        private static string SummarizeEnemy(JsonElement e)
        {
            string type = GetString(e, "type");
            string x = GetString(e, "x");
            string diff = GetString(e, "difficulty");
            return $"{type} @ x={x} [{diff}]";
        }

        private static string SummarizeStructure(JsonElement s)
        {
            string type = GetString(s, "type");
            string x = GetString(s, "x");
            string y = GetString(s, "y");
            string w = GetString(s, "width");
            string h = GetString(s, "height");
            return $"{type} @ ({x},{y})  {w}x{h}";
        }

        private static string GetString(JsonElement parent, string field)
        {
            if (parent.ValueKind != JsonValueKind.Object) return "?";
            if (!parent.TryGetProperty(field, out var el)) return "?";
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? "",
                _ => el.GetRawText(),
            };
        }
    }
}
#endif
