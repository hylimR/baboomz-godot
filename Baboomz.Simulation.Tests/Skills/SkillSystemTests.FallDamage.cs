using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        [Test]
        public void Teleport_ResetsLastGroundedY_WhenAirborne()
        {
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            Assert.AreEqual(SkillType.Teleport, p.SkillSlots[0].Type);

            p.LastGroundedY = 20f;
            p.IsGrounded = true;
            p.AimAngle = 0f;
            p.FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(p.Position.y, p.LastGroundedY, 0.01f,
                "Teleport must reset LastGroundedY to the new position to prevent inflated fall damage");
        }

        [Test]
        public void ShadowStep_ResetsLastGroundedY_OnRecall_WhenAirborne()
        {
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.ShadowStep));
            state.Players[0].Energy = 100f;

            p.LastGroundedY = 20f;
            p.IsGrounded = true;

            SkillSystem.ActivateSkill(state, 0, 0);
            float markedY = p.SkillTargetPosition.y;

            p.Position = new Vec2(p.Position.x, markedY + 10f);
            p.LastGroundedY = markedY + 10f;
            p.IsGrounded = false;

            state.Players[0].SkillSlots[0].DurationRemaining = 0f;
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(p.Position.y, p.LastGroundedY, 0.01f,
                "ShadowStep recall must reset LastGroundedY to prevent inflated fall damage");
        }

        [Test]
        public void ShadowStep_RecallSkillEvent_Position_IsPreRecallPosition()
        {
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.ShadowStep));
            state.Players[0].Energy = 100f;
            p.IsGrounded = true;

            Vec2 markPos = p.Position;
            SkillSystem.ActivateSkill(state, 0, 0);

            Vec2 preRecallPos = new Vec2(markPos.x + 8f, markPos.y + 5f);
            p.Position = preRecallPos;
            p.IsGrounded = false;

            state.SkillEvents.Clear();
            state.Players[0].SkillSlots[0].DurationRemaining = 0f;
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(1, state.SkillEvents.Count, "Recall must emit exactly one SkillEvent");
            var ev = state.SkillEvents[0];
            Assert.AreEqual(SkillType.ShadowStep, ev.Type);

            Assert.AreEqual(preRecallPos.x, ev.Position.x, 0.01f,
                "SkillEvent.Position must be the pre-recall position (from), not the mark");
            Assert.AreEqual(preRecallPos.y, ev.Position.y, 0.01f,
                "SkillEvent.Position must be the pre-recall position (from), not the mark");

            Assert.AreNotEqual(ev.Position.x, ev.TargetPosition.x,
                "Position and TargetPosition must differ — player moved away from the mark");
        }
    }
}
