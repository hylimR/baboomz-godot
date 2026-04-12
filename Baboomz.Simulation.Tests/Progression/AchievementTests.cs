using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class AchievementDefsTests
    {
        [Test]
        public void AllDefs_Has30Achievements()
        {
            Assert.AreEqual(30, AchievementDefs.All.Length);
        }

        [Test]
        public void AllDefs_UniqueIds()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var def in AchievementDefs.All)
            {
                Assert.IsTrue(ids.Add(def.Id), $"Duplicate ID: {def.Id}");
            }
        }

        [Test]
        public void GetById_ReturnsCorrectDef()
        {
            var def = AchievementDefs.GetById("cm_1");
            Assert.IsNotNull(def);
            Assert.AreEqual("First Blood", def.Value.Name);
        }

        [Test]
        public void GetById_InvalidId_ReturnsNull()
        {
            var def = AchievementDefs.GetById("nonexistent");
            Assert.IsNull(def);
        }

        [Test]
        public void HiddenAchievements_Are5MiscOnes()
        {
            int hidden = 0;
            foreach (var def in AchievementDefs.All)
                if (def.IsHidden) hidden++;
            Assert.AreEqual(5, hidden);
        }

        [Test]
        public void CategoryCounts_MatchDesign()
        {
            int combat = 0, skill = 0, campaign = 0, misc = 0;
            foreach (var def in AchievementDefs.All)
            {
                switch (def.Category)
                {
                    case AchievementCategory.Combat: combat++; break;
                    case AchievementCategory.Skill: skill++; break;
                    case AchievementCategory.Campaign: campaign++; break;
                    case AchievementCategory.Misc: misc++; break;
                }
            }
            Assert.AreEqual(10, combat);
            Assert.AreEqual(8, skill);
            Assert.AreEqual(7, campaign);
            Assert.AreEqual(5, misc);
        }
    }

    [TestFixture]
    public class AchievementTrackerTests
    {
        static GameConfig SmallConfig()
        {
            return new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f,
                Player1SpawnX = -10f,
                Player2SpawnX = 10f,
                SpawnProbeY = 20f,
                DeathBoundaryY = -25f,
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                BarrelCount = 0,
                MineCount = 0
            };
        }

        [SetUp]
        public void SetUp()
        {
            AchievementTracker.LoadUnlocked(System.Array.Empty<string>());
        }

        [Test]
        public void Reset_ClearsPerMatchState()
        {
            AchievementTracker.Reset();
            Assert.AreEqual(0, AchievementTracker.Unlocked.Count);
        }

        [Test]
        public void LoadUnlocked_RestoresIds()
        {
            AchievementTracker.LoadUnlocked(new[] { "cm_1", "sm_2" });
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_1"));
            Assert.IsTrue(AchievementTracker.IsUnlocked("sm_2"));
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_3"));
        }

        [Test]
        public void TryUnlock_NewAchievement_ReturnsTrue()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            bool result = AchievementTracker.TryUnlock("cm_1", state, 0);
            Assert.IsTrue(result);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_1"));
            Assert.AreEqual(1, state.AchievementEvents.Count);
            Assert.AreEqual("cm_1", state.AchievementEvents[0].AchievementId);
        }

        [Test]
        public void TryUnlock_AlreadyUnlocked_ReturnsFalse()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.TryUnlock("cm_1", state, 0);
            state.AchievementEvents.Clear();

            bool result = AchievementTracker.TryUnlock("cm_1", state, 0);
            Assert.IsFalse(result);
            Assert.AreEqual(0, state.AchievementEvents.Count);
        }

        [Test]
        public void GetSaveString_ReturnsCommaSeparatedIds()
        {
            AchievementTracker.LoadUnlocked(new[] { "cm_1", "sm_2" });
            string save = AchievementTracker.GetSaveString();
            Assert.IsTrue(save.Contains("cm_1"));
            Assert.IsTrue(save.Contains("sm_2"));
        }

        [Test]
        public void FirstBlood_UnlocksOnDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            // Simulate a damage event from player 0 to player 1
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0,
                TargetIndex = 1,
                Amount = 25f,
                Position = state.Players[1].Position
            });

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_1"));
        }

        [Test]
        public void Overkill_UnlocksOn100PlusDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0,
                TargetIndex = 1,
                Amount = 100f,
                Position = state.Players[1].Position
            });

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_7"));
        }

        [Test]
        public void Overkill_DoesNotUnlockBelow100()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0,
                TargetIndex = 1,
                Amount = 99f,
                Position = state.Players[1].Position
            });

            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_7"));
        }

        [Test]
        public void SelfDestruct_UnlocksWhenPlayerKillsSelf()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0,
                TargetIndex = 0,
                Amount = 100f,
                Position = state.Players[0].Position
            });

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("mi_1"));
        }

        [Test]
        public void Untouchable_UnlocksOnWinWithNoDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            // Simulate player winning without damage
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;
            state.Phase = MatchPhase.Ended;
            state.WinnerIndex = 0;

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_6"));
        }

        [Test]
        public void Untouchable_DoesNotUnlockIfPlayerTookDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);
            state.Phase = MatchPhase.Playing;

            // Player takes damage first
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 1,
                TargetIndex = 0,
                Amount = 10f,
                Position = state.Players[0].Position
            });
            AchievementTracker.Update(state);
            state.DamageEvents.Clear();
            state.AchievementEvents.Clear();

            // Then player wins
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;
            state.Phase = MatchPhase.Ended;
            state.WinnerIndex = 0;

            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_6"));
        }

        [Test]
        public void AgainstAllOdds_UnlocksWinAt1HP()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            state.Players[0].Health = 1f;
            state.Players[1].IsDead = true;
            state.Phase = MatchPhase.Ended;
            state.WinnerIndex = 0;

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("mi_3"));
        }

        [Test]
        public void PacifistRound_UnlocksWinWithNoShots()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            // Player didn't fire any shots
            state.Players[1].IsDead = true;
            state.Phase = MatchPhase.Ended;
            state.WinnerIndex = 0;

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("mi_2"));
        }

        [Test]
        public void ZapMaster_UnlocksOnChainHitscan()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            state.HitscanEvents.Add(new HitscanEvent
            {
                PrimaryTargetIndex = 1,
                ChainTargetIndex = 0,
                Origin = Vec2.Zero,
                HitPoint = new Vec2(5f, 5f),
                ChainHitPoint = new Vec2(8f, 5f)
            });

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_4"));
        }

        [Test]
        public void ChainReaction_UnlocksWhen2BarrelsExplodeSameTick()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AchievementTracker.OnMatchStart(state);

            // Simulate 2 barrel detonations in the same tick
            state.BarrelDetonationsThisTick = 2;

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_3"));
        }

        [Test]
        public void ChainReaction_DoesNotUnlockWhenBarrelsDieInSeparateTicks()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AchievementTracker.OnMatchStart(state);

            // Tick 1: one barrel detonates
            state.BarrelDetonationsThisTick = 1;
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_3"));

            // Tick 2: another barrel detonates (separate tick, not a chain)
            state.BarrelDetonationsThisTick = 1;
            state.AchievementEvents.Clear();
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_3"));
        }

        [Test]
        public void CampaignAchievement_TryUnlockExternally()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            bool result = AchievementTracker.TryUnlock("ca_1", state, 0);
            Assert.IsTrue(result);
            Assert.IsTrue(AchievementTracker.IsUnlocked("ca_1"));
        }

        [Test]
        public void AchievementEvents_ClearedEachTick()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            // Force an achievement
            AchievementTracker.TryUnlock("cm_1", state, 0);
            Assert.AreEqual(1, state.AchievementEvents.Count);

            // Tick clears events
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(0, state.AchievementEvents.Count);
        }

        [Test]
        public void OnPlayerKill_SheepThrills()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            AchievementTracker.OnPlayerKill(state, 0, 1, "sheep");
            Assert.IsTrue(AchievementTracker.IsUnlocked("mi_4"));
        }

        [Test]
        public void OnIndestructibleDestroyed_HolyMoly()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            AchievementTracker.OnIndestructibleDestroyed(state, 0);
            Assert.IsTrue(AchievementTracker.IsUnlocked("mi_5"));
        }

        [Test]
        public void AI_DamageDoesNotUnlock_FirstBlood()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

            // AI (player 1) deals damage — should not unlock for player 0
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 1,
                TargetIndex = 0,
                Amount = 25f,
                Position = state.Players[0].Position
            });

            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_1"));
        }

        // --- Issue #65: new achievement trackers ---

        [Test]
        public void ShieldWall_sm4_UnlocksAt100DamageBlocked()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Playing;
            AchievementTracker.OnMatchStart(state);

            state.Players[0].ShieldDamageBlocked = 99f;
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("sm_4"));

            state.Players[0].ShieldDamageBlocked = 100f;
            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("sm_4"));
        }

        [Test]
        public void DemolitionExpert_cm5_UnlocksAt500PixelsPerTick()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Playing;
            AchievementTracker.OnMatchStart(state);

            // First tick: 400 pixels (not enough)
            state.Players[0].TerrainPixelsDestroyed = 400;
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_5"));

            // Second tick: +500 pixels delta = 900 total, delta = 500
            state.Players[0].TerrainPixelsDestroyed = 900;
            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_5"));
        }
    }
}
