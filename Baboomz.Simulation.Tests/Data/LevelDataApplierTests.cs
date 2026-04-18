using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Data
{
    // Regression #162: before this fix, GameModeContext.SelectedLevelId was
    // written by LevelSelectPanel but read by no one, so campaign levels all
    // played as identical random matches. These tests lock in the contract
    // that a level JSON actually reshapes the GameConfig.
    [TestFixture]
    public class LevelDataApplierTests
    {
        static string TestsDir([CallerFilePath] string p = "") => Path.GetDirectoryName(p)!;
        static string RepoRoot => Path.GetFullPath(Path.Combine(TestsDir(), "..", ".."));
        static string LevelsDir => Path.Combine(RepoRoot, "Resources", "Levels");

        const string SampleJson = @"{
            ""id"": ""test_level"",
            ""name"": ""Test"",
            ""terrain"": {
                ""seed"": 4242,
                ""mapWidth"": 250,
                ""minHeight"": 0.3,
                ""maxHeight"": 0.7,
                ""hillFrequency"": 1.5,
                ""indestructibleFloorHeight"": 0.1
            },
            ""playerSpawn"": { ""x"": -90, ""groundOffset"": 2 },
            ""enemies"": [],
            ""structures"": [],
            ""objectives"": { ""type"": ""eliminate_all"" },
            ""difficulty"": {
                ""hpMultiplier"": 1.5,
                ""damageMultiplier"": 1.25,
                ""speedMultiplier"": 1.1,
                ""windStrengthOverride"": 5.0,
                ""mineCountOverride"": 0
            }
        }";

        [Test]
        public void Apply_ValidJson_SetsMatchTypeAndReturnsSeed()
        {
            var cfg = new GameConfig();
            var result = LevelDataApplier.Apply(SampleJson, cfg);

            Assert.IsTrue(result.Applied, "Valid JSON should apply");
            Assert.AreEqual("test_level", result.LevelId);
            Assert.AreEqual(4242, result.TerrainSeed);
            Assert.AreEqual(Baboomz.Simulation.MatchType.Campaign, cfg.MatchType,
                "Any applied level must set Baboomz.Simulation.MatchType.Campaign");
        }

        [Test]
        public void Apply_ValidJson_OverridesTerrainAndSpawnFields()
        {
            var cfg = new GameConfig();
            LevelDataApplier.Apply(SampleJson, cfg);

            Assert.AreEqual(250f, cfg.MapWidth, 0.01f);
            Assert.AreEqual(1.5f, cfg.TerrainHillFrequency, 0.01f);
            Assert.AreEqual(-90f, cfg.Player1SpawnX, 0.01f);
        }

        [Test]
        public void Apply_Difficulty_MultipliesPlayerDefaults()
        {
            var cfg = new GameConfig();
            float hpBefore = cfg.DefaultMaxHealth;
            float dmgBefore = cfg.DefaultDamageMultiplier;
            float spdBefore = cfg.DefaultMoveSpeed;

            LevelDataApplier.Apply(SampleJson, cfg);

            Assert.AreEqual(hpBefore * 1.5f, cfg.DefaultMaxHealth, 0.01f);
            Assert.AreEqual(dmgBefore * 1.25f, cfg.DefaultDamageMultiplier, 0.01f);
            Assert.AreEqual(spdBefore * 1.1f, cfg.DefaultMoveSpeed, 0.01f);
            Assert.AreEqual(5.0f, cfg.MaxWindStrength, 0.01f,
                "windStrengthOverride must override, not multiply");
            Assert.AreEqual(0, cfg.MineCount,
                "mineCountOverride must override (0 disables mines)");
        }

        [Test]
        public void Apply_NullOrEmptyJson_Noop_ReturnsNotApplied()
        {
            var cfg = new GameConfig();
            var original = cfg.MatchType;

            Assert.IsFalse(LevelDataApplier.Apply(null, cfg).Applied);
            Assert.IsFalse(LevelDataApplier.Apply("", cfg).Applied);
            Assert.IsFalse(LevelDataApplier.Apply("   ", cfg).Applied);
            Assert.AreEqual(original, cfg.MatchType, "Config must not mutate on empty input");
        }

        [Test]
        public void Apply_InvalidJson_Noop_ReturnsNotApplied()
        {
            var cfg = new GameConfig();
            var original = cfg.MapWidth;

            Assert.IsFalse(LevelDataApplier.Apply("{ this is not json", cfg).Applied);
            Assert.IsFalse(LevelDataApplier.Apply("[]", cfg).Applied,
                "Root must be an object, not an array");
            Assert.AreEqual(original, cfg.MapWidth, "Config must not mutate on invalid JSON");
        }

        [Test]
        public void Apply_MinimalJson_AppliesDefaultsWithoutCrashing()
        {
            // Only id + objectives — everything else should be left alone on cfg.
            var cfg = new GameConfig();
            float mapBefore = cfg.MapWidth;
            float spawnBefore = cfg.Player1SpawnX;
            float hpBefore = cfg.DefaultMaxHealth;

            var result = LevelDataApplier.Apply(@"{ ""id"": ""bare"", ""objectives"": {} }", cfg);

            Assert.IsTrue(result.Applied);
            Assert.IsNull(result.TerrainSeed, "No terrain block -> no seed");
            Assert.AreEqual(Baboomz.Simulation.MatchType.Campaign, cfg.MatchType);
            Assert.AreEqual(mapBefore, cfg.MapWidth, 0.01f);
            Assert.AreEqual(spawnBefore, cfg.Player1SpawnX, 0.01f);
            Assert.AreEqual(hpBefore, cfg.DefaultMaxHealth, 0.01f);
        }

        [Test]
        public void Apply_NullConfig_ReturnsNotApplied()
        {
            var result = LevelDataApplier.Apply(SampleJson, null);
            Assert.IsFalse(result.Applied);
        }

        [Test]
        public void Apply_AllShippedLevelJsons_ApplyWithoutCrashing()
        {
            Assert.IsTrue(Directory.Exists(LevelsDir),
                $"Resources/Levels not found at {LevelsDir}");

            var files = Directory.GetFiles(LevelsDir, "*.json");
            Assert.Greater(files.Length, 0);

            int applied = 0;
            foreach (var path in files)
            {
                var cfg = new GameConfig();
                var json = File.ReadAllText(path);
                var result = LevelDataApplier.Apply(json, cfg);
                Assert.IsTrue(result.Applied, $"{Path.GetFileName(path)} should apply cleanly");
                Assert.AreEqual(Baboomz.Simulation.MatchType.Campaign, cfg.MatchType,
                    $"{Path.GetFileName(path)} should set Baboomz.Simulation.MatchType.Campaign");
                applied++;
            }

            Assert.Greater(applied, 20, "Expected many shipped levels to apply");
        }

        [Test]
        public void Apply_TutorialSteps_ParsesAllFields()
        {
            const string json = @"{
                ""id"": ""tutorial_01"",
                ""objectives"": { ""type"": ""eliminate_all"" },
                ""tutorialSteps"": [
                    {
                        ""stepId"": 1,
                        ""actionType"": ""move_right"",
                        ""threshold"": 50.0,
                        ""title"": ""Move Right"",
                        ""description"": ""Walk to the right"",
                        ""targetWeaponSlot"": -1,
                        ""targetSkillSlot"": -1
                    },
                    {
                        ""stepId"": 2,
                        ""actionType"": ""charge_and_fire"",
                        ""threshold"": 1.0,
                        ""title"": ""Fire!"",
                        ""description"": ""Hold and release to fire"",
                        ""targetWeaponSlot"": 0,
                        ""targetSkillSlot"": -1
                    },
                    {
                        ""stepId"": 3,
                        ""actionType"": ""use_skill"",
                        ""threshold"": 1.0,
                        ""title"": ""Use Skill"",
                        ""description"": ""Activate your skill"",
                        ""targetWeaponSlot"": -1,
                        ""targetSkillSlot"": 2
                    }
                ]
            }";

            var cfg = new GameConfig();
            var result = LevelDataApplier.Apply(json, cfg);

            Assert.IsTrue(result.Applied);
            Assert.IsNotNull(result.TutorialSteps);
            Assert.AreEqual(3, result.TutorialSteps.Length);

            var s1 = result.TutorialSteps[0];
            Assert.AreEqual(1, s1.StepId);
            Assert.AreEqual(TutorialActionType.MoveRight, s1.ActionType);
            Assert.AreEqual(50f, s1.Threshold, 0.01f);
            Assert.AreEqual("Move Right", s1.Title);
            Assert.AreEqual("Walk to the right", s1.Description);
            Assert.AreEqual(-1, s1.TargetWeaponSlot);
            Assert.AreEqual(-1, s1.TargetSkillSlot);

            var s2 = result.TutorialSteps[1];
            Assert.AreEqual(2, s2.StepId);
            Assert.AreEqual(TutorialActionType.ChargeAndFire, s2.ActionType);
            Assert.AreEqual(1f, s2.Threshold, 0.01f);
            Assert.AreEqual("Fire!", s2.Title);
            Assert.AreEqual("Hold and release to fire", s2.Description);
            Assert.AreEqual(0, s2.TargetWeaponSlot);
            Assert.AreEqual(-1, s2.TargetSkillSlot);

            var s3 = result.TutorialSteps[2];
            Assert.AreEqual(3, s3.StepId);
            Assert.AreEqual(TutorialActionType.UseSkill, s3.ActionType);
            Assert.AreEqual("Use Skill", s3.Title);
            Assert.AreEqual("Activate your skill", s3.Description);
            Assert.AreEqual(-1, s3.TargetWeaponSlot);
            Assert.AreEqual(2, s3.TargetSkillSlot);
        }

        [Test]
        public void Apply_NoTutorialSteps_ReturnsNullArray()
        {
            var cfg = new GameConfig();
            var result = LevelDataApplier.Apply(SampleJson, cfg);

            Assert.IsTrue(result.Applied);
            Assert.IsNull(result.TutorialSteps);
        }
    }
}
