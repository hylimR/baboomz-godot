using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class ComboTrackerTests
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
                DeathBoundaryY = -25f
            };
        }

        static GameState CreateState()
        {
            return GameSimulation.CreateMatch(SmallConfig(), 42);
        }

        // --- Hit streak tests ---

        [Test]
        public void TrackHit_IncrementsConsecutiveHits()
        {
            var state = CreateState();
            state.Time = 1f;

            CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits);
            Assert.AreEqual(1f, state.Players[0].LastHitTime, 0.001f);
        }

        [Test]
        public void TrackHit_TwoHits_EmitsDoubleHit()
        {
            var state = CreateState();
            state.Time = 1f;

            CombatResolver.TrackHit(state, 0);
            CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(2, state.Players[0].ConsecutiveHits);
            Assert.AreEqual(1, state.ComboEvents.Count);
            Assert.AreEqual(ComboType.DoubleHit, state.ComboEvents[0].Type);
            Assert.AreEqual(0, state.ComboEvents[0].PlayerIndex);
        }

        [Test]
        public void TrackHit_ThreeHits_EmitsTripleHit()
        {
            var state = CreateState();
            state.Time = 2f;

            CombatResolver.TrackHit(state, 0);
            CombatResolver.TrackHit(state, 0);
            CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(3, state.Players[0].ConsecutiveHits);
            // Should have DoubleHit at hit 2 and TripleHit at hit 3
            Assert.AreEqual(2, state.ComboEvents.Count);
            Assert.AreEqual(ComboType.DoubleHit, state.ComboEvents[0].Type);
            Assert.AreEqual(ComboType.TripleHit, state.ComboEvents[1].Type);
        }

        [Test]
        public void TrackHit_FourHits_EmitsQuadHit()
        {
            var state = CreateState();
            state.Time = 3f;

            for (int i = 0; i < 4; i++)
                CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(4, state.Players[0].ConsecutiveHits);
            Assert.AreEqual(3, state.ComboEvents.Count);
            Assert.AreEqual(ComboType.QuadHit, state.ComboEvents[2].Type);
        }

        [Test]
        public void TrackHit_FiveHits_EmitsUnstoppable()
        {
            var state = CreateState();
            state.Time = 4f;

            for (int i = 0; i < 5; i++)
                CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(5, state.Players[0].ConsecutiveHits);
            Assert.AreEqual(4, state.ComboEvents.Count);
            Assert.AreEqual(ComboType.Unstoppable, state.ComboEvents[3].Type);
        }

        [Test]
        public void TrackHit_SingleHit_NoComboEvent()
        {
            var state = CreateState();
            state.Time = 1f;

            CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(0, state.ComboEvents.Count);
        }

        // --- Kill streak tests ---

        [Test]
        public void TrackKill_SingleKill_NoComboEvent()
        {
            var state = CreateState();
            state.Time = 5f;

            CombatResolver.TrackKill(state, 0);

            Assert.AreEqual(1, state.Players[0].KillsInWindow);
            Assert.AreEqual(0, state.ComboEvents.Count);
        }

        [Test]
        public void TrackKill_TwoKillsWithin3s_EmitsDoubleKill()
        {
            var state = CreateState();
            state.Time = 5f;
            CombatResolver.TrackKill(state, 0);

            state.Time = 7f; // 2s later, within 3s window
            CombatResolver.TrackKill(state, 0);

            Assert.AreEqual(2, state.Players[0].KillsInWindow);
            Assert.AreEqual(1, state.ComboEvents.Count);
            Assert.AreEqual(ComboType.DoubleKill, state.ComboEvents[0].Type);
        }

        [Test]
        public void TrackKill_ThreeKillsWithin3s_EmitsMultiKill()
        {
            var state = CreateState();
            state.Time = 5f;
            CombatResolver.TrackKill(state, 0);
            state.Time = 6f;
            CombatResolver.TrackKill(state, 0);
            state.Time = 7f;
            CombatResolver.TrackKill(state, 0);

            Assert.AreEqual(3, state.Players[0].KillsInWindow);
            Assert.AreEqual(2, state.ComboEvents.Count);
            Assert.AreEqual(ComboType.MultiKill, state.ComboEvents[1].Type);
        }

        [Test]
        public void TrackKill_GapExceeds3s_ResetsWindow()
        {
            var state = CreateState();
            state.Time = 5f;
            CombatResolver.TrackKill(state, 0);

            state.Time = 9f; // 4s later, exceeds 3s window
            CombatResolver.TrackKill(state, 0);

            // Window should have been reset, so this is kill 1 again
            Assert.AreEqual(1, state.Players[0].KillsInWindow);
            Assert.AreEqual(0, state.ComboEvents.Count);
        }

        // --- Decay tests ---

        [Test]
        public void DecayCombo_ResetsHitsAfter2s()
        {
            var state = CreateState();
            state.Time = 1f;
            CombatResolver.TrackHit(state, 0);
            CombatResolver.TrackHit(state, 0);
            Assert.AreEqual(2, state.Players[0].ConsecutiveHits);

            state.Time = 3.1f; // > 2s after last hit at t=1
            CombatResolver.DecayCombo(state);

            Assert.AreEqual(0, state.Players[0].ConsecutiveHits);
        }

        [Test]
        public void DecayCombo_DoesNotResetHitsWithin2s()
        {
            var state = CreateState();
            state.Time = 1f;
            CombatResolver.TrackHit(state, 0);
            CombatResolver.TrackHit(state, 0);

            state.Time = 2.5f; // 1.5s after last hit, within 2s window
            CombatResolver.DecayCombo(state);

            Assert.AreEqual(2, state.Players[0].ConsecutiveHits);
        }

        [Test]
        public void DecayCombo_ResetsKillsAfter3s()
        {
            var state = CreateState();
            state.Time = 1f;
            CombatResolver.TrackKill(state, 0);

            state.Time = 4.1f; // > 3s after last kill
            CombatResolver.DecayCombo(state);

            Assert.AreEqual(0, state.Players[0].KillsInWindow);
        }

        [Test]
        public void DecayCombo_DoesNotResetKillsWithin3s()
        {
            var state = CreateState();
            state.Time = 1f;
            CombatResolver.TrackKill(state, 0);

            state.Time = 3.5f; // 2.5s after last kill, within 3s window
            CombatResolver.DecayCombo(state);

            Assert.AreEqual(1, state.Players[0].KillsInWindow);
        }

        // --- Integration: ApplyExplosion triggers combo ---

        [Test]
        public void ApplyExplosion_DamageToEnemy_IncrementsCombo()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            // Place player 1 right at the explosion
            state.Players[1].Position = new Vec2(0f, 0f);
            state.Players[1].Health = 100f;

            CombatResolver.ApplyExplosion(state, new Vec2(0f, 0f), 5f, 30f, 5f, 0, false);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits);
        }

        [Test]
        public void ApplyExplosion_TwoHitsOnEnemy_EmitsDoubleHitCombo()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[1].Position = new Vec2(0f, 0f);
            state.Players[1].Health = 200f;

            CombatResolver.ApplyExplosion(state, new Vec2(0f, 0f), 5f, 30f, 5f, 0, false);
            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 5f, 30f, 5f, 0, false);

            Assert.AreEqual(2, state.Players[0].ConsecutiveHits);
            Assert.IsTrue(state.ComboEvents.Exists(e => e.Type == ComboType.DoubleHit));
        }

        [Test]
        public void TrackHit_UnstoppableContinuesFiringAbove5()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            // Build up to 5 consecutive hits (Unstoppable threshold)
            for (int i = 0; i < 5; i++)
                CombatResolver.TrackHit(state, 0);

            int unstoppableCount = state.ComboEvents.FindAll(
                e => e.Type == ComboType.Unstoppable).Count;
            Assert.AreEqual(1, unstoppableCount, "Unstoppable should fire at 5 hits");

            // Hit 6 should also emit Unstoppable
            CombatResolver.TrackHit(state, 0);
            int unstoppableAfter = state.ComboEvents.FindAll(
                e => e.Type == ComboType.Unstoppable).Count;
            Assert.AreEqual(2, unstoppableAfter,
                "6th hit should emit another Unstoppable event");
        }

        [Test]
        public void TrackHit_SevenHits_EmitsUnstoppableForEachAbove4()
        {
            var state = CreateState();
            state.Time = 1f;

            for (int i = 0; i < 7; i++)
                CombatResolver.TrackHit(state, 0);

            Assert.AreEqual(7, state.Players[0].ConsecutiveHits);
            // Hits 2,3,4 emit DoubleHit,TripleHit,QuadHit (3 events)
            // Hits 5,6,7 each emit Unstoppable (3 events)
            Assert.AreEqual(6, state.ComboEvents.Count);
            int unstoppableCount = state.ComboEvents.FindAll(
                e => e.Type == ComboType.Unstoppable).Count;
            Assert.AreEqual(3, unstoppableCount,
                "Hits 5, 6, and 7 should each emit Unstoppable");
        }

        [Test]
        public void TrackKill_FourthKillInWindow_EmitsMultiKill()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            // Build up to 3 kills (MultiKill threshold)
            for (int i = 0; i < 3; i++)
                CombatResolver.TrackKill(state, 0);

            int multiKillCount = state.ComboEvents.FindAll(
                e => e.Type == ComboType.MultiKill).Count;
            Assert.AreEqual(1, multiKillCount, "MultiKill should fire exactly once at 3 kills");

            // Kill 4 should also emit MultiKill (regression: was silently dropped)
            CombatResolver.TrackKill(state, 0);
            int multiKillAfter = state.ComboEvents.FindAll(
                e => e.Type == ComboType.MultiKill).Count;
            Assert.AreEqual(2, multiKillAfter,
                "4th kill in window should emit another MultiKill event");
        }

        [Test]
        public void ApplyExplosion_SelfDamage_DoesNotIncrementCombo()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            // Explosion at player 0's own position
            state.Players[0].Position = new Vec2(0f, 0f);
            state.Players[0].Health = 200f;

            // Move player 1 far away so only self-damage occurs
            state.Players[1].Position = new Vec2(100f, 100f);

            CombatResolver.ApplyExplosion(state, new Vec2(0f, 0f), 5f, 30f, 5f, 0, false);

            Assert.AreEqual(0, state.Players[0].ConsecutiveHits);
        }

        [Test]
        public void ApplyExplosion_ZeroDamageBoundary_DoesNotInflateStats()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            // Place player 1 exactly at explosion radius boundary → dmgRatio = 0
            state.Players[1].Position = new Vec2(5f, 0f);
            state.Players[0].DirectHits = 0;
            state.Players[0].ConsecutiveHits = 0;
            state.FirstBloodPlayerIndex = -1;

            CombatResolver.ApplyExplosion(state, new Vec2(0f, 0f), 5f, 30f, 5f, 0, false);

            Assert.AreEqual(0, state.Players[0].DirectHits,
                "Zero-damage boundary hit should not increment DirectHits");
            Assert.AreEqual(0, state.Players[0].ConsecutiveHits,
                "Zero-damage boundary hit should not trigger combo");
            Assert.AreEqual(-1, state.FirstBloodPlayerIndex,
                "Zero-damage boundary hit should not claim FirstBlood");
        }

        [Test]
        public void ComboEvents_ClearedEachTick()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;
            state.ComboEvents.Add(new ComboEvent
            {
                PlayerIndex = 0,
                Type = ComboType.DoubleHit,
                Time = 1f
            });

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.ComboEvents.Count);
        }
    }
}
