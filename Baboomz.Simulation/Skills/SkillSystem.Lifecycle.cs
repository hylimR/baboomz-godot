using System;

namespace Baboomz.Simulation
{
    public static partial class SkillSystem
    {
        static void UpdateActiveSkill(GameState state, ref PlayerState p, ref SkillSlotState skill, float dt, int playerIndex)
        {
            switch (skill.Type)
            {
                case SkillType.Heal:
                    if (p.FreezeTimer > 0f) return;
                    float healDuration = skill.Duration > 0f ? skill.Duration : 1f;
                    float healPerSecond = skill.Value / healDuration;
                    p.Health = MathF.Min(p.MaxHealth, p.Health + healPerSecond * dt);
                    break;

                case SkillType.Jetpack:
                    if (p.FreezeTimer > 0f) { p.Velocity = Vec2.Zero; return; }
                    p.Velocity.y = skill.Value;
                    p.IsGrounded = false;
                    break;

                case SkillType.GrapplingHook:
                    UpdateRopeSwing(state, ref p, ref skill, dt, playerIndex);
                    break;

                case SkillType.Dash:
                    if (p.FreezeTimer > 0f) { p.Velocity = Vec2.Zero; return; }
                    p.Velocity.x = skill.Value * p.FacingDirection;
                    break;

                case SkillType.Shield:
                    break;

                case SkillType.Deflect:
                    TickDeflect(state, playerIndex, ref skill);
                    break;

                case SkillType.Decoy:
                    UpdateDecoy(state, ref p, ref skill, dt, playerIndex);
                    break;

                case SkillType.ShadowStep:
                    break;
            }
        }

        static void DeactivateSkill(GameState state, ref PlayerState p, ref SkillSlotState skill, int playerIndex)
        {
            skill.IsActive = false;
            skill.DurationRemaining = 0f;

            switch (skill.Type)
            {
                case SkillType.Shield:
                    p.ArmorMultiplier = skill.Value2 > 0f ? skill.Value2 : state.Config.DefaultArmorMultiplier;
                    break;

                case SkillType.GrapplingHook:
                    DeactivateRope(state, ref p);
                    break;

                case SkillType.Deflect:
                    p.DeflectTimer = 0f;
                    break;

                case SkillType.Decoy:
                    ClearDecoy(ref p);
                    break;

                case SkillType.ShadowStep:
                    DeactivateShadowStep(state, ref p, playerIndex);
                    break;
            }
        }
    }
}
