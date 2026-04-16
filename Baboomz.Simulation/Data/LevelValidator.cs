using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Baboomz
{
    /// <summary>
    /// Schema validator for level JSON files in Resources/Levels/.
    /// Pure C# — no Godot / Unity dependency.
    /// Callable from tests, CLI, and the Godot editor plugin.
    /// Checks are split across partial files by concern:
    /// - LevelValidator.cs:        public API, types, helpers, root dispatch
    /// - LevelValidator.Schema.cs: terrain, spawn, objectives
    /// - LevelValidator.Entities.cs: enemies, structures, boss, difficulty, hidden, tutorial
    /// </summary>
    public static partial class LevelValidator
    {
        public static readonly string[] KnownObjectiveTypes =
        {
            "eliminate_all",
            "defeat_boss",
            "survive_time",
            "survive_waves",
            "destroy_target",
        };

        public static readonly string[] KnownHiddenObjectiveTypes =
        {
            "",
            "no_mines_triggered",
            "no_damage_taken",
            "speedrun",
            "cannon_only",
        };

        public static readonly string[] KnownTutorialActions =
        {
            "move_right",
            "jump",
            "aim_up",
            "charge_and_fire",
            "switch_weapon",
            "use_skill",
            "destroy_terrain",
            "kill_enemy",
        };

        public enum Severity { Ok, Warning, Error }

        public readonly struct Issue
        {
            public readonly Severity Level;
            public readonly string Field;
            public readonly string Message;

            public Issue(Severity level, string field, string message)
            {
                Level = level;
                Field = field;
                Message = message;
            }

            public override string ToString() => $"[{Level}] {Field}: {Message}";
        }

        public sealed class Report
        {
            public string FilePath { get; init; } = "";
            public List<Issue> Issues { get; } = new();
            public bool HasErrors
            {
                get
                {
                    foreach (var i in Issues)
                        if (i.Level == Severity.Error) return true;
                    return false;
                }
            }
            public int ErrorCount
            {
                get
                {
                    int c = 0;
                    foreach (var i in Issues)
                        if (i.Level == Severity.Error) c++;
                    return c;
                }
            }
            public int WarningCount
            {
                get
                {
                    int c = 0;
                    foreach (var i in Issues)
                        if (i.Level == Severity.Warning) c++;
                    return c;
                }
            }
        }

        /// <summary>Validate a JSON string. Returns a report with any issues found.</summary>
        public static Report Validate(string json, string filePath = "")
        {
            var report = new Report { FilePath = filePath };
            if (string.IsNullOrWhiteSpace(json))
            {
                report.Issues.Add(new Issue(Severity.Error, "<root>", "empty JSON"));
                return report;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                report.Issues.Add(new Issue(Severity.Error, "<root>", $"malformed JSON: {ex.Message}"));
                return report;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    report.Issues.Add(new Issue(Severity.Error, "<root>", "root must be a JSON object"));
                    return report;
                }

                ValidateRoot(root, report);
            }
            return report;
        }

        /// <summary>Convenience: read a file from disk and validate.</summary>
        public static Report ValidateFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var r = new Report { FilePath = filePath };
                r.Issues.Add(new Issue(Severity.Error, "<file>", "file does not exist"));
                return r;
            }
            var json = File.ReadAllText(filePath);
            return Validate(json, filePath);
        }

        /// <summary>Validate every *.json in a directory. Returns one report per file.</summary>
        public static List<Report> ValidateDirectory(string directory)
        {
            var reports = new List<Report>();
            if (!Directory.Exists(directory)) return reports;
            foreach (var path in Directory.GetFiles(directory, "*.json"))
            {
                reports.Add(ValidateFile(path));
            }
            return reports;
        }

        // ── Root dispatch ─────────────────────────────────────────────────────

        static void ValidateRoot(JsonElement root, Report r)
        {
            RequireString(root, "id", r, required: true);
            RequireString(root, "name", r, required: true);

            if (TryGetProperty(root, "terrain", out var terrain))
                ValidateTerrain(terrain, r);
            else
                r.Issues.Add(new Issue(Severity.Error, "terrain", "missing required object"));

            if (TryGetProperty(root, "playerSpawn", out var spawn))
                ValidateSpawn(spawn, r);
            else
                r.Issues.Add(new Issue(Severity.Warning, "playerSpawn", "missing; default will be used"));

            if (TryGetProperty(root, "objectives", out var objectives))
                ValidateObjectives(objectives, root, r);
            else
                r.Issues.Add(new Issue(Severity.Error, "objectives", "missing required object"));

            if (TryGetProperty(root, "enemies", out var enemies))
                ValidateEnemyArray(enemies, "enemies", r);

            if (TryGetProperty(root, "structures", out var structures))
                ValidateStructureArray(structures, "structures", r);

            if (TryGetProperty(root, "boss", out var boss))
                ValidateBoss(boss, r);

            if (TryGetProperty(root, "difficulty", out var difficulty))
                ValidateDifficulty(difficulty, r);

            if (TryGetProperty(root, "hiddenObjective", out var hidden))
                ValidateHiddenObjective(hidden, r);

            if (TryGetProperty(root, "tutorialSteps", out var steps))
                ValidateTutorialSteps(steps, r);

            if (TryGetProperty(root, "worldIndex", out var wi) && wi.ValueKind == JsonValueKind.Number)
            {
                int v = wi.GetInt32();
                if (v < 0) r.Issues.Add(new Issue(Severity.Error, "worldIndex", $"must be >= 0 (got {v})"));
            }
            if (TryGetProperty(root, "levelIndex", out var li) && li.ValueKind == JsonValueKind.Number)
            {
                int v = li.GetInt32();
                if (v < 0) r.Issues.Add(new Issue(Severity.Error, "levelIndex", $"must be >= 0 (got {v})"));
            }
            if (TryGetProperty(root, "parTime", out var pt) && pt.ValueKind == JsonValueKind.Number)
            {
                float v = pt.GetSingle();
                if (v <= 0f) r.Issues.Add(new Issue(Severity.Warning, "parTime", $"should be > 0 (got {v})"));
            }
        }

        // ── Shared helpers ────────────────────────────────────────────────────

        static void RequireString(JsonElement parent, string field, Report r, bool required, string pathPrefix = "")
        {
            string path = string.IsNullOrEmpty(pathPrefix) ? field : $"{pathPrefix}.{field}";
            if (!TryGetProperty(parent, field, out var el))
            {
                if (required)
                    r.Issues.Add(new Issue(Severity.Error, path, "missing required string"));
                return;
            }
            if (el.ValueKind != JsonValueKind.String)
            {
                r.Issues.Add(new Issue(Severity.Error, path, $"must be a string (got {el.ValueKind})"));
                return;
            }
            string v = el.GetString() ?? "";
            if (required && string.IsNullOrEmpty(v))
                r.Issues.Add(new Issue(Severity.Error, path, "must be non-empty"));
        }

        static bool TryGetProperty(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out value))
                return true;
            value = default;
            return false;
        }

        static bool Contains(string[] arr, string value)
        {
            foreach (var v in arr) if (v == value) return true;
            return false;
        }
    }
}
