using System;

namespace Baboomz.Simulation
{
    /// <summary>Skill activation (Execute*) methods — instant effects when a skill fires.</summary>
    public static partial class SkillSystem
    {
        static void ExecuteTeleport(GameState state, ref PlayerState p, ref SkillSlotState skill)
        {
            float range = skill.Range;
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            Vec2 target = p.Position + direction * range;

            // Raycast to find terrain obstacle
            if (GamePhysics.RaycastTerrain(state.Terrain, p.Position + new Vec2(0f, 0.5f), target, out Vec2 hitPoint))
            {
                // Place just before the hit, pull back 0.5 units along direction
                Vec2 pullback = direction * -0.5f;
                p.Position = hitPoint + pullback;
            }
            else
            {
                p.Position = target;
            }

            // Reset velocity so player doesn't retain pre-teleport momentum
            p.Velocity = Vec2.Zero;

            // Resolve any terrain penetration
            GamePhysics.ResolveTerrainPenetration(state.Terrain, ref p.Position, 3f);

            // Clamp to map bounds
            float halfMap = state.Config.MapWidth / 2f;
            GamePhysics.ClampToMapBounds(ref p.Position, -halfMap, halfMap,
                state.Config.DeathBoundaryY - 10f);

            // Re-check grounding after teleport.
            // Always reset LastGroundedY to the teleport destination so fall damage is
            // measured from the new position, not the pre-teleport grounded position.
            p.IsGrounded = GamePhysics.IsGrounded(state.Terrain, p.Position);
            p.LastGroundedY = p.Position.y;
        }

        static bool ExecuteGrappleStart(GameState state, ref PlayerState p, ref SkillSlotState skill,
            bool preserveMomentum = false)
        {
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            Vec2 target = p.Position + direction * skill.Range;

            if (!GamePhysics.RaycastTerrain(state.Terrain, p.Position + new Vec2(0f, 0.5f), target, out Vec2 hitPoint))
                return false;

            // Store attachment point and rope length for swing
            p.SkillTargetPosition = hitPoint;
            float ropeLen = Vec2.Distance(p.Position, hitPoint);
            p.RopeLength = Math.Max(ropeLen, 2f);
            skill.IsActive = true;
            skill.DurationRemaining = 2f; // 2 second swing duration
            p.IsGrounded = false;
            if (!preserveMomentum)
                p.Velocity = Vec2.Zero; // reset velocity for clean swing start
            return true;
        }

        static void ExecuteWarCry(GameState state, int casterIndex, ref SkillSlotState skill)
        {
            float duration = skill.Duration > 0f ? skill.Duration : 5f;
            float dmgMult = skill.Value > 0f ? skill.Value : 1.5f;

            bool isTeamMode = state.Config.TeamMode;
            int casterTeam = state.Players[casterIndex].TeamIndex;

            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState target = ref state.Players[i];
                if (target.IsDead) continue;

                bool isCaster = (i == casterIndex);
                bool isTeammate = isTeamMode && casterTeam >= 0 && target.TeamIndex == casterTeam;

                if (!isCaster && !isTeammate) continue;

                // Solo mode: caster gets stronger buff (1.9x dmg, 1.4x speed) — #126
                // Team mode: both get 1.5x dmg, 1.2x speed
                float appliedDmg = (!isTeamMode && isCaster) ? 1.9f : dmgMult;
                float appliedSpeed = (!isTeamMode && isCaster) ? 1.4f : 1.2f;

                // Don't stack with DoubleDamage — take higher multiplier
                target.DamageMultiplier = Math.Max(target.DamageMultiplier, appliedDmg);
                target.WarCryDamageBuff = appliedDmg;
                target.WarCryTimer = duration;

                // Restore previous speed buff before applying new one to prevent stacking
                if (target.WarCrySpeedBuff > 0f)
                    target.MoveSpeed /= target.WarCrySpeedBuff;
                target.WarCrySpeedBuff = appliedSpeed;
                target.MoveSpeed *= appliedSpeed;
            }
        }

        static void ExecuteDecoy(GameState state, ref PlayerState p, ref SkillSlotState skill)
        {
            p.DecoyPosition = p.Position;
            p.DecoyTimer = skill.Duration > 0f ? skill.Duration : 2f;
            p.IsInvisible = true;
            skill.IsActive = true;
            skill.DurationRemaining = skill.Duration > 0f ? skill.Duration : 2f;
        }

        static bool ExecuteHookShot(GameState state, int ci, ref PlayerState p, ref SkillSlotState skill)
        {
            float range = skill.Range > 0f ? skill.Range : 10f;
            float damage = skill.Value > 0f ? skill.Value : 10f;
            const float pullDistance = 5f;
            const float launchVelocityY = 2f;

            // Find nearest living enemy within range (same pattern as EnergyDrain)
            int target = -1;
            float closest = range;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == ci || state.Players[i].IsDead || state.Players[i].FreezeTimer > 0f) continue;
                if (state.Players[i].IsInvulnerable) continue;
                if (state.Config.TeamMode && p.TeamIndex >= 0 && state.Players[i].TeamIndex == p.TeamIndex) continue;
                float d = Vec2.Distance(p.Position, state.Players[i].Position);
                if (d < closest) { closest = d; target = i; }
            }
            if (target < 0) return false;

            ref PlayerState t = ref state.Players[target];

            // Apply damage (respects caster's DamageMultiplier and target's ArmorMultiplier)
            float finalDamage = damage * p.DamageMultiplier / MathF.Max(t.ArmorMultiplier, 0.01f);
            t.Health -= finalDamage;
            t.TotalDamageTaken += finalDamage;
            state.DamageEvents.Add(new DamageEvent
            {
                TargetIndex = target,
                Amount = finalDamage,
                Position = t.Position,
                SourceIndex = ci
            });

            // Track stats
            p.TotalDamageDealt += finalDamage;
            p.DirectHits++;
            if (finalDamage > p.MaxSingleDamage)
                p.MaxSingleDamage = finalDamage;

            // Pull target toward caster
            Vec2 direction = p.Position - t.Position;
            float dist = MathF.Sqrt(direction.x * direction.x + direction.y * direction.y);
            if (dist > 0.01f)
            {
                direction.x /= dist;
                direction.y /= dist;
                float actualPull = MathF.Min(pullDistance, dist - 1f); // don't pull past caster
                if (actualPull > 0f)
                {
                    t.Position.x += direction.x * actualPull;
                    t.Position.y += direction.y * actualPull;
                }
            }

            // Small upward velocity so target isn't stuck in ground
            t.Velocity.y = launchVelocityY;
            t.IsGrounded = false;

            // Resolve terrain penetration after pull
            GamePhysics.ResolveTerrainPenetration(state.Terrain, ref t.Position, 3f);

            // Clamp to map bounds
            float halfMap = state.Config.MapWidth / 2f;
            GamePhysics.ClampToMapBounds(ref t.Position, -halfMap, halfMap,
                state.Config.DeathBoundaryY - 10f);

            // Check death
            if (t.Health <= 0f && !t.IsDead)
            {
                t.Health = 0f;
                t.IsDead = true;
                GameSimulation.ScoreSurvivalKill(state, target);
                GameSimulation.DropCtfFlag(state, target);
                GameSimulation.SpawnHeadhunterTokens(state, target);
                CombatResolver.TrackKill(state, ci);
                state.Players[ci].TotalKills++;
                float killDist = Vec2.Distance(state.Players[ci].Position, t.Position);
                if (killDist <= 5f)
                    state.Players[ci].CloseRangeKills++;
            }

            return true;
        }

        static void ExecuteShadowStep(ref PlayerState p, ref SkillSlotState skill)
        {
            // Mark current position for recall
            p.SkillTargetPosition = p.Position;
            skill.IsActive = true;
            skill.DurationRemaining = skill.Duration;
        }

        static bool ExecuteOvercharge(GameState state, ref PlayerState p, ref SkillSlotState skill)
        {
            // Min-energy gate (stored in skill.Range): must have >= gate energy to commit
            float minEnergy = skill.Range > 0f ? skill.Range : 60f;
            if (p.Energy < minEnergy) return false;

            // Commit: drain all energy
            p.Energy = 0f;

            // Apply damage multiplier — take higher (don't stack with DoubleDamage/WarCry)
            float multiplier = skill.Value > 0f ? skill.Value : 2f;
            if (multiplier > p.DamageMultiplier)
                p.DamageMultiplier = multiplier;

            // Arm the overcharge window — consumed by Fire() or expires in Duration seconds
            p.OverchargeTimer = skill.Duration > 0f ? skill.Duration : 5f;
            return true;
        }

        static void ExecutePetrify(GameState state, int casterIndex, ref PlayerState p, ref SkillSlotState skill)
        {
            float range = skill.Range > 0f ? skill.Range : 10f;
            float radius = skill.Value > 0f ? skill.Value : 2f;
            float freezeDuration = skill.Duration > 0f ? skill.Duration : 2f;

            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            Vec2 target = p.Position + direction * range;

            int casterTeam = p.TeamIndex;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == casterIndex) continue;
                ref PlayerState t = ref state.Players[i];
                if (t.IsDead || t.IsInvulnerable) continue;
                if (state.Config.TeamMode && casterTeam >= 0 && t.TeamIndex == casterTeam) continue;

                float dist = Vec2.Distance(t.Position, target);
                if (dist <= radius)
                    t.FreezeTimer = MathF.Max(t.FreezeTimer, freezeDuration);
            }
        }

        static bool ExecuteEnergyDrain(GameState state, int ci, ref PlayerState p, ref SkillSlotState skill)
        {
            float range = skill.Range > 0f ? skill.Range : 12f;
            int target = -1; float closest = range;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == ci || state.Players[i].IsDead || state.Players[i].FreezeTimer > 0f) continue;
                if (state.Players[i].IsInvulnerable) continue;
                if (state.Config.TeamMode && p.TeamIndex >= 0 && state.Players[i].TeamIndex == p.TeamIndex) continue;
                float d = Vec2.Distance(p.Position, state.Players[i].Position);
                if (d < closest) { closest = d; target = i; }
            }
            if (target < 0) return false;
            ref PlayerState t = ref state.Players[target];
            float drained = MathF.Min(skill.Value > 0f ? skill.Value : 30f, t.Energy);
            // #163: match HookShot/Overcharge whiff behaviour — if the target
            // has no energy to drain, refund cost + skip cooldown instead of
            // consuming the cast for a zero-payload "success".
            if (drained <= 0f) return false;
            t.Energy -= drained;
            p.Energy = MathF.Min(p.Energy + drained, p.MaxEnergy);
            if (t.IsCharging) t.IsCharging = false;
            state.EnergyDrainEvents.Add(new EnergyDrainEvent { CasterIndex = ci, TargetIndex = target, AmountDrained = drained });
            return true;
        }
    }
}
