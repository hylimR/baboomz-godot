using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Mob-specific AI behaviors: Bomber, Shielder, Flyer, Healer.
    /// Partial class extension of AILogic.
    /// </summary>
    public static partial class AILogic
    {
        // ── Bomber ───────────────────────────────────────────────────────────
        // Moves to range 10-15, lobs bouncing grenades. Flees if too close.
        static void UpdateBomber(GameState state, int index, float dt)
        {
            ref PlayerState ai = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) { ai.Velocity.x = 0f; return; }

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - ai.Position;
            float dist = MathF.Abs(toTarget.x);
            ai.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            if (dist < 8f)
                ai.Velocity.x = -ai.FacingDirection * ai.MoveSpeed;
            else if (dist > 16f)
                ai.Velocity.x = ai.FacingDirection * ai.MoveSpeed;
            else if (t >= mobRepoTime[index])
            {
                // Start repositioning: pick random direction, move for 1-2 seconds
                moveDirection[index] = rng.NextDouble() > 0.5 ? 1f : -1f;
                moveEndTime[index] = t + 1f + (float)(rng.NextDouble() * 1.0);
                mobRepoTime[index] = moveEndTime[index] + 4f + (float)(rng.NextDouble() * 4.0);
                ai.Velocity.x = moveDirection[index] * ai.MoveSpeed;
            }
            else if (t < moveEndTime[index])
                ai.Velocity.x = moveDirection[index] * ai.MoveSpeed;
            else
                ai.Velocity.x = 0f;

            if (t >= nextShootTime[index] && ai.ShootCooldownRemaining <= 0f && dist < 20f)
            {
                float lobAngle = 55f + (float)(rng.NextDouble() * 15.0);
                float error = (float)(rng.NextDouble() * 8.0 - 4.0);
                ai.AimAngle = lobAngle + error;

                var weapon = ai.WeaponSlots[ai.ActiveWeaponSlot];
                float power = Math.Clamp(dist * 0.8f, weapon.MinPower, weapon.MaxPower);
                ai.AimPower = power;

                GameSimulation.Fire(state, index);
                ai.AimPower = 0f;

                nextShootTime[index] = t + 3f + (float)(rng.NextDouble() * 2.0);
            }
        }

        // ── Shielder ─────────────────────────────────────────────────────────
        // Slow advance with frontal shield. Melee attacks when close.
        static void UpdateShielder(GameState state, int index, float dt)
        {
            ref PlayerState ai = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) { ai.Velocity.x = 0f; return; }

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - ai.Position;
            float dist = MathF.Abs(toTarget.x);
            ai.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            if (dist > 2.5f)
            {
                ai.Velocity.x = ai.FacingDirection * ai.MoveSpeed;
            }
            else
            {
                ai.Velocity.x = 0f;
                if (t >= nextShootTime[index] && ai.ShootCooldownRemaining <= 0f)
                {
                    ai.AimAngle = 0f;
                    ai.AimPower = 5f;
                    GameSimulation.Fire(state, index);
                    ai.AimPower = 0f;
                    nextShootTime[index] = t + 1.5f;
                }
            }
        }

        // ── Flyer ────────────────────────────────────────────────────────────
        // Hovers above terrain, orbits player, rapid weak shots.
        static void UpdateFlyer(GameState state, int index, float dt)
        {
            ref PlayerState ai = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) { ai.Velocity.x = 0f; return; }

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - ai.Position;
            float distX = MathF.Abs(toTarget.x);
            ai.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            float groundY = GamePhysics.FindGroundY(state.Terrain, ai.Position.x, ai.Position.y + 20f);
            float hoverTarget = groundY + 10f;
            float yDiff = hoverTarget - ai.Position.y;
            ai.Velocity.y = Math.Clamp(yDiff * 3f, -6f, 6f);

            float orbitRadius = 15f;
            float orbitSpeed = 2f;
            float phaseOffset = index * MathF.PI * 0.67f;
            float desiredX = target.Position.x + MathF.Sin(t * orbitSpeed + phaseOffset) * orbitRadius;
            float xDiff = desiredX - ai.Position.x;
            ai.Velocity.x = Math.Clamp(xDiff * 2f, -ai.MoveSpeed, ai.MoveSpeed);

            if (t >= nextShootTime[index] && ai.ShootCooldownRemaining <= 0f)
            {
                float angle = MathF.Atan2(toTarget.y, MathF.Abs(toTarget.x)) * (180f / MathF.PI);
                float error = (float)(rng.NextDouble() * 10.0 - 5.0);
                ai.AimAngle = angle + error;

                var weapon = ai.WeaponSlots[ai.ActiveWeaponSlot];
                float power = Math.Clamp(distX * 0.7f, weapon.MinPower, weapon.MaxPower);
                ai.AimPower = power;

                GameSimulation.Fire(state, index);
                ai.AimPower = 0f;

                nextShootTime[index] = t + 2f + (float)(rng.NextDouble() * 0.5);
            }
        }

        // ── Healer ───────────────────────────────────────────────────────────
        // No attacks. Heals nearest damaged ally. Flees from player.
        static void UpdateHealer(GameState state, int index, float dt)
        {
            ref PlayerState ai = ref state.Players[index];

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) { ai.Velocity.x = 0f; return; }

            ref PlayerState target = ref state.Players[targetIdx];
            float distToPlayer = MathF.Abs(target.Position.x - ai.Position.x);

            if (distToPlayer < 8f)
            {
                int fleeDir = target.Position.x > ai.Position.x ? -1 : 1;
                ai.Velocity.x = fleeDir * ai.MoveSpeed;
                ai.FacingDirection = fleeDir;
            }
            else
            {
                int allyIdx = -1;
                float bestRatio = float.MaxValue;
                for (int i = 0; i < state.Players.Length; i++)
                {
                    if (i == index || state.Players[i].IsDead) continue;
                    if (!state.Players[i].IsMob) continue;
                    if (state.Players[i].Health >= state.Players[i].MaxHealth) continue;
                    float ratio = state.Players[i].Health / state.Players[i].MaxHealth;
                    if (ratio < bestRatio) { bestRatio = ratio; allyIdx = i; }
                }

                float allyDist = allyIdx >= 0 ? Vec2.Distance(ai.Position, state.Players[allyIdx].Position) : 0f;
                if (allyIdx >= 0 && allyDist > 6f)
                {
                    float dir = state.Players[allyIdx].Position.x > ai.Position.x ? 1f : -1f;
                    ai.Velocity.x = dir * ai.MoveSpeed;
                    ai.FacingDirection = (int)dir;
                }
                else
                    ai.Velocity.x = 0f;
            }

            // Heal most damaged ally within 10 units (lowest HP ratio)
            int healIdx = -1;
            float lowestRatio = float.MaxValue;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == index || state.Players[i].IsDead) continue;
                if (!state.Players[i].IsMob) continue;
                if (state.Players[i].Health >= state.Players[i].MaxHealth) continue;

                float dist = Vec2.Distance(ai.Position, state.Players[i].Position);
                if (dist <= 10f)
                {
                    float ratio = state.Players[i].Health / state.Players[i].MaxHealth;
                    if (ratio < lowestRatio)
                    {
                        lowestRatio = ratio;
                        healIdx = i;
                    }
                }
            }
            if (healIdx >= 0)
            {
                state.Players[healIdx].Health = MathF.Min(
                    state.Players[healIdx].Health + 5f * dt,
                    state.Players[healIdx].MaxHealth);
            }
        }
    }
}
