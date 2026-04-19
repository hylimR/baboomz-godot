using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Buff/multiplier timer management: DoubleDamage, WarCry, Overcharge decay and priority resolution.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        // Fallback used when the Overcharge skill def is missing or its Value is non-positive.
        // Matches the historic literal (see issue #140) so behavior is unchanged in the default config.
        const float OverchargeMultiplierFallback = 2f;

        /// <summary>
        /// Reads the Overcharge damage multiplier from the skill config so buff-resolution
        /// paths don't drift from GameConfigSkills. Falls back to 2f if the skill def is
        /// missing or Value is non-positive.
        /// </summary>
        static float GetOverchargeMultiplier(GameState state)
        {
            var skills = state.Config.Skills;
            if (skills == null) return OverchargeMultiplierFallback;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i].Type == SkillType.Overcharge)
                    return skills[i].Value > 0f ? skills[i].Value : OverchargeMultiplierFallback;
            }
            return OverchargeMultiplierFallback;
        }

        static void TickBuffTimers(GameState state, ref PlayerState p, float dt)
        {
            // Tick emote timer
            if (p.EmoteTimer > 0f)
            {
                p.EmoteTimer -= dt;
                if (p.EmoteTimer <= 0f) p.ActiveEmote = EmoteType.None;
            }

            // Tick temporary buffs
            if (p.DoubleDamageTimer > 0f)
            {
                p.DoubleDamageTimer -= dt;
                if (p.DoubleDamageTimer <= 0f)
                {
                    p.DoubleDamageTimer = 0f;
                    // Restore higher-priority active buff if any, otherwise reset to default.
                    // Overcharge must be checked here too — otherwise a shorter DoubleDamage
                    // crate that expires while Overcharge is armed wipes its multiplier.
                    if (p.OverchargeTimer > 0f)
                        p.DamageMultiplier = GetOverchargeMultiplier(state);
                    else if (p.WarCryTimer > 0f && p.WarCryDamageBuff > 0f)
                        p.DamageMultiplier = p.WarCryDamageBuff;
                    else
                        p.DamageMultiplier = state.Config.DefaultDamageMultiplier;
                }
            }

            if (p.WarCryTimer > 0f)
            {
                p.WarCryTimer -= dt;
                if (p.WarCryTimer <= 0f)
                {
                    p.WarCryTimer = 0f;
                    // Restore speed
                    if (p.WarCrySpeedBuff > 0f)
                    {
                        p.MoveSpeed /= p.WarCrySpeedBuff;
                        p.WarCrySpeedBuff = 0f;
                    }
                    p.WarCryDamageBuff = 0f;
                    // Only reset damage if DoubleDamage/Overcharge isn't also active
                    if (p.DoubleDamageTimer <= 0f && p.OverchargeTimer <= 0f)
                        p.DamageMultiplier = state.Config.DefaultDamageMultiplier;
                }
            }

            // Sprint: tick speed buff timer (mirrors WarCry speed restoration pattern)
            if (p.SprintTimer > 0f)
            {
                p.SprintTimer -= dt;
                if (p.SprintTimer <= 0f)
                {
                    p.SprintTimer = 0f;
                    if (p.SprintSpeedBuff > 0f)
                    {
                        p.MoveSpeed /= p.SprintSpeedBuff;
                        p.SprintSpeedBuff = 0f;
                    }
                }
            }

            // Overcharge: expires unused if the player doesn't fire in time
            if (p.OverchargeTimer > 0f)
            {
                p.OverchargeTimer -= dt;
                if (p.OverchargeTimer <= 0f)
                {
                    p.OverchargeTimer = 0f;
                    RevertOverchargeMultiplier(state, ref p);
                }
            }
        }

        /// <summary>
        /// Revert damage multiplier after Overcharge ends (via fire consumption or expiry).
        /// Preserves higher-priority buffs that are still active.
        /// </summary>
        internal static void RevertOverchargeMultiplier(GameState state, ref PlayerState p)
        {
            // DoubleDamage crates always apply a 2x multiplier (see CrateSystem), so its
            // literal stays hardcoded. WarCry reads from its own stored buff. Overcharge's
            // own multiplier is read via GetOverchargeMultiplier when relevant.
            if (p.DoubleDamageTimer > 0f)
                p.DamageMultiplier = 2f;
            else if (p.WarCryTimer > 0f && p.WarCryDamageBuff > 0f)
                p.DamageMultiplier = p.WarCryDamageBuff;
            else
                p.DamageMultiplier = state.Config.DefaultDamageMultiplier;
        }

        public static void TriggerEmote(GameState state, int playerIndex, EmoteType emote)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            if (p.IsDead) return;
            if (p.EmoteTimer > 0f) return; // don't interrupt active emote
            p.ActiveEmote = emote;
            p.EmoteTimer = 2f; // show emote for 2 seconds
            state.EmoteEvents.Add(new EmoteEvent { PlayerIndex = playerIndex, Emote = emote });
        }
    }
}
