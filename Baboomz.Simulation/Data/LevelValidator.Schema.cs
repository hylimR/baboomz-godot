using System.Text.Json;

namespace Baboomz
{
    public static partial class LevelValidator
    {
        // ── Terrain + spawn + objective-root schema checks ────────────────────

        static void ValidateTerrain(JsonElement terrain, Report r)
        {
            if (terrain.ValueKind != JsonValueKind.Object)
            {
                r.Issues.Add(new Issue(Severity.Error, "terrain", "must be an object"));
                return;
            }
            if (TryGetProperty(terrain, "mapWidth", out var w) && w.ValueKind == JsonValueKind.Number)
            {
                int v = w.GetInt32();
                if (v <= 0) r.Issues.Add(new Issue(Severity.Error, "terrain.mapWidth", $"must be > 0 (got {v})"));
            }
            if (TryGetProperty(terrain, "minHeight", out var min) && TryGetProperty(terrain, "maxHeight", out var max)
                && min.ValueKind == JsonValueKind.Number && max.ValueKind == JsonValueKind.Number)
            {
                float minH = min.GetSingle();
                float maxH = max.GetSingle();
                if (minH < 0f || minH > 1f)
                    r.Issues.Add(new Issue(Severity.Error, "terrain.minHeight", $"must be in [0,1] (got {minH})"));
                if (maxH < 0f || maxH > 1f)
                    r.Issues.Add(new Issue(Severity.Error, "terrain.maxHeight", $"must be in [0,1] (got {maxH})"));
                if (minH > maxH)
                    r.Issues.Add(new Issue(Severity.Error, "terrain.minHeight",
                        $"must be <= maxHeight (min={minH} max={maxH})"));
            }
            if (TryGetProperty(terrain, "indestructibleFloorHeight", out var idh) && idh.ValueKind == JsonValueKind.Number)
            {
                float v = idh.GetSingle();
                if (v < 0f || v > 1f)
                    r.Issues.Add(new Issue(Severity.Error, "terrain.indestructibleFloorHeight",
                        $"must be in [0,1] (got {v})"));
            }
        }

        static void ValidateSpawn(JsonElement spawn, Report r)
        {
            if (spawn.ValueKind != JsonValueKind.Object)
            {
                r.Issues.Add(new Issue(Severity.Error, "playerSpawn", "must be an object"));
                return;
            }
            if (!TryGetProperty(spawn, "x", out _))
                r.Issues.Add(new Issue(Severity.Warning, "playerSpawn.x", "missing (defaults to 0)"));
        }

        static void ValidateObjectives(JsonElement objectives, JsonElement root, Report r)
        {
            if (objectives.ValueKind != JsonValueKind.Object)
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives", "must be an object"));
                return;
            }
            string type = "";
            if (TryGetProperty(objectives, "type", out var t) && t.ValueKind == JsonValueKind.String)
                type = t.GetString() ?? "";

            if (string.IsNullOrEmpty(type))
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.type", "missing or empty"));
                return;
            }
            if (!Contains(KnownObjectiveTypes, type))
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.type",
                    $"unknown type '{type}' — expected one of: {string.Join(", ", KnownObjectiveTypes)}"));
                return;
            }

            switch (type)
            {
                case "survive_time": ValidateSurviveTime(objectives, r); break;
                case "survive_waves": ValidateSurviveWaves(objectives, r); break;
                case "destroy_target": ValidateDestroyTarget(objectives, root, r); break;
                case "defeat_boss": ValidateDefeatBoss(objectives, root, r); break;
            }
        }

        static void ValidateSurviveTime(JsonElement objectives, Report r)
        {
            if (!TryGetProperty(objectives, "timeLimit", out var tl) || tl.ValueKind != JsonValueKind.Number
                || tl.GetSingle() <= 0f)
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.timeLimit",
                    "survive_time requires timeLimit > 0"));
            }
        }

        static void ValidateSurviveWaves(JsonElement objectives, Report r)
        {
            int waveCount = 0;
            if (TryGetProperty(objectives, "waveCount", out var wc) && wc.ValueKind == JsonValueKind.Number)
                waveCount = wc.GetInt32();
            if (waveCount <= 0)
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.waveCount",
                    "survive_waves requires waveCount > 0"));
            }
            if (!TryGetProperty(objectives, "waves", out var waves) || waves.ValueKind != JsonValueKind.Array)
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.waves",
                    "survive_waves requires waves array"));
                return;
            }
            if (waves.GetArrayLength() != waveCount)
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.waves",
                    $"waves.length ({waves.GetArrayLength()}) must match waveCount ({waveCount})"));
                return;
            }
            int i = 0;
            foreach (var wave in waves.EnumerateArray())
            {
                if (TryGetProperty(wave, "enemies", out var we))
                    ValidateEnemyArray(we, $"objectives.waves[{i}].enemies", r);
                i++;
            }
        }

        static void ValidateDestroyTarget(JsonElement objectives, JsonElement root, Report r)
        {
            int targetCount = 0;
            if (TryGetProperty(objectives, "targetCount", out var tc) && tc.ValueKind == JsonValueKind.Number)
                targetCount = tc.GetInt32();
            if (targetCount <= 0)
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.targetCount",
                    "destroy_target requires targetCount > 0"));
            }
            int actualTargets = 0;
            if (TryGetProperty(root, "structures", out var structs) && structs.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in structs.EnumerateArray())
                {
                    if (TryGetProperty(s, "type", out var st) && st.ValueKind == JsonValueKind.String
                        && st.GetString() == "target")
                        actualTargets++;
                }
            }
            if (targetCount > 0 && actualTargets != targetCount)
            {
                r.Issues.Add(new Issue(Severity.Warning, "objectives.targetCount",
                    $"targetCount={targetCount} but {actualTargets} structures of type 'target' found"));
            }
        }

        static void ValidateDefeatBoss(JsonElement objectives, JsonElement root, Report r)
        {
            string bossType = "";
            if (TryGetProperty(objectives, "bossType", out var bt) && bt.ValueKind == JsonValueKind.String)
                bossType = bt.GetString() ?? "";
            if (string.IsNullOrEmpty(bossType))
            {
                r.Issues.Add(new Issue(Severity.Error, "objectives.bossType",
                    "defeat_boss requires non-empty bossType"));
            }
            if (!TryGetProperty(root, "boss", out _))
            {
                r.Issues.Add(new Issue(Severity.Warning, "boss",
                    "defeat_boss objective set but no 'boss' block defined"));
            }
        }
    }
}
