using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Buff/multiplier timer management: DoubleDamage, WarCry, Overcharge decay and priority resolution.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
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
                    // crate that expires while Overcharge is armed wipes the 2x multiplier.
                    if (p.OverchargeTimer > 0f)
                        p.DamageMultiplier = 2f;
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
