using System;

namespace Baboomz.Simulation
{
    public static partial class BossLogic
    {
        // Slow walking mech. Close: flamethrower (rapid shots). Far: mortar barrage.
        // Stomp at 50%/25%. Armor at 75%.
        static void UpdateForgeColossus(GameState state, int index, float dt)
        {
            ref PlayerState boss = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) return;

            ref PlayerState target = ref state.Players[targetIdx];
            Vec2 toTarget = target.Position - boss.Position;
            float dist = MathF.Abs(toTarget.x);
            boss.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            // Slow walk toward target
            boss.Velocity.x = boss.FacingDirection * 2f;

            // First-tick initialization (matches SandWyrm pattern — #170)
            if (attackTimer[index] == 0f)
                attackTimer[index] = t + 4f;

            float hpRatio = boss.Health / boss.MaxHealth;

            // Armor phase at 75%: 50% damage reduction for 10s
            if (hpRatio <= 0.75f && boss.BossPhase < 1)
            {
                boss.BossPhase = 1;
                boss.ArmorMultiplier = 2f;
                specialTimer[index] = t + 10f;
            }
            if (boss.BossPhase >= 1 && t >= specialTimer[index])
            {
                boss.ArmorMultiplier = 1f;
            }

            // Stomp at 50% and 25% — boss is briefly invulnerable to avoid self-damage
            if (hpRatio <= 0.5f && boss.BossPhase < 2)
            {
                boss.BossPhase = 2;
                boss.IsInvulnerable = true;
                CombatResolver.ApplyExplosion(state, boss.Position, 6f, 30f, 12f, index, false);
                boss.IsInvulnerable = false;
            }
            if (hpRatio <= 0.25f && boss.BossPhase < 3)
            {
                boss.BossPhase = 3;
                boss.IsInvulnerable = true;
                CombatResolver.ApplyExplosion(state, boss.Position, 6f, 30f, 12f, index, false);
                boss.IsInvulnerable = false;
            }

            // Attack based on distance
            if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
            {
                var weapon = boss.WeaponSlots[boss.ActiveWeaponSlot];

                if (dist < 8f)
                {
                    // Flamethrower: rapid low-damage shots
                    boss.AimAngle = 5f;
                    boss.AimPower = weapon.MinPower;
                    GameSimulation.Fire(state, index);
                    boss.AimPower = 0f;
                    attackTimer[index] = t + 0.5f;
                }
                else
                {
                    // Mortar barrage: 3 high-arc rockets
                    for (int s = 0; s < 3; s++)
                    {
                        float angle = 55f + (float)(rng.NextDouble() * 15.0);
                        float error = (float)(rng.NextDouble() * 6.0 - 3.0);
                        boss.AimAngle = angle + error;
                        float power = Math.Clamp(dist * 0.8f, weapon.MinPower, weapon.MaxPower);
                        boss.AimPower = power;
                        boss.ShootCooldownRemaining = 0f;
                        GameSimulation.Fire(state, index);
                    }
                    boss.AimPower = 0f;
                    attackTimer[index] = t + 8f;
                }
            }
        }
    }
}
