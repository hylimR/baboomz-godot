using System.Collections.Generic;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public partial class TutorialSystemTests
    {
        static TutorialStepDef MakeStep(int id, TutorialActionType action, float threshold = 1f,
            int targetWeapon = -1, int targetSkill = -1)
        {
            return new TutorialStepDef
            {
                StepId = id,
                Title = $"Step {id}",
                Description = $"Do step {id}",
                ActionType = action,
                Threshold = threshold,
                TargetWeaponSlot = targetWeapon,
                TargetSkillSlot = targetSkill
            };
        }

        static GameState MakeMinimalState()
        {
            var state = new GameState
            {
                Phase = MatchPhase.Playing,
                Config = new GameConfig(),
                Players = new[]
                {
                    new PlayerState
                    {
                        Position = new Vec2(0f, 0f),
                        AimAngle = 45f,
                        Health = 100f,
                        MaxHealth = 100f,
                        WeaponSlots = new WeaponSlotState[4],
                        SkillSlots = new SkillSlotState[2]
                    },
                    new PlayerState
                    {
                        Position = new Vec2(30f, 0f),
                        Health = 50f,
                        MaxHealth = 50f,
                        IsMob = true,
                        IsAI = true,
                        WeaponSlots = new WeaponSlotState[4],
                        SkillSlots = new SkillSlotState[2]
                    }
                },
                Input = new InputState()
            };
            return state;
        }

        [Test]
        public void CreateFromSteps_InitializesCorrectly()
        {
            var steps = new[]
            {
                MakeStep(1, TutorialActionType.MoveRight, 5f),
                MakeStep(2, TutorialActionType.Jump, 1f)
            };

            var tut = TutorialSystem.CreateFromSteps(steps);

            Assert.AreEqual(2, tut.Steps.Length);
            Assert.AreEqual(0, tut.CurrentStepIndex);
            Assert.IsFalse(tut.IsComplete);
            Assert.IsFalse(tut.IsSkipped);
        }

        [Test]
        public void CreateFromSteps_EmptySteps_MarksComplete()
        {
            var state = MakeMinimalState();
            state.Tutorial = TutorialSystem.CreateFromSteps(new TutorialStepDef[0]);

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.IsComplete);
        }

        [Test]
        public void CreateFromSteps_NullSteps_MarksComplete()
        {
            var state = MakeMinimalState();
            state.Tutorial = TutorialSystem.CreateFromSteps(null);

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.IsComplete);
        }

        [Test]
        public void MoveRight_CompletesWhenThresholdReached()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.MoveRight, 5f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Move player right by 5 units
            state.Players[0].Position = new Vec2(5f, 0f);

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
            Assert.AreEqual(1, state.Tutorial.CurrentStepIndex);
        }

        [Test]
        public void MoveRight_DoesNotCompleteBeforeThreshold()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.MoveRight, 5f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Move player right by only 3 units
            state.Players[0].Position = new Vec2(3f, 0f);

            TutorialSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Tutorial.StepJustCompleted);
            Assert.AreEqual(0, state.Tutorial.CurrentStepIndex);
        }

        [Test]
        public void Jump_CompletesWhenHeightReached()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.Jump, 2f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Player jumps to Y=2
            state.Players[0].Position = new Vec2(0f, 2f);

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void AimUp_CompletesWhenAngleReached()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.AimUp, 20f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Player aims 20 degrees up from starting angle
            state.Players[0].AimAngle = 65f; // started at 45

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void ChargeAndFire_CompletesOnFireRelease()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.ChargeAndFire, 1f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Player has fired and released
            state.Players[0].ShotsFired = 1;
            state.Input.FireReleased = true;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void ChargeAndFire_DoesNotCompleteWithoutFiring()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.ChargeAndFire, 1f) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            // Fire released but no shot fired
            state.Input.FireReleased = true;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void SwitchWeapon_CompletesOnTargetSlot()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.SwitchWeapon, 1f, targetWeapon: 1) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.Players[0].ActiveWeaponSlot = 1;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsTrue(state.Tutorial.StepJustCompleted);
        }

        [Test]
        public void SwitchWeapon_DoesNotCompleteOnWrongSlot()
        {
            var state = MakeMinimalState();
            var steps = new[] { MakeStep(1, TutorialActionType.SwitchWeapon, 1f, targetWeapon: 2) };
            state.Tutorial = TutorialSystem.CreateFromSteps(steps);
            TutorialSystem.InitStepTracking(state.Tutorial, state);

            state.Players[0].ActiveWeaponSlot = 1;

            TutorialSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Tutorial.StepJustCompleted);
        }

    }
}
