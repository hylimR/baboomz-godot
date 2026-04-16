using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using Baboomz;

namespace Baboomz.Tests.Data
{
    [TestFixture]
    public class LevelValidatorTests
    {
        // Anchor path via [CallerFilePath]: this file's location is stable across machines.
        static string TestsDir([CallerFilePath] string p = "") => Path.GetDirectoryName(p)!;
        static string RepoRoot => Path.GetFullPath(Path.Combine(TestsDir(), "..", ".."));
        static string LevelsDir => Path.Combine(RepoRoot, "Resources", "Levels");

        [Test]
        public void AllShippedLevelJsons_ValidateWithoutErrors()
        {
            Assert.IsTrue(Directory.Exists(LevelsDir),
                $"Resources/Levels not found at {LevelsDir}");

            var reports = LevelValidator.ValidateDirectory(LevelsDir);
            Assert.Greater(reports.Count, 0, "Expected at least one level JSON to validate");

            var sb = new StringBuilder();
            int totalErrors = 0;
            foreach (var r in reports)
            {
                if (r.ErrorCount > 0)
                {
                    sb.AppendLine($"{Path.GetFileName(r.FilePath)}: {r.ErrorCount} error(s), {r.WarningCount} warning(s)");
                    foreach (var issue in r.Issues)
                    {
                        if (issue.Level == LevelValidator.Severity.Error)
                            sb.AppendLine($"    {issue}");
                    }
                    totalErrors += r.ErrorCount;
                }
            }

            Assert.AreEqual(0, totalErrors,
                $"Level JSONs have schema errors:\n{sb}");
        }

        // ── Pure-input unit tests ─────────────────────────────────────────────

        const string MinimalValid = @"{
            ""id"": ""test"",
            ""name"": ""Test Level"",
            ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.7, ""indestructibleFloorHeight"": 0 },
            ""playerSpawn"": { ""x"": -40, ""groundOffset"": 2 },
            ""objectives"": { ""type"": ""eliminate_all"" }
        }";

        [Test]
        public void Validate_MinimalValidLevel_NoErrors()
        {
            var report = LevelValidator.Validate(MinimalValid);
            Assert.AreEqual(0, report.ErrorCount,
                $"Expected 0 errors, got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_MalformedJson_ReportsError()
        {
            var report = LevelValidator.Validate("{ not valid");
            Assert.IsTrue(report.HasErrors);
            StringAssert.Contains("malformed JSON", report.Issues[0].Message);
        }

        [Test]
        public void Validate_EmptyJson_ReportsError()
        {
            var report = LevelValidator.Validate("");
            Assert.IsTrue(report.HasErrors);
            StringAssert.Contains("empty JSON", report.Issues[0].Message);
        }

        [Test]
        public void Validate_MissingId_ReportsError()
        {
            string json = @"{
                ""name"": ""Test"",
                ""terrain"": { ""mapWidth"": 100 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""eliminate_all"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "id"),
                $"Expected error on 'id', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_UnknownObjectiveType_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""win_by_magic"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "objectives.type"),
                $"Expected error on 'objectives.type', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_SurviveWaves_WavesLengthMismatch_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": {
                    ""type"": ""survive_waves"",
                    ""waveCount"": 3,
                    ""waves"": [ { ""delay"": 0, ""enemies"": [] } ]
                }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "objectives.waves"),
                $"Expected error on 'objectives.waves', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_SurviveTime_MissingTimeLimit_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""survive_time"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "objectives.timeLimit"),
                $"Expected error on 'objectives.timeLimit', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_DefeatBoss_MissingBossType_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""defeat_boss"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "objectives.bossType"),
                $"Expected error on 'objectives.bossType', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_DestroyTarget_MissingTargetCount_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""destroy_target"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "objectives.targetCount"),
                $"Expected error on 'objectives.targetCount', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_Terrain_MinHeightGreaterThanMaxHeight_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.8, ""maxHeight"": 0.3 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""eliminate_all"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "terrain.minHeight"),
                $"Expected error on 'terrain.minHeight', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_Terrain_MapWidthZero_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 0, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""eliminate_all"" }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "terrain.mapWidth"),
                $"Expected error on 'terrain.mapWidth', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_Boss_InvalidPhaseThreshold_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""defeat_boss"", ""bossType"": ""baron_cogsworth"" },
                ""boss"": { ""bossType"": ""baron_cogsworth"", ""bossHP"": 500, ""phaseThresholds"": [0.66, 1.5] }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "boss.phaseThresholds[1]"),
                $"Expected error on 'boss.phaseThresholds[1]', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_TutorialStep_UnknownAction_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""eliminate_all"" },
                ""tutorialSteps"": [ { ""stepId"": 1, ""actionType"": ""teleport_to_mars"" } ]
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "tutorialSteps[0].actionType"),
                $"Expected error on 'tutorialSteps[0].actionType', got:\n{FormatIssues(report)}");
        }

        [Test]
        public void Validate_Difficulty_NegativeMultiplier_ReportsError()
        {
            string json = @"{
                ""id"": ""t"", ""name"": ""T"",
                ""terrain"": { ""mapWidth"": 100, ""minHeight"": 0.3, ""maxHeight"": 0.6 },
                ""playerSpawn"": { ""x"": 0 },
                ""objectives"": { ""type"": ""eliminate_all"" },
                ""difficulty"": { ""hpMultiplier"": -1 }
            }";
            var report = LevelValidator.Validate(json);
            Assert.IsTrue(HasErrorOnField(report, "difficulty.hpMultiplier"),
                $"Expected error on 'difficulty.hpMultiplier', got:\n{FormatIssues(report)}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static bool HasErrorOnField(LevelValidator.Report r, string field)
        {
            foreach (var i in r.Issues)
                if (i.Level == LevelValidator.Severity.Error && i.Field == field) return true;
            return false;
        }

        static string FormatIssues(LevelValidator.Report r)
        {
            var sb = new StringBuilder();
            foreach (var i in r.Issues) sb.AppendLine("  " + i.ToString());
            return sb.ToString();
        }
    }
}
