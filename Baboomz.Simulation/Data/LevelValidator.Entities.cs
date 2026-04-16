using System.Text.Json;

namespace Baboomz
{
    public static partial class LevelValidator
    {
        // ── Entity / sub-object schema checks ─────────────────────────────────

        static void ValidateEnemyArray(JsonElement arr, string field, Report r)
        {
            if (arr.ValueKind != JsonValueKind.Array)
            {
                r.Issues.Add(new Issue(Severity.Error, field, "must be an array"));
                return;
            }
            int i = 0;
            foreach (var e in arr.EnumerateArray())
            {
                string path = $"{field}[{i}]";
                if (e.ValueKind != JsonValueKind.Object)
                {
                    r.Issues.Add(new Issue(Severity.Error, path, "must be an object"));
                }
                else
                {
                    RequireString(e, "type", r, required: true, pathPrefix: path);
                }
                i++;
            }
        }

        static void ValidateStructureArray(JsonElement arr, string field, Report r)
        {
            if (arr.ValueKind != JsonValueKind.Array)
            {
                r.Issues.Add(new Issue(Severity.Error, field, "must be an array"));
                return;
            }
            int i = 0;
            foreach (var s in arr.EnumerateArray())
            {
                string path = $"{field}[{i}]";
                if (s.ValueKind != JsonValueKind.Object)
                {
                    r.Issues.Add(new Issue(Severity.Error, path, "must be an object"));
                    i++;
                    continue;
                }
                RequireString(s, "type", r, required: true, pathPrefix: path);
                if (TryGetProperty(s, "width", out var w) && w.ValueKind == JsonValueKind.Number)
                {
                    float v = w.GetSingle();
                    if (v <= 0f) r.Issues.Add(new Issue(Severity.Error, $"{path}.width", $"must be > 0 (got {v})"));
                }
                if (TryGetProperty(s, "height", out var h) && h.ValueKind == JsonValueKind.Number)
                {
                    float v = h.GetSingle();
                    if (v <= 0f) r.Issues.Add(new Issue(Severity.Error, $"{path}.height", $"must be > 0 (got {v})"));
                }
                i++;
            }
        }

        static void ValidateBoss(JsonElement boss, Report r)
        {
            if (boss.ValueKind != JsonValueKind.Object)
            {
                r.Issues.Add(new Issue(Severity.Error, "boss", "must be an object"));
                return;
            }
            RequireString(boss, "bossType", r, required: true, pathPrefix: "boss");
            if (TryGetProperty(boss, "bossHP", out var hp) && hp.ValueKind == JsonValueKind.Number)
            {
                float v = hp.GetSingle();
                if (v <= 0f) r.Issues.Add(new Issue(Severity.Error, "boss.bossHP", $"must be > 0 (got {v})"));
            }
            if (TryGetProperty(boss, "phaseThresholds", out var pt) && pt.ValueKind == JsonValueKind.Array)
            {
                int i = 0;
                foreach (var th in pt.EnumerateArray())
                {
                    if (th.ValueKind == JsonValueKind.Number)
                    {
                        float v = th.GetSingle();
                        if (v <= 0f || v >= 1f)
                            r.Issues.Add(new Issue(Severity.Error,
                                $"boss.phaseThresholds[{i}]",
                                $"must be in (0,1) (got {v})"));
                    }
                    i++;
                }
            }
            if (TryGetProperty(boss, "reinforcements", out var rein))
                ValidateEnemyArray(rein, "boss.reinforcements", r);
        }

        static void ValidateDifficulty(JsonElement diff, Report r)
        {
            if (diff.ValueKind != JsonValueKind.Object)
            {
                r.Issues.Add(new Issue(Severity.Error, "difficulty", "must be an object"));
                return;
            }
            foreach (var field in new[] { "hpMultiplier", "damageMultiplier", "speedMultiplier" })
            {
                if (TryGetProperty(diff, field, out var m) && m.ValueKind == JsonValueKind.Number)
                {
                    float v = m.GetSingle();
                    if (v <= 0f)
                        r.Issues.Add(new Issue(Severity.Error, $"difficulty.{field}", $"must be > 0 (got {v})"));
                }
            }
        }

        static void ValidateHiddenObjective(JsonElement hidden, Report r)
        {
            if (hidden.ValueKind != JsonValueKind.Object)
            {
                r.Issues.Add(new Issue(Severity.Error, "hiddenObjective", "must be an object"));
                return;
            }
            if (TryGetProperty(hidden, "type", out var t) && t.ValueKind == JsonValueKind.String)
            {
                string v = t.GetString() ?? "";
                if (!Contains(KnownHiddenObjectiveTypes, v))
                    r.Issues.Add(new Issue(Severity.Error, "hiddenObjective.type",
                        $"unknown type '{v}' — expected one of: {string.Join(", ", KnownHiddenObjectiveTypes)}"));
            }
        }

        static void ValidateTutorialSteps(JsonElement steps, Report r)
        {
            if (steps.ValueKind != JsonValueKind.Array)
            {
                r.Issues.Add(new Issue(Severity.Error, "tutorialSteps", "must be an array"));
                return;
            }
            int i = 0;
            foreach (var s in steps.EnumerateArray())
            {
                string path = $"tutorialSteps[{i}]";
                if (s.ValueKind != JsonValueKind.Object)
                {
                    r.Issues.Add(new Issue(Severity.Error, path, "must be an object"));
                    i++;
                    continue;
                }
                if (TryGetProperty(s, "actionType", out var at) && at.ValueKind == JsonValueKind.String)
                {
                    string v = at.GetString() ?? "";
                    if (!Contains(KnownTutorialActions, v))
                        r.Issues.Add(new Issue(Severity.Error, $"{path}.actionType",
                            $"unknown actionType '{v}' — expected one of: {string.Join(", ", KnownTutorialActions)}"));
                }
                else
                {
                    r.Issues.Add(new Issue(Severity.Error, $"{path}.actionType", "missing or not a string"));
                }
                i++;
            }
        }
    }
}
