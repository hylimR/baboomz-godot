using System;

namespace Baboomz.Simulation
{
    public static partial class BossLogic
    {
        // Burrows underground and resurfaces.
        // SubState: 0=surfaced, 1=submerging, 2=underground, 3=emerging
        static void UpdateSandWyrm(GameState state, int index, float dt)
        {
            ref PlayerState boss = ref state.Players[index];
            float t = state.Time;

            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) return;

            ref PlayerState target = ref state.Players[targetIdx];

            switch (subState[index])
            {
                case 0: // Surfaced — attack and move
                    // First tick after spawn: stateTimer is 0, so initialize the 5s surface window
                    if (stateTimer[index] == 0f)
                    {
                        stateTimer[index] = t + 5f;
                        attackTimer[index] = t + 1f;
                    }
                    boss.IsInvulnerable = false;
                    boss.FacingDirection = (target.Position.x - boss.Position.x) >= 0f ? 1 : -1;

                    float dx = target.Position.x - boss.Position.x;
                    if (MathF.Abs(dx) > 5f)
                        boss.Velocity.x = MathF.Sign(dx) * boss.MoveSpeed;
                    else
                        boss.Velocity.x = 0f;

                    // Attack: cluster spit every 5s
                    if (t >= attackTimer[index] && boss.ShootCooldownRemaining <= 0f)
                    {
                        float angle = 50f + (float)(rng.NextDouble() * 10.0);
                        float error = (float)(rng.NextDouble() * 8.0 - 4.0);
                        boss.AimAngle = angle + error;
                        var weapon = boss.WeaponSlots[boss.ActiveWeaponSlot];
                        boss.AimPower = Math.Clamp(MathF.Abs(dx) * 0.8f,
                            weapon.MinPower, weapon.MaxPower);
                        GameSimulation.Fire(state, index);
                        boss.AimPower = 0f;
                        attackTimer[index] = t + 5f;
                    }

                    // After 5s surfaced, start submerging
                    if (t >= stateTimer[index])
                    {
                        subState[index] = 1;
                        stateTimer[index] = t + 1f;
                    }
                    break;

                case 1: // Submerging
                    boss.Velocity.x = 0f;
                    boss.Velocity.y = -8f;
                    if (t >= stateTimer[index])
                    {
                        subState[index] = 2;
                        boss.IsInvulnerable = true;
                        stateTimer[index] = t + 3f;
                    }
                    break;

                case 2: // Underground — invulnerable, pick resurface point
                    boss.Velocity = new Vec2(0f, 0f);
                    if (t >= stateTimer[index])
                    {
                        float offsetX = (float)(rng.NextDouble() * 30.0 - 15.0);
                        float halfMap = state.Config.MapWidth / 2f;
                        boss.Position.x = Math.Clamp(target.Position.x + offsetX, -halfMap, halfMap);
                        subState[index] = 3;
                        stateTimer[index] = t + 1f;
                    }
                    break;

                case 3: // Emerging
                    boss.Velocity.y = 8f;
                    if (t >= stateTimer[index])
                    {
                        subState[index] = 0;
                        boss.IsInvulnerable = false;
                        stateTimer[index] = t + 5f;
                        attackTimer[index] = t + 1f;
                    }
                    break;
            }
        }
    }
}
