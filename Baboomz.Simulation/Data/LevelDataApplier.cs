using System.Text.Json;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Pure-C# applier that reads a level JSON (see LevelData schema) and maps
    /// the fields that are meaningful at match-creation time onto a GameConfig.
    ///
    /// This is the minimal #162 fix: before this, LevelSelectPanel set
    /// GameModeContext.SelectedLevelId but nothing read it, so every campaign
    /// level silently fell through to a default random match. This applier
    /// makes the selection materially change the match (map width, spawn,
    /// difficulty scaling, MatchType).
    ///
    /// Fuller level wiring — spawning the enemies[], structures[], and
    /// enforcing objectives — is tracked separately; this method is the
    /// foundation those will build on.
    /// </summary>
    public static class LevelDataApplier
    {
        public readonly struct ApplyResult
        {
            public readonly bool Applied;
            public readonly int? TerrainSeed;
            public readonly string LevelId;
            public readonly TutorialStepDef[] TutorialSteps;

            public ApplyResult(bool applied, int? seed, string levelId,
                TutorialStepDef[] tutorialSteps = null)
            {
                Applied = applied;
                TerrainSeed = seed;
                LevelId = levelId;
                TutorialSteps = tutorialSteps;
            }
        }

        /// <summary>
        /// Parse <paramref name="json"/> (a level file's contents) and write the
        /// match-relevant fields into <paramref name="cfg"/>. Returns
        /// <c>Applied = false</c> when the JSON is null/empty/invalid — caller
        /// should fall back to the existing default-match path.
        /// </summary>
        public static ApplyResult Apply(string json, GameConfig cfg)
        {
            if (cfg == null) return new ApplyResult(false, null, "");
            if (string.IsNullOrWhiteSpace(json)) return new ApplyResult(false, null, "");

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch { return new ApplyResult(false, null, ""); }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return new ApplyResult(false, null, "");

                // Every campaign level plays as MatchType.Campaign. Call sites
                // that rely on other match types (KOTH, Survival, etc.) must
                // set MatchType themselves after applying.
                cfg.MatchType = MatchType.Campaign;

                string levelId = TryString(root, "id") ?? "";
                int? seed = null;

                // Terrain
                if (root.TryGetProperty("terrain", out var terrain) &&
                    terrain.ValueKind == JsonValueKind.Object)
                {
                    if (TryInt(terrain, "seed", out int s)) seed = s;
                    if (TryFloat(terrain, "mapWidth", out float mw) && mw > 0f)
                        cfg.MapWidth = mw;
                    if (TryFloat(terrain, "hillFrequency", out float hf) && hf > 0f)
                        cfg.TerrainHillFrequency = hf;
                }

                // Player spawn (x only — groundOffset is applied by SpawnProbeY logic)
                if (root.TryGetProperty("playerSpawn", out var spawn) &&
                    spawn.ValueKind == JsonValueKind.Object)
                {
                    if (TryFloat(spawn, "x", out float px))
                        cfg.Player1SpawnX = px;
                }

                // Difficulty — scale player defaults so campaign difficulty
                // bites without fighting the AI-difficulty tuning.
                if (root.TryGetProperty("difficulty", out var diff) &&
                    diff.ValueKind == JsonValueKind.Object)
                {
                    if (TryFloat(diff, "hpMultiplier", out float hp) && hp > 0f)
                        cfg.DefaultMaxHealth *= hp;
                    if (TryFloat(diff, "damageMultiplier", out float dm) && dm > 0f)
                        cfg.DefaultDamageMultiplier *= dm;
                    if (TryFloat(diff, "speedMultiplier", out float sm) && sm > 0f)
                        cfg.DefaultMoveSpeed *= sm;
                    if (TryFloat(diff, "windStrengthOverride", out float w) && w >= 0f)
                        cfg.MaxWindStrength = w;
                    if (TryInt(diff, "mineCountOverride", out int mc) && mc >= 0)
                        cfg.MineCount = mc;
                }

                // Tutorial steps
                TutorialStepDef[] tutorialSteps = null;
                if (root.TryGetProperty("tutorialSteps", out var steps) &&
                    steps.ValueKind == JsonValueKind.Array)
                {
                    var list = new System.Collections.Generic.List<TutorialStepDef>();
                    foreach (var s in steps.EnumerateArray())
                    {
                        if (s.ValueKind != JsonValueKind.Object) continue;
                        var step = new TutorialStepDef();
                        if (TryInt(s, "stepId", out int sid)) step.StepId = sid;
                        step.Title = TryString(s, "title") ?? "";
                        step.Description = TryString(s, "description") ?? "";
                        string action = TryString(s, "actionType") ?? "move_right";
                        step.ActionType = TutorialSystem.ParseActionType(action);
                        if (TryFloat(s, "threshold", out float th)) step.Threshold = th;
                        else step.Threshold = 1f;
                        if (TryInt(s, "targetWeaponSlot", out int tw)) step.TargetWeaponSlot = tw;
                        else step.TargetWeaponSlot = -1;
                        if (TryInt(s, "targetSkillSlot", out int ts)) step.TargetSkillSlot = ts;
                        else step.TargetSkillSlot = -1;
                        list.Add(step);
                    }
                    if (list.Count > 0) tutorialSteps = list.ToArray();
                }

                return new ApplyResult(true, seed, levelId, tutorialSteps);
            }
        }

        private static string TryString(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static bool TryFloat(JsonElement el, string name, out float val)
        {
            val = 0f;
            if (!el.TryGetProperty(name, out var v)) return false;
            if (v.ValueKind != JsonValueKind.Number) return false;
            if (!v.TryGetDouble(out double d)) return false;
            val = (float)d;
            return true;
        }

        private static bool TryInt(JsonElement el, string name, out int val)
        {
            val = 0;
            if (!el.TryGetProperty(name, out var v)) return false;
            if (v.ValueKind != JsonValueKind.Number) return false;
            return v.TryGetInt32(out val);
        }
    }
}
