using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
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

            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0, TargetIndex = 1,
                Amount = 25f, Position = state.Players[1].Position
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
                SourceIndex = 0, TargetIndex = 1,
                Amount = 100f, Position = state.Players[1].Position
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
                SourceIndex = 0, TargetIndex = 1,
                Amount = 99f, Position = state.Players[1].Position
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
                SourceIndex = 0, TargetIndex = 0,
                Amount = 100f, Position = state.Players[0].Position
            });

            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("mi_1"));
        }

        [Test]
        public void Untouchable_UnlocksOnWinWithNoDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            AchievementTracker.OnMatchStart(state);

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

            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 1, TargetIndex = 0,
                Amount = 10f, Position = state.Players[0].Position
            });
            AchievementTracker.Update(state);
            state.DamageEvents.Clear();
            state.AchievementEvents.Clear();

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
                PrimaryTargetIndex = 1, ChainTargetIndex = 0,
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

            state.BarrelDetonationsThisTick = 1;
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_3"));

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

            AchievementTracker.TryUnlock("cm_1", state, 0);
            Assert.AreEqual(1, state.AchievementEvents.Count);

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

            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 1, TargetIndex = 0,
                Amount = 25f, Position = state.Players[0].Position
            });

            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_1"));
        }

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

            state.Players[0].TerrainPixelsDestroyed = 400;
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_5"));

            state.Players[0].TerrainPixelsDestroyed = 900;
            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_5"));
        }
        [Test]
        public void CannonMaster_cm2_UnlocksViaWeaponHitsNotActiveSlot()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Playing;
            AchievementTracker.OnMatchStart(state);

            // Simulate 3 cannon hits tracked in WeaponHits (as CombatResolver does)
            state.WeaponHits[0]["cannon"] = 2;
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0, TargetIndex = 1,
                Amount = 25f, Position = state.Players[1].Position
            });

            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("cm_2"),
                "Should not unlock at 2 cannon hits");

            state.WeaponHits[0]["cannon"] = 3;
            state.DamageEvents.Clear();
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0, TargetIndex = 1,
                Amount = 25f, Position = state.Players[1].Position
            });

            // Player's active weapon is NOT cannon — should still unlock via WeaponHits
            state.Players[0].ActiveWeaponSlot = 1;
            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("cm_2"),
                "cm_2 should unlock based on WeaponHits dict, not ActiveWeaponSlot");
        }

        [Test]
        public void WarMachine_sm7_DeduplicatesMultiHitKills()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Playing;
            AchievementTracker.OnMatchStart(state);

            state.Players[0].WarCryTimer = 5f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            // Shotgun-like: 4 damage events to same dead target in one tick
            for (int i = 0; i < 4; i++)
            {
                state.DamageEvents.Add(new DamageEvent
                {
                    SourceIndex = 0, TargetIndex = 1,
                    Amount = 10f, Position = state.Players[1].Position
                });
            }

            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("sm_7"),
                "4 damage events to 1 target should count as 1 kill, not 4");
        }
    }
}
