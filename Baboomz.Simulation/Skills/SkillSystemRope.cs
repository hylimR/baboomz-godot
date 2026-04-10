namespace Baboomz.Simulation
{
    /// <summary>
    /// Ninja Rope re-hook logic: voluntary detach, momentum chaining, re-hook window.
    /// Max 5 re-hooks per activation, 0.5s window after detach, no energy cost.
    /// </summary>
    public static partial class SkillSystem
    {
        const int MaxRopeRehooks = 5;
        const float RopeRehookWindowDuration = 0.5f;

        /// <summary>
        /// Handles rope-specific early returns in ActivateSkill.
        /// Returns true if the activation was consumed (caller should return).
        /// </summary>
        internal static bool TryHandleRopeActivation(GameState state, ref PlayerState p,
            ref SkillSlotState skill, int playerIndex)
        {
            // Pressing skill during active swing: voluntary detach (preserves momentum)
            if (skill.IsActive)
            {
                DeactivateSkill(state, ref p, ref skill, playerIndex);
                return true;
            }

            // Re-hook during window: attach to new anchor with preserved momentum
            if (p.RopeRehookWindow > 0f && p.RopeHookCount < MaxRopeRehooks && !p.IsGrounded)
            {
                if (ExecuteGrappleStart(state, ref p, ref skill, preserveMomentum: true))
                {
                    p.RopeHookCount++;
                    p.RopeRehookWindow = 0f;
                    state.SkillEvents.Add(new SkillEvent
                    {
                        PlayerIndex = playerIndex,
                        Type = SkillType.GrapplingHook,
                        Position = p.Position,
                        TargetPosition = p.SkillTargetPosition
                    });
                }
                return true;
            }

            return false;
        }

        /// <summary>Tick the rope re-hook window timer; landing or expiry resets state.</summary>
        internal static void TickRopeRehookWindow(ref PlayerState p, float dt)
        {
            if (p.RopeRehookWindow <= 0f) return;

            p.RopeRehookWindow -= dt;
            if (p.RopeRehookWindow <= 0f)
            {
                p.RopeRehookWindow = 0f;
                p.RopeHookCount = 0;
            }

            if (p.IsGrounded)
            {
                p.RopeRehookWindow = 0f;
                p.RopeHookCount = 0;
            }
        }

        /// <summary>GrapplingHook-specific deactivation: opens re-hook window if airborne.</summary>
        internal static void DeactivateRope(GameState state, ref PlayerState p)
        {
            p.IsGrounded = GamePhysics.IsGrounded(state.Terrain, p.Position);
            if (p.IsGrounded)
            {
                p.LastGroundedY = p.Position.y;
                p.RopeHookCount = 0;
                p.RopeRehookWindow = 0f;
            }
            else
            {
                p.RopeRehookWindow = RopeRehookWindowDuration;
            }
        }
    }
}
