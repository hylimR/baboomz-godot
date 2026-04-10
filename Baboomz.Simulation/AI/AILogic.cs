using System;

namespace Baboomz.Simulation
{
    /// <summary>Pure AI decision logic. Mob behaviors in AILogicMobs.cs.</summary>
    public static partial class AILogic
    {
        // Per-AI state tracked outside PlayerState (timers, state machine)
        // Dynamically sized to support any number of players/mobs.
        internal static float[] nextShootTime = new float[16];
        internal static float[] nextMoveTime = new float[16];
        internal static float[] moveEndTime = new float[16];
        internal static float[] moveDirection = new float[16];
        internal static float[] mobRepoTime = new float[16];
        internal static Random rng = new Random(42);

        public static void Reset(int seed, int playerCount = 16)
        {
            rng = new Random(seed);
            int size = Math.Max(playerCount, 16);
            nextShootTime = new float[size];
            nextMoveTime = new float[size];
            moveEndTime = new float[size];
            moveDirection = new float[size];
            mobRepoTime = new float[size];
        }

        public static void Update(GameState state, float dt)
        {
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (!p.IsAI || p.IsDead) continue;

                UpdateAI(state, i, dt);
            }
        }

        static void UpdateAI(GameState state, int index, float dt)
        {
            ref PlayerState ai = ref state.Players[index];
            if (ai.FreezeTimer > 0f) { ai.Velocity = Vec2.Zero; return; } // frozen
            if (ai.IsSwimming) return; // can't act while swimming
            var config = state.Config;
            float t = state.Time;

            // Route boss AI
            if (!string.IsNullOrEmpty(ai.BossType))
            {
                BossLogic.Update(state, index, dt);
                return;
            }

            // Route mob-specific AI behaviors
            if (ai.IsMob && !string.IsNullOrEmpty(ai.MobType))
            {
                switch (ai.MobType)
                {
                    case "bomber": UpdateBomber(state, index, dt); return;
                    case "shielder": UpdateShielder(state, index, dt); return;
                    case "flyer": UpdateFlyer(state, index, dt); return;
                    case "healer": UpdateHealer(state, index, dt); return;
                }
            }

            // Try skills before combat decisions
            TryUseSkills(state, index, dt);

            // Find target
            int targetIdx = FindTarget(state, index);
            if (targetIdx < 0) return;

            ref PlayerState target = ref state.Players[targetIdx];

            // --- Decoy misdirection: aim at hologram position when target is invisible ---
            Vec2 targetPos = target.IsInvisible ? target.DecoyPosition : target.Position;

            // --- Face toward target ---
            Vec2 toTarget = targetPos - ai.Position;
            ai.FacingDirection = toTarget.x >= 0f ? 1 : -1;

            // --- Ballistic aim calculation ---
            float dx = MathF.Abs(toTarget.x);
            float dy = toTarget.y;
            float g = state.Config.Gravity;

            var currentWeapon = ai.WeaponSlots[ai.ActiveWeaponSlot];
            float power = currentWeapon.WeaponId != null
                ? Math.Clamp(dx * 0.9f + MathF.Abs(dy) * 0.5f, currentWeapon.MinPower, currentWeapon.MaxPower)
                : config.DefaultMaxPower;

            float v = power;
            float v2 = v * v;
            float v4 = v2 * v2;
            float discriminant = v4 - g * (g * dx * dx + 2f * dy * v2);

            float angle;
            if (discriminant >= 0f && dx > 0.5f)
            {
                float sqrtD = MathF.Sqrt(discriminant);
                angle = MathF.Atan2(v2 - sqrtD, g * dx) * (180f / MathF.PI);
            }
            else
            {
                angle = 60f;
                power = currentWeapon.WeaponId != null ? currentWeapon.MaxPower : config.DefaultMaxPower;
            }

            float aimError = config.AIAimErrorMargin;
            // Smoke screen: 4x aim error when fire vector passes through smoke
            if (state.SmokeZones != null && SkillSystem.IsLineObscuredBySmoke(state, ai.Position, targetPos))
                aimError *= 4f;
            float error = (float)(rng.NextDouble() * aimError * 2 - aimError);
            float finalAngle = Math.Clamp(angle + error, -90f, 90f);
            ai.AimAngle = finalAngle;

            // --- Movement ---
            if (t >= moveEndTime[index])
            {
                ai.Velocity.x = 0f;
                if (nextMoveTime[index] <= moveEndTime[index])
                {
                    nextMoveTime[index] = moveEndTime[index] + config.AIMoveInterval
                        + (float)(rng.NextDouble() * 2.0);
                }
                if (t >= nextMoveTime[index])
                {
                    moveDirection[index] = rng.NextDouble() > 0.5 ? 1f : -1f;
                    moveEndTime[index] = t + config.AIMoveDuration;
                }
            }
            else
            {
                ai.Velocity.x = moveDirection[index] * ai.MoveSpeed;
                ai.FacingDirection = moveDirection[index] >= 0 ? 1 : -1;
            }

            // Weapon selection (see SelectWeapon in AILogic partial)
            if (rng.NextDouble() < 0.02 * dt * 60.0)
                SelectWeapon(state, ref ai, ref target, index);

            // --- Shooting (blocked while invisible) ---
            if (t >= nextShootTime[index] && ai.ShootCooldownRemaining <= 0f && ai.RetreatTimer <= 0f
                && !ai.IsInvisible)
            {
                var weapon = ai.WeaponSlots[ai.ActiveWeaponSlot];
                if (weapon.WeaponId != null)
                {
                    bool directLOS = !GamePhysics.RaycastTerrain(state.Terrain,
                        ai.Position + new Vec2(0f, 0.5f),
                        targetPos + new Vec2(0f, 0.5f),
                        out _);

                    if (!directLOS)
                    {
                        ai.AimAngle = Math.Clamp(ai.AimAngle + 15f, 40f, 75f);
                        power = Math.Clamp(power * 1.3f, weapon.MinPower, weapon.MaxPower);
                    }

                    ai.AimPower = Math.Clamp(power, weapon.MinPower, weapon.MaxPower);
                    GameSimulation.Fire(state, index);
                    ai.AimPower = 0f;
                }

                nextShootTime[index] = t + config.AIShootInterval +
                    (float)(rng.NextDouble() * config.AIShootIntervalRandomness);

                // AI taunts after shooting (small chance)
                if (rng.NextDouble() < 0.15)
                    GameSimulation.TriggerEmote(state, index, EmoteType.Taunt);
            }

            // AI uses Decoy skill when under fire and has no other defensive skill ready
            if (HasIncomingProjectile(state, index, 4f) && ai.Energy >= 35f)
            {
                TryActivateSkillByType(state, index, SkillType.Decoy);
            }

            // Emote when enemy is low HP
            if (target.Health < target.MaxHealth * 0.25f && ai.EmoteTimer <= 0f
                && rng.NextDouble() < 0.003 * dt * 60.0)
                GameSimulation.TriggerEmote(state, index, EmoteType.Laugh);
        }

        // TryUseSkills, skill activation helpers, PickLoadout in AILogicLoadout.cs (partial class)

        /// <summary>Find first alive non-self player as target.</summary>
        public static int FindTarget(GameState state, int selfIndex)
        {
            int selfTeam = state.Players[selfIndex].TeamIndex;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == selfIndex || state.Players[i].IsDead) continue;
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[i].TeamIndex == selfTeam) continue;
                if (!state.Players[i].IsMob) return i;
            }
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == selfIndex || state.Players[i].IsDead) continue;
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[i].TeamIndex == selfTeam) continue;
                return i;
            }
            return -1;
        }
    }
}
