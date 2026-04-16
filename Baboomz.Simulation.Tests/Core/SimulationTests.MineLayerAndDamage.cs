using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Mine Layer skill tests ---

        [Test]
        public void MineLay_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Skills.Length >= 11, "Should have at least 11 skills");
            Assert.AreEqual("mine_layer", config.Skills[10].SkillId);
            Assert.AreEqual(SkillType.MineLay, config.Skills[10].Type);
            Assert.AreEqual(25f, config.Skills[10].EnergyCost);
            Assert.AreEqual(10f, config.Skills[10].Cooldown); // #125: 12 -> 10
            Assert.AreEqual(35f, config.Skills[10].Value); // #125: 30 -> 35
        }

        [Test]
        public void MineLay_PlacesMineAtAimTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = -45f; // aim downward

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 100f;

            int minesBefore = state.Mines.Count;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(minesBefore + 1, state.Mines.Count,
                "Mine Layer should place a mine");
            var mine = state.Mines[state.Mines.Count - 1];
            Assert.IsTrue(mine.Active);
            Assert.AreEqual(0, mine.OwnerIndex);
            Assert.AreEqual(30f, mine.Damage); // slot override (from test SkillSlotState.Value)
            Assert.AreEqual(15f, mine.Lifetime);
        }

        [Test]
        public void MineLay_MaxThreeMinesPerPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 1000f;

            // Place 4 mines — fourth should deactivate the first (#125: cap is 3)
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);

            int activeOwned = 0;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == 0)
                    activeOwned++;

            Assert.LessOrEqual(activeOwned, 3,
                "Player should have at most 3 active mines");
        }

        [Test]
        public void MineLay_OverflowRemovesActuallyOldestByPlacedTime()
        {
            // Regression test for #33: overflow eviction previously picked the
            // first-found-owned-index, which is unstable when a deactivated slot
            // gets reused. Ensure we remove the mine with the smallest PlacedTime.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 1000f;

            // Seed a deactivated-slot scenario: place mine A, kill it, then fill slots
            // with mines B, C, D at increasing times. When we place E (4th active) it
            // should evict the earliest of {B, C, D} — which is B — not whichever the
            // loop finds first. Updated for #125 cap change (2 -> 3 active mines).
            state.Time = 1f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine A @ t=1
            // Simulate mine A exploding/deactivating: flip its slot dead.
            int aIdx = -1;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == 0) { aIdx = i; break; }
            var a = state.Mines[aIdx];
            a.Active = false;
            state.Mines[aIdx] = a;

            state.Time = 5f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine B @ t=5
            state.Time = 10f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine C @ t=10
            state.Time = 12f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine D @ t=12

            // Track active placed times before overflow
            float bTime = float.MaxValue;
            float dTime = float.MinValue;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (state.Mines[i].PlacedTime < bTime) bTime = state.Mines[i].PlacedTime;
                if (state.Mines[i].PlacedTime > dTime) dTime = state.Mines[i].PlacedTime;
            }
            Assert.AreEqual(5f, bTime, 0.01f);
            Assert.AreEqual(12f, dTime, 0.01f);

            // Place mine E — B should be evicted (oldest), C and D should survive alongside E
            state.Time = 15f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine E @ t=15

            bool bStillActive = false;
            bool cStillActive = false;
            bool dStillActive = false;
            bool eActive = false;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (MathF.Abs(state.Mines[i].PlacedTime - 5f) < 0.01f) bStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 10f) < 0.01f) cStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 12f) < 0.01f) dStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 15f) < 0.01f) eActive = true;
            }
            Assert.IsFalse(bStillActive, "Oldest mine (B, t=5) should have been evicted");
            Assert.IsTrue(cStillActive, "Mid mine (C, t=10) should still be active");
            Assert.IsTrue(dStillActive, "Newer mine (D, t=12) should still be active");
            Assert.IsTrue(eActive, "Newly placed mine (E, t=15) should be active");
        }

        [Test]
        public void MineLay_DoesNotTriggerOnOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 0 at a known position
            state.Players[0].Position = new Vec2(0f, 5f);

            // Add a mine owned by player 0 at player 0's position
            state.Mines.Add(new MineState
            {
                Position = state.Players[0].Position,
                TriggerRadius = 2f,
                ExplosionRadius = 3f,
                Damage = 30f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = 0
            });

            float healthBefore = state.Players[0].Health;

            // Tick — mine should NOT trigger on its owner
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Player-laid mine should not trigger on its owner");
            Assert.IsTrue(state.Mines[state.Mines.Count - 1].Active,
                "Mine should still be active (not triggered by owner)");
        }

        [Test]
        public void MineLay_ExplosionCreditsOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Player 1 is at a known position
            state.Players[1].Position = new Vec2(5f, 5f);

            // Add a mine owned by player 0 at player 1's position
            state.Mines.Add(new MineState
            {
                Position = state.Players[1].Position,
                TriggerRadius = 2f,
                ExplosionRadius = 3f,
                Damage = 30f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = 0
            });

            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            // Check that explosion credited player 0
            bool foundOwnerCredit = false;
            for (int d = 0; d < state.DamageEvents.Count; d++)
            {
                if (state.DamageEvents[d].SourceIndex == 0 && state.DamageEvents[d].TargetIndex == 1)
                {
                    foundOwnerCredit = true;
                    break;
                }
            }
            Assert.IsTrue(foundOwnerCredit,
                "Mine explosion should credit the mine owner (SourceIndex = OwnerIndex)");
        }

    }
}
