using System;

namespace Baboomz.Simulation
{
    public static partial class BossLogic
    {
        // Stationary turret boss. Phase 1: single shot every 4s.
        // Phase 2 (50% HP): adds 3-shot burst every 8s.
        static void UpdateIronSentinel(GameState state, int index, float dt)
        {
            ref PlayerState boss = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) return;

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - boss.Position;
            boss.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            // Stationary
            boss.Velocity.x = 0f;

            // Check phase transition
            float hpRatio = boss.Health / boss.MaxHealth;
            if (hpRatio <= 0.5f && boss.BossPhase == 0)
            {
                boss.BossPhase = 1;
                specialTimer[index] = t + 2f;
            }

            // Aim at target with moderate error
            float dx = MathF.Abs(toTarget.x);
            float dy = toTarget.y;
            float angle = MathF.Atan2(dy, dx) * (180f / MathF.PI);
            if (angle < 10f) angle = 30f;
            float error = (float)(rng.NextDouble() * 24.0 - 12.0);
            boss.AimAngle = Math.Clamp(angle + error, -90f, 90f);

            var weapon = boss.WeaponSlots[boss.ActiveWeaponSlot];
            float power = Math.Clamp(dx * 0.85f, weapon.MinPower, weapon.MaxPower);
            boss.AimPower = power;

            // Attack 1: single shot every 4s
            if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
            {
                GameSimulation.Fire(state, index);
                boss.AimPower = 0f;
                attackTimer[index] = t + 4f;
            }

            // Attack 2 (Phase 2): 3-shot burst every 8s
            if (boss.BossPhase >= 1 && t >= specialTimer[index])
            {
                float savedAngle = boss.AimAngle;
                for (int s = 0; s < 3; s++)
                {
                    boss.AimAngle = savedAngle + (s - 1) * 7f;
                    boss.AimPower = power * 0.9f;
                    boss.ShootCooldownRemaining = 0f;
                    GameSimulation.Fire(state, index);
                }
                boss.AimPower = 0f;
                specialTimer[index] = t + 8f;
            }
        }
    }
}
