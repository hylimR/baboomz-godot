using System;

namespace Baboomz.Simulation
{
    public static partial class BossLogic
    {
        // Stationary elevated. Ice barrage (5 rapid shots). Frost zones at phase thresholds.
        // Shield phase at 50%: becomes invulnerable for 8s.
        static void UpdateGlacialCannon(GameState state, int index, float dt)
        {
            ref PlayerState boss = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) return;

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - boss.Position;
            boss.FacingDirection = toTarget.x >= 0f ? 1 : -1;
            boss.Velocity.x = 0f;

            // First-tick initialization (matches SandWyrm pattern — #170)
            if (attackTimer[index] == 0f)
                attackTimer[index] = t + 6f;

            float hpRatio = boss.Health / boss.MaxHealth;

            // Phase transitions checked in order: frost (60%) → shield (50%) → frost (30%)
            if (hpRatio <= 0.6f && boss.BossPhase < 1)
            {
                boss.BossPhase = 1;
                SpawnFrostZones(state, target.Position, 4);
            }
            if (hpRatio <= 0.5f && boss.BossPhase < 2)
            {
                boss.BossPhase = 2;
                boss.IsInvulnerable = true;
                specialTimer[index] = t + 8f;
            }
            if (hpRatio <= 0.3f && boss.BossPhase < 3)
            {
                boss.BossPhase = 3;
                SpawnFrostZones(state, target.Position, 4);
            }

            // End shield phase
            if (boss.IsInvulnerable && t >= specialTimer[index])
            {
                boss.IsInvulnerable = false;
            }

            if (boss.IsInvulnerable) return;

            // Ice barrage: 5 shots every 6s
            if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
            {
                float baseAngle = MathF.Atan2(toTarget.y, MathF.Abs(toTarget.x)) * (180f / MathF.PI);
                if (baseAngle < 15f) baseAngle = 35f;

                var weapon = boss.WeaponSlots[boss.ActiveWeaponSlot];
                float basePower = Math.Clamp(MathF.Abs(toTarget.x) * 0.85f,
                    weapon.MinPower, weapon.MaxPower);

                for (int s = 0; s < 5; s++)
                {
                    float spread = (s - 2) * 5f;
                    boss.AimAngle = baseAngle + spread;
                    boss.AimPower = basePower;
                    boss.ShootCooldownRemaining = 0f;
                    GameSimulation.Fire(state, index);
                }
                boss.AimPower = 0f;
                attackTimer[index] = t + 6f;
            }
        }
    }
}
