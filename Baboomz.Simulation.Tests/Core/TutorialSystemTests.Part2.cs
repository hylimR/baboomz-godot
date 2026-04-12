using System.Collections.Generic;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class TutorialSystemTests
    {
        [Test]
        public void UseSkill_CompletesOnSkillEvent()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.UseSkill, 1f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.SkillEvents.Add(new SkillEvent { PlayerIndex = 0, Type = SkillType.Teleport });

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void UseSkill_DoesNotCompleteOnEnemySkillEvent()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.UseSkill, 1f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Enemy uses skill, not player
            state.SkillEvents.Add(new SkillEvent { PlayerIndex = 1, Type = SkillType.Teleport });

            TutorialSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void DestroyTerrain_CompletesWhenThresholdReached()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.DestroyTerrain, 50f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            state.Players[0].TerrainPixelsDestroyed = 10;
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.Players[0].TerrainPixelsDestroyed = 60;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void KillEnemy_CompletesWhenEnemyDies()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.KillEnemy, 1f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.Players[1].IsDead = true;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void KillEnemy_DoesNotCompleteWhileAlive()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.KillEnemy, 1f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            TutorialSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void MultiStep_AdvancesThroughAll()
        {
            var state = MakeMinimalState();
            var steps = new[]
            {
                MakeStep(1, TutorialActionType.MoveRight, 3f),
                MakeStep(2, TutorialActionType.Jump, 1f)
            };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Complete step 1
            state.Players[0].Position = new Vec2(3f, 0f);
            TutorialSystem.Update(state, 0.016f);
            Assert.AreEqual(1, state.Tutorial.CurrentStepIndex);
            Assert.IsFalse(state.Tutorial.IsComplete);

            // Re-init tracking for step 2
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Complete step 2
            state.Players[0].Position = new Vec2(3f, 1f);
            TutorialSystem.Update(state, 0.016f);
            Assert.IsTrue(state.Tutorial.IsComplete);
        }

        [Test]
        public void Skip_MarksAsSkipped()
        {
            var steps = new[] { MakeStep(1, TutorialActionType.MoveRight, 5f) };
            var tut = TutorialSystem.CreateFromSteps(steps);

            TutorialSystem.Skip(tut);

            Assert.IsTrue(tut.IsSkipped);
        }

        [Test]
        public void Update_Skipped_DoesNotProgress()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.MoveRight, 5f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);
            TutorialSystem.Skip(state.Tutorial);

            state.Players[0].Position = new Vec2(10f, 0f);
            TutorialSystem.Update(state, 0.016f);

            Assert.AreEqual(0, state.Tutorial.CurrentStepIndex);
        }

        [Test]
        public void Update_NullTutorial_NoError()
        {
            var state = MakeMinimalState();
            state.Tutorial = null;

            // Should not throw
            TutorialSystem.Update(state, 0.016f);
        }

        [Test]
        public void GetCurrentStep_ReturnsCorrectStep()
        {
            var steps = new[]
            {
                MakeStep(1, TutorialActionType.MoveRight, 5f),
                MakeStep(2, TutorialActionType.Jump, 1f)
            };
            var tut = TutorialSystem.CreateFromSteps(steps);

            var current = TutorialSystem.GetCurrentStep(tut);

            Assert.AreEqual(1, current.StepId);
            Assert.AreEqual(TutorialActionType.MoveRight, current.ActionType);
        }

        [Test]
        public void GetCurrentStep_CompletedTutorial_ReturnsNull()
        {
            var tut = TutorialSystem.CreateFromSteps(new TutorialStepDef[0]);
            tut.IsComplete = true;

            Assert.IsNull(TutorialSystem.GetCurrentStep(tut));
        }

        [Test]
        public void ParseActionType_AllValues()
        {
            Assert.AreEqual(TutorialActionType.MoveRight, TutorialSystem.ParseActionType("move_right"));
            Assert.AreEqual(TutorialActionType.Jump, TutorialSystem.ParseActionType("jump"));
            Assert.AreEqual(TutorialActionType.AimUp, TutorialSystem.ParseActionType("aim_up"));
            Assert.AreEqual(TutorialActionType.ChargeAndFire, TutorialSystem.ParseActionType("charge_and_fire"));
            Assert.AreEqual(TutorialActionType.SwitchWeapon, TutorialSystem.ParseActionType("switch_weapon"));
            Assert.AreEqual(TutorialActionType.UseSkill, TutorialSystem.ParseActionType("use_skill"));
            Assert.AreEqual(TutorialActionType.DestroyTerrain, TutorialSystem.ParseActionType("destroy_terrain"));
            Assert.AreEqual(TutorialActionType.KillEnemy, TutorialSystem.ParseActionType("kill_enemy"));
        }

        [Test]
        public void ParseActionType_Unknown_DefaultsToMoveRight()
        {
            Assert.AreEqual(TutorialActionType.MoveRight, TutorialSystem.ParseActionType("unknown_action"));
        }

        [Test]
        public void StepProgress_TracksCorrectly()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.MoveRight, 10f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.Players[0].Position = new Vec2(4f, 0f);
            TutorialSystem.Update(state, 0.016f);

            Assert.AreEqual(4f, state.Tutorial.StepProgress, 0.001f);
            Assert.IsFalse(state.Tutorial.IsComplete);
        }

        [Test]
        public void SwitchWeapon_AnySlot_CompletesOnNonZero()
        {
            var state = MakeMinimalState();
            // targetWeapon = -1 means any non-zero slot
            var steps = new[] { MakeStep(1, TutorialActionType.SwitchWeapon, 1f, targetWeapon: -1) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.Players[0].ActiveWeaponSlot = 2;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }
    }
}
