using System;

namespace Baboomz.Simulation
{
    /// <summary>Skill update, deactivation, and tick logic (partial class of SkillSystem).</summary>
    public static partial class SkillSystem
    {
        static void UpdateRopeSwing(GameState state, ref PlayerState p, ref SkillSlotState skill, float dt, int playerIndex = 0)
        {
            // Frozen players don't swing — hold position, zero velocity
            if (p.FreezeTimer > 0f)
            {
                p.Velocity = Vec2.Zero;
                return;
            }

            Vec2 anchor = p.SkillTargetPosition;
            float ropeLen = p.RopeLength;
            Vec2 toPlayer = p.Position - anchor;
            float angle = MathF.Atan2(toPlayer.x, -toPlayer.y);

            float angularVel = 0f;
            if (ropeLen > 0.1f)
            {
                Vec2 tangent = new Vec2(-MathF.Cos(angle), -MathF.Sin(angle));
                angularVel = (p.Velocity.x * tangent.x + p.Velocity.y * tangent.y) / ropeLen;
            }

            angularVel += state.Config.Gravity * MathF.Sin(angle) / Math.Max(ropeLen, 1f) * dt;
            if (!p.IsAI) angularVel += state.PlayerInputs[playerIndex].MoveX * 3f * dt;
            angularVel *= (1f - 0.5f * dt);
            angle += angularVel * dt;

            Vec2 newPos = new Vec2(anchor.x + MathF.Sin(angle) * ropeLen, anchor.y - MathF.Cos(angle) * ropeLen);
            float halfMap = state.Config.MapWidth / 2f;
            newPos.x = Math.Clamp(newPos.x, -halfMap, halfMap);

            Vec2 newTangent = new Vec2(MathF.Cos(angle), MathF.Sin(angle));
            p.Velocity = newTangent * (angularVel * ropeLen);
            p.Position = newPos;
            p.IsGrounded = false;
        }

        static void TickDeflect(GameState state, int deflectorIndex, ref SkillSlotState skill)
        {
            ref PlayerState p = ref state.Players[deflectorIndex];
            float radius = skill.Range > 0f ? skill.Range : 2.5f;
            float radiusSq = radius * radius;

            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                var proj = state.Projectiles[i];
                if (!proj.Alive) continue;

                // Cannot deflect stuck projectiles (sticky bombs attached to player/terrain)
                if (proj.IsSticky && (proj.StuckToPlayerId >= 0 || proj.StuckToTerrain)) continue;

                // Cannot deflect own projectiles
                if (proj.OwnerIndex == deflectorIndex) continue;

                float dx = proj.Position.x - p.Position.x;
                float dy = proj.Position.y - p.Position.y;
                float distSq = dx * dx + dy * dy;
                if (distSq > radiusSq) continue;

                int originalOwner = proj.OwnerIndex;
                float speed = MathF.Sqrt(proj.Velocity.x * proj.Velocity.x +
                                          proj.Velocity.y * proj.Velocity.y);
                speed = MathF.Max(speed, 5f); // ensure minimum speed after deflect

                // Owner-less projectiles (mines, environment): just reverse velocity
                if (originalOwner < 0 || originalOwner >= state.Players.Length)
                {
                    proj.Velocity.x = -proj.Velocity.x;
                    proj.Velocity.y = -proj.Velocity.y;
                }
                else
                {
                    // Aim toward original shooter's position
                    Vec2 targetPos = state.Players[originalOwner].Position;
                    float tdx = targetPos.x - proj.Position.x;
                    float tdy = targetPos.y - proj.Position.y;
                    float tDist = MathF.Sqrt(tdx * tdx + tdy * tdy);

                    if (tDist > 0.01f)
                    {
                        proj.Velocity.x = (tdx / tDist) * speed;
                        proj.Velocity.y = (tdy / tDist) * speed;
                    }
                    else
                    {
                        proj.Velocity.x = -proj.Velocity.x;
                        proj.Velocity.y = -proj.Velocity.y;
                    }
                }

                // Flip ownership to deflector
                proj.OwnerIndex = deflectorIndex;

                state.Projectiles[i] = proj;
            }
        }

        static void UpdateDecoy(GameState state, ref PlayerState p, ref SkillSlotState skill, float dt, int playerIndex)
        {
            // Splash damage reveals the invisible player early
            for (int d = 0; d < state.DamageEvents.Count; d++)
            {
                if (state.DamageEvents[d].TargetIndex == playerIndex && state.DamageEvents[d].Amount > 0f)
                {
                    // Force DurationRemaining to zero so the Update loop calls DeactivateSkill,
                    // which clears both visibility state and skill.IsActive. Without this the
                    // player would be revealed but skill-locked for the remainder of the duration.
                    skill.DurationRemaining = 0f;
                    return;
                }
            }
        }

        static void ClearDecoy(ref PlayerState p)
        {
            p.DecoyTimer = 0f;
            p.IsInvisible = false;
            p.DecoyPosition = Vec2.Zero;
        }

        static void ClearSprint(ref PlayerState p)
        {
            if (p.SprintSpeedBuff > 0f)
            {
                p.MoveSpeed /= p.SprintSpeedBuff;
                p.SprintSpeedBuff = 0f;
            }
            p.SprintTimer = 0f;
        }

        static void DeactivateShadowStep(GameState state, ref PlayerState p, int playerIndex)
        {
            // Capture pre-recall position before teleporting (mirrors Teleport fix in SkillSystem.cs)
            Vec2 fromPos = p.Position;

            // Recall: teleport back to marked position
            p.Position = p.SkillTargetPosition;
            p.Velocity = Vec2.Zero;

            // Resolve terrain penetration (terrain may have been destroyed at mark point)
            GamePhysics.ResolveTerrainPenetration(state.Terrain, ref p.Position, 3f);

            // Clamp to map bounds
            float halfMap = state.Config.MapWidth / 2f;
            GamePhysics.ClampToMapBounds(ref p.Position, -halfMap, halfMap,
                state.Config.DeathBoundaryY - 10f);

            // Re-check grounding — if terrain was destroyed, player falls.
            // Always reset LastGroundedY to the recall destination so fall damage is
            // measured from the new position, not the pre-ShadowStep grounded position.
            p.IsGrounded = GamePhysics.IsGrounded(state.Terrain, p.Position);
            p.LastGroundedY = p.Position.y;

            // Emit return event for VFX: Position = where the player came FROM,
            // TargetPosition = where they arrived (the mark).
            state.SkillEvents.Add(new SkillEvent
            {
                PlayerIndex = playerIndex,
                Type = SkillType.ShadowStep,
                Position = fromPos,
                TargetPosition = p.SkillTargetPosition
            });
        }
    }
}
