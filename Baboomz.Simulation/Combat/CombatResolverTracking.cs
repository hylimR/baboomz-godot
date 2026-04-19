using System;

namespace Baboomz.Simulation
{
    public static partial class CombatResolver
    {
        public static void TrackDamageStats(GameState state, int ownerIndex, int targetIndex,
            float damage, string sourceWeaponId = null)
        {
            if (ownerIndex < 0 || ownerIndex >= state.Players.Length || targetIndex == ownerIndex)
                return;

            ref PlayerState owner = ref state.Players[ownerIndex];
            owner.TotalDamageDealt += damage;
            owner.DirectHits++;
            if (damage > owner.MaxSingleDamage)
                owner.MaxSingleDamage = damage;

            if (state.FirstBloodPlayerIndex < 0)
                state.FirstBloodPlayerIndex = ownerIndex;

            state.Players[targetIndex].LastDamagedByIndex = ownerIndex;
            state.Players[targetIndex].LastDamagedByTimer = 5f;

            GameSimulation.OnArmsRaceDamage(state, ownerIndex, targetIndex);

            TrackWeaponHit(state, ownerIndex, sourceWeaponId);
            TrackWeaponDamage(state, ownerIndex, sourceWeaponId, damage);
            TrackHit(state, ownerIndex);
        }

        public static void TrackHit(GameState state, int ownerIndex)
        {
            ref PlayerState owner = ref state.Players[ownerIndex];
            owner.ConsecutiveHits++;
            owner.LastHitTime = state.Time;

            ComboType? combo = owner.ConsecutiveHits switch
            {
                2 => ComboType.DoubleHit,
                3 => ComboType.TripleHit,
                4 => ComboType.QuadHit,
                >= 5 => ComboType.Unstoppable,
                _ => null
            };

            if (combo.HasValue)
            {
                state.ComboEvents.Add(new ComboEvent
                {
                    PlayerIndex = ownerIndex,
                    Type = combo.Value,
                    Time = state.Time
                });
            }
        }

        public static void TrackKill(GameState state, int ownerIndex)
        {
            ref PlayerState owner = ref state.Players[ownerIndex];

            if (owner.KillsInWindow > 0 && state.Time - owner.LastKillTime > 3f)
                owner.KillsInWindow = 0;

            owner.KillsInWindow++;
            owner.LastKillTime = state.Time;

            ComboType? combo = owner.KillsInWindow switch
            {
                2 => ComboType.DoubleKill,
                >= 3 => ComboType.MultiKill,
                _ => null
            };

            if (combo.HasValue)
            {
                state.ComboEvents.Add(new ComboEvent
                {
                    PlayerIndex = ownerIndex,
                    Type = combo.Value,
                    Time = state.Time
                });
            }
        }

        public static void DecayCombo(GameState state)
        {
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.ConsecutiveHits > 0 && state.Time - p.LastHitTime > 2f)
                    p.ConsecutiveHits = 0;
                if (p.KillsInWindow > 0 && state.Time - p.LastKillTime > 3f)
                    p.KillsInWindow = 0;
            }
        }

        public static void TrackWeaponHit(GameState state, int ownerIndex, string weaponId)
        {
            if (weaponId == null || state.WeaponHits == null || ownerIndex < 0 || ownerIndex >= state.WeaponHits.Length) return;
            var dict = state.WeaponHits[ownerIndex];
            dict.TryGetValue(weaponId, out int count);
            dict[weaponId] = count + 1;
        }

        public static void TrackWeaponKill(GameState state, int ownerIndex, string weaponId)
        {
            if (weaponId == null || state.WeaponKills == null || ownerIndex < 0 || ownerIndex >= state.WeaponKills.Length) return;
            var dict = state.WeaponKills[ownerIndex];
            dict.TryGetValue(weaponId, out int count);
            dict[weaponId] = count + 1;
        }

        public static void TrackWeaponDamage(GameState state, int ownerIndex, string weaponId, float damage)
        {
            if (weaponId == null || state.WeaponDamage == null || ownerIndex < 0 || ownerIndex >= state.WeaponDamage.Length) return;
            var dict = state.WeaponDamage[ownerIndex];
            dict.TryGetValue(weaponId, out float total);
            dict[weaponId] = total + damage;
        }

        internal static bool IsFrontalHit(in PlayerState player, Vec2 explosionPos)
        {
            float dx = explosionPos.x - player.Position.x;
            return (player.FacingDirection >= 0 && dx > 0f) ||
                   (player.FacingDirection < 0 && dx < 0f);
        }
    }
}
