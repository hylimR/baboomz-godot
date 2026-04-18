using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        [Test]
        public void HookShot_PullsTargetTowardCaster()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            Vec2 targetPosBefore = state.Players[1].Position;
            Vec2 casterPos = state.Players[0].Position;

            SkillSystem.ActivateSkill(state, 0, 0);

            float distBefore = Vec2.Distance(casterPos, targetPosBefore);
            float distAfter = Vec2.Distance(casterPos, state.Players[1].Position);
            Assert.Less(distAfter, distBefore, "Target should be pulled closer to caster");
        }

        [Test]
        public void HookShot_DealsDamage()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[1].Health, hpBefore, "Target should take damage");
            Assert.AreEqual(1, state.DamageEvents.Count, "Should emit a DamageEvent");
            Assert.AreEqual(1, state.DamageEvents[0].TargetIndex);
        }

        [Test]
        public void HookShot_NoTarget_RefundsEnergy()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;

            state.Players[1].IsDead = true;

            float energyBefore = state.Players[0].Energy;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when no valid target");
            Assert.AreEqual(0, state.SkillEvents.Count,
                "No skill event should be emitted on refund");
        }

        [Test]
        public void HookShot_SkipsTeammates()
        {
            var state = CreateState();
            state.Config.TeamMode = true;
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;

            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when only target is a teammate");
        }

        [Test]
        public void HookShot_SkipsFrozenTargets()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].FreezeTimer = 5f;

            float energyBefore = state.Players[0].Energy;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when only target is frozen");
        }

        [Test]
        public void HookShot_IsInstant()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "HookShot should be instant (no duration)");
        }

        [Test]
        public void HookShot_EmitsSkillEvent()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count);
            Assert.AreEqual(SkillType.HookShot, state.SkillEvents[0].Type);
        }

        [Test]
        public void HookShot_TracksDamageStats()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].TotalDamageDealt, 0f);
            Assert.AreEqual(1, state.Players[0].DirectHits);
        }

        [Test]
        public void HookShot_TargetGetsUpwardVelocity()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[1].Velocity.y, 0f,
                "Target should receive upward velocity after pull");
        }
    }
}
