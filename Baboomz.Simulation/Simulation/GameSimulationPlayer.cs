using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Player-specific simulation: input processing, movement, and physics.
    /// Player factory (CreatePlayer) lives in GameSimulationPlayerFactory.cs.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        static void ProcessInput(GameState state, int playerIndex, float dt)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            if (p.IsDead || p.IsAI) return;
            if (p.FreezeTimer > 0f) { p.Velocity = Vec2.Zero; return; } // frozen: no input or movement

            var input = state.PlayerInputs[playerIndex];
            var config = state.Config;

            if (p.IsSwimming)
            {
                float swimSpeed = p.MoveSpeed * config.SwimSpeedMultiplier;
                p.Velocity.x = input.MoveX * swimSpeed;
                if (input.MoveX > 0.1f) p.FacingDirection = 1;
                else if (input.MoveX < -0.1f) p.FacingDirection = -1;
                return;
            }

            p.Velocity.x = input.MoveX * p.MoveSpeed;
            if (input.MoveX > 0.1f) p.FacingDirection = 1;
            else if (input.MoveX < -0.1f) p.FacingDirection = -1;

            if (input.JumpPressed && p.IsGrounded)
            {
                p.Velocity.y = p.JumpForce;
                p.IsGrounded = false;
            }

            p.AimAngle += input.AimDelta * config.AimAngleSpeed * dt;
            p.AimAngle = Math.Clamp(p.AimAngle, -90f, 90f);

            // Invisible players (decoy active) can move but cannot fire, use skills, or emote
            if (!p.IsInvisible)
            {
                if (input.Skill1Pressed) SkillSystem.ActivateSkill(state, playerIndex, 0);
                if (input.Skill2Pressed) SkillSystem.ActivateSkill(state, playerIndex, 1);

                // Emote input (1-4 mapped to emote types)
                if (input.EmotePressed > 0 && input.EmotePressed <= 4)
                    TriggerEmote(state, playerIndex, (EmoteType)input.EmotePressed);
            }

            if (input.WeaponSlotPressed >= 0 && input.WeaponSlotPressed < p.WeaponSlots.Length
                && state.Config.MatchType != MatchType.ArmsRace
                && state.Config.MatchType != MatchType.Roulette)
            {
                if (p.WeaponSlots[input.WeaponSlotPressed].WeaponId != null)
                    p.ActiveWeaponSlot = input.WeaponSlotPressed;
            }

            // Scroll through weapon slots with [ ] or mouse wheel (#377)
            if (input.WeaponScrollDelta != 0 && state.Config.MatchType != MatchType.ArmsRace
                && state.Config.MatchType != MatchType.Roulette)
            {
                int total = p.WeaponSlots.Length;
                int dir = input.WeaponScrollDelta > 0 ? 1 : -1;
                for (int step = 1; step <= total; step++)
                {
                    int next = ((p.ActiveWeaponSlot + step * dir) % total + total) % total;
                    if (p.WeaponSlots[next].WeaponId != null)
                    {
                        p.ActiveWeaponSlot = next;
                        break;
                    }
                }
            }

            var weapon = p.WeaponSlots[p.ActiveWeaponSlot];
            if (weapon.WeaponId == null) return;

            // Invisible players cannot fire
            if (p.IsInvisible) return;

            if (input.FireHeld && p.ShootCooldownRemaining <= 0f)
            {
                p.IsCharging = true;
                float chargeTime = weapon.ChargeTime > 0f ? weapon.ChargeTime : 1f;
                p.AimPower += (weapon.MaxPower - weapon.MinPower) / chargeTime * dt;
                p.AimPower = Math.Clamp(p.AimPower, weapon.MinPower, weapon.MaxPower);
            }

            if (input.FireReleased && p.IsCharging)
            {
                Fire(state, playerIndex);
                p.IsCharging = false;
                p.AimPower = 0f;
            }
        }

        static void UpdatePlayer(GameState state, int index, float dt)
        {
            ref PlayerState p = ref state.Players[index];
            if (p.IsDead) return;

            bool frozen = p.FreezeTimer > 0f;
            if (frozen) p.Velocity = Vec2.Zero;

            if (p.IsSwimming)
            {
                float swimSpeed = p.MoveSpeed * state.Config.SwimSpeedMultiplier;
                p.Velocity.x = Math.Clamp(p.Velocity.x, -swimSpeed, swimSpeed);
                p.Velocity.y = -state.Config.SwimSinkSpeed;
            }

            // Skip gravity while rope-swinging — UpdateRopeSwing already models gravity
            // as angular acceleration. Applying it again here causes double-gravity (~√2× speed).
            bool isSwinging = false;
            if (p.SkillSlots != null)
            {
                for (int s = 0; s < p.SkillSlots.Length; s++)
                    if (p.SkillSlots[s].IsActive && p.SkillSlots[s].Type == SkillType.GrapplingHook)
                    { isSwinging = true; break; }
            }

            if (!p.IsGrounded && !isSwinging && !p.IsSwimming)
                GamePhysics.ApplyGravity(ref p.Velocity, dt, state.Config.Gravity);
            else if (!isSwinging && !p.IsSwimming && p.Velocity.y < 0f)
                p.Velocity.y = 0f;

            Vec2 newPos = p.Position + p.Velocity * dt;
            if (MathF.Abs(p.Velocity.x) > 0.01f)
            {
                // Sample at 3 vertical points (feet, chest, head) matching the player body.
                // A single chest-height sample missed low ledges (feet) and overhangs (head).
                float checkX = newPos.x + (p.Velocity.x > 0 ? 0.4f : -0.4f);
                int cx = state.Terrain.WorldToPixelX(checkX);
                bool wallHit =
                    state.Terrain.IsSolid(cx, state.Terrain.WorldToPixelY(p.Position.y + 0.1f)) ||
                    state.Terrain.IsSolid(cx, state.Terrain.WorldToPixelY(p.Position.y + 0.8f)) ||
                    state.Terrain.IsSolid(cx, state.Terrain.WorldToPixelY(p.Position.y + 1.4f));
                if (wallHit)
                {
                    newPos.x = p.Position.x;
                    p.Velocity.x = 0f;
                }
            }

            // Upward terrain collision — prevent clipping through ceilings/overhangs
            if (p.Velocity.y > 0f)
            {
                float headY = newPos.y + 1.5f;
                int headPy = state.Terrain.WorldToPixelY(headY);
                int cx = state.Terrain.WorldToPixelX(newPos.x);
                if (state.Terrain.IsSolid(cx, headPy)
                    || state.Terrain.IsSolid(state.Terrain.WorldToPixelX(newPos.x - 0.3f), headPy)
                    || state.Terrain.IsSolid(state.Terrain.WorldToPixelX(newPos.x + 0.3f), headPy))
                {
                    newPos.y = p.Position.y;
                    p.Velocity.y = 0f;
                }
            }

            if (p.IsGrounded && MathF.Abs(p.Velocity.x) > 0.01f)
            {
                float groundY = GamePhysics.FindGroundY(state.Terrain, newPos.x, newPos.y + 2f, 0.1f);
                float yDiff = groundY - newPos.y;
                if (yDiff > -1.5f && yDiff < 1.5f)
                    newPos.y = groundY;
            }

            p.Position = newPos;

            bool wasGroundedBefore = p.IsGrounded;
            p.IsGrounded = GamePhysics.IsGrounded(state.Terrain, p.Position);
            if (p.IsGrounded && p.Velocity.y <= 0f)
            {
                GamePhysics.ResolveTerrainPenetration(state.Terrain, ref p.Position);
                p.Velocity.y = 0f;
            }

            if (p.IsGrounded && !wasGroundedBefore && !p.IsInvulnerable)
            {
                float fallDist = p.LastGroundedY - p.Position.y;
                if (fallDist > state.Config.FallDamageMinDistance)
                {
                    float excess = fallDist - state.Config.FallDamageMinDistance;
                    float damage = MathF.Min(excess * state.Config.FallDamagePerMeter, state.Config.FallDamageMax);
                    damage *= (1f / MathF.Max(p.ArmorMultiplier, 0.01f));
                    p.Health -= damage;
                    p.TotalDamageTaken += damage;
                    state.DamageEvents.Add(new DamageEvent { TargetIndex = index, Amount = damage, Position = p.Position, SourceIndex = -1 });
                    if (p.Health <= 0f) { p.Health = 0f; p.IsDead = true; ScoreSurvivalKill(state, index); DropCtfFlag(state, index); SpawnHeadhunterTokens(state, index); }
                }
            }

            if (p.IsGrounded) p.LastGroundedY = p.Position.y;

            float halfMap = state.Config.MapWidth / 2f;
            GamePhysics.ClampToMapBounds(ref p.Position, -halfMap, halfMap, state.Config.DeathBoundaryY - 10f);

            if (p.ShootCooldownRemaining > 0f) p.ShootCooldownRemaining -= dt;
            if (p.Energy < p.MaxEnergy) p.Energy = MathF.Min(p.MaxEnergy, p.Energy + p.EnergyRegen * dt);
            if (p.HealthRegen > 0f && p.Health < p.MaxHealth)
                p.Health = MathF.Min(p.MaxHealth, p.Health + p.HealthRegen * dt);

            // Tick retreat timer
            if (p.RetreatTimer > 0f) p.RetreatTimer -= dt;
            if (p.FreezeTimer > 0f) p.FreezeTimer -= dt;
            if (p.DeflectTimer > 0f) p.DeflectTimer -= dt;
            if (p.FirecrackerCooldown > 0f) p.FirecrackerCooldown -= dt;

            TickBuffTimers(state, ref p, dt);
        }
    }
}
