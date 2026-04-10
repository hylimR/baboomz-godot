using System;

namespace Baboomz.Simulation
{
    public static partial class BossLogic
    {
        // 3-phase final boss.
        // Phase 1 (100-66%): stationary, precision shots + gear bombs
        // Phase 2 (66-33%): teleports, dual cannons
        // Phase 3 (33-0%): aggressive walk, rapid fire, desperation blast at 10%
        static void UpdateBaronCogsworth(GameState state, int index, float dt)
        {
            ref PlayerState boss = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) return;

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - boss.Position;
            float dist = MathF.Abs(toTarget.x);
            boss.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            float hpRatio = boss.Health / boss.MaxHealth;

            // Phase transitions
            if (hpRatio <= 0.66f && boss.BossPhase < 1)
            {
                boss.BossPhase = 1;
                specialTimer[index] = t + 10f;
            }
            if (hpRatio <= 0.33f && boss.BossPhase < 2)
            {
                boss.BossPhase = 2;
            }

            var weapon = boss.WeaponSlots[boss.ActiveWeaponSlot];

            switch (boss.BossPhase)
            {
                case 0: // Phase 1: stationary precision
                    boss.Velocity.x = 0f;

                    if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
                    {
                        float angle = MathF.Atan2(toTarget.y, MathF.Abs(toTarget.x)) * (180f / MathF.PI);
                        if (angle < 10f) angle = 25f;
                        float error = (float)(rng.NextDouble() * 6.0 - 3.0);
                        boss.AimAngle = angle + error;
                        boss.AimPower = Math.Clamp(dist * 0.9f, weapon.MinPower, weapon.MaxPower);
                        GameSimulation.Fire(state, index);
                        boss.AimPower = 0f;
                        attackTimer[index] = t + 3f;
                    }

                    // Gear bomb every 8s
                    if (t >= specialTimer[index])
                    {
                        boss.AimAngle = 60f + (float)(rng.NextDouble() * 10.0);
                        boss.AimPower = Math.Clamp(dist * 0.7f, weapon.MinPower, weapon.MaxPower);
                        GameSimulation.Fire(state, index);
                        boss.AimPower = 0f;
                        specialTimer[index] = t + 8f;
                    }
                    break;

                case 1: // Phase 2: teleport + dual cannons
                    boss.Velocity.x = 0f;

                    // Teleport every 10s
                    if (t >= stateTimer[index])
                    {
                        float newX = boss.Position.x + (float)(rng.NextDouble() * 40.0 - 20.0);
                        float halfMap = state.Config.MapWidth / 2f;
                        boss.Position.x = Math.Clamp(newX, -halfMap, halfMap);
                        stateTimer[index] = t + 10f;
                    }

                    // Dual cannons every 4s
                    if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
                    {
                        float baseAngle = MathF.Atan2(toTarget.y, MathF.Abs(toTarget.x)) * (180f / MathF.PI);
                        if (baseAngle < 10f) baseAngle = 25f;
                        float power = Math.Clamp(dist * 0.85f, weapon.MinPower, weapon.MaxPower);

                        boss.AimAngle = baseAngle - 5f;
                        boss.AimPower = power;
                        boss.ShootCooldownRemaining = 0f;
                        GameSimulation.Fire(state, index);
                        boss.AimAngle = baseAngle + 5f;
                        boss.ShootCooldownRemaining = 0f;
                        GameSimulation.Fire(state, index);
                        boss.AimPower = 0f;
                        attackTimer[index] = t + 4f;
                    }
                    break;

                case 2: // Phase 3: aggressive walk + rapid fire
                    boss.Velocity.x = boss.FacingDirection * 4f;

                    if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
                    {
                        float angle = MathF.Atan2(toTarget.y, MathF.Abs(toTarget.x)) * (180f / MathF.PI);
                        if (angle < 5f) angle = 15f;
                        float error = (float)(rng.NextDouble() * 10.0 - 5.0);
                        boss.AimAngle = angle + error;
                        boss.AimPower = Math.Clamp(dist * 0.9f, weapon.MinPower, weapon.MaxPower);
                        GameSimulation.Fire(state, index);
                        boss.AimPower = 0f;
                        attackTimer[index] = t + 1.5f;
                    }

                    // Desperation blast at 10% HP (one-time)
                    if (hpRatio <= 0.1f && subState[index] == 0)
                    {
                        subState[index] = 1;
                        for (int s = 0; s < 8; s++)
                        {
                            float angle = 20f + s * 18f;
                            boss.AimAngle = angle;
                            boss.AimPower = weapon.MaxPower * 0.8f;
                            boss.ShootCooldownRemaining = 0f;
                            GameSimulation.Fire(state, index);
                        }
                        boss.AimPower = 0f;
                    }
                    break;
            }
        }
    }
}
