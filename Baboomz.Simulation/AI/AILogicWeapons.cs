using System;

namespace Baboomz.Simulation
{
    /// <summary>AI weapon selection logic (partial class of AILogic).</summary>
    public static partial class AILogic
    {
        static void SelectWeapon(GameState state, ref PlayerState ai, ref PlayerState target, int index)
        {
            if (state.Config.MatchType == MatchType.ArmsRace) return; // weapon locked by Arms Race
            if (state.Config.MatchType == MatchType.Roulette) return; // weapon locked by Roulette

            float dist = Vec2.Distance(ai.Position, target.Position);

            if (TrySelect(ref ai, 9, dist < 15f && target.Health < target.MaxHealth * 0.3f, 0.4f))
                return; // HHG finishing blow
            if (TrySelect(ref ai, 6, target.Health < target.MaxHealth * 0.4f || dist > 25f, 0.5f))
                return; // airstrike
            if (dist < 5f && ai.WeaponSlots.Length > 1 && ai.WeaponSlots[1].WeaponId != null
                && ai.WeaponSlots[1].Ammo != 0
                && !(ai.WeaponSlots[1].EnergyCost > 0f && ai.Energy < ai.WeaponSlots[1].EnergyCost))
                { ai.ActiveWeaponSlot = 1; return; } // shotgun
            if (TrySelect(ref ai, 2, dist > 20f, 0f))
                return; // rocket
            if (TrySelect(ref ai, 4, dist > 8f && dist < 18f, 0.6f))
                return; // dynamite
            if (TrySelect(ref ai, 12, dist < 15f && target.FreezeTimer <= 0f, 0.5f))
                return; // freeze grenade (CC at close-medium range, skip if already frozen)
            if (TrySelect(ref ai, 5, dist > 12f && dist < 25f, 0.7f))
                return; // napalm
            if (TrySelect(ref ai, 16, dist > 8f && dist < 18f, 0.6f))
                return; // gravity bomb (area denial at medium range)
            if (ai.WeaponSlots.Length > 7 && ai.WeaponSlots[7].WeaponId != null
                && ai.WeaponSlots[7].Ammo != 0
                && !(ai.WeaponSlots[7].EnergyCost > 0f && ai.Energy < ai.WeaponSlots[7].EnergyCost)
                && dist > 5f && dist < 20f
                && GamePhysics.RaycastTerrain(state.Terrain,
                    ai.Position + new Vec2(0f, 0.5f),
                    target.Position + new Vec2(0f, 0.5f), out _)
                && rng.NextDouble() > 0.5)
                { ai.ActiveWeaponSlot = 7; return; } // drill (LOS blocked)
            if (TrySelect(ref ai, 10, dist < 12f && MathF.Abs(ai.Position.y - target.Position.y) < 3f, 0.6f))
                return; // sheep
            if (TrySelect(ref ai, 11, dist > 8f, 0.7f))
                return; // banana bomb
            if (TrySelect(ref ai, 14, dist > 5f && dist < 20f, 0.5f))
                return; // lightning rod (hitscan at medium range)
            if (TrySelect(ref ai, 13, dist > 6f && dist < 20f, 0.5f))
                return; // sticky bomb (precision mid-range)
            if (TrySelect(ref ai, 15, dist > 5f && dist < 18f, 0.6f))
                return; // boomerang (reliable chip damage, infinite ammo)
            if (TrySelect(ref ai, 17, dist > 6f && dist < 20f, 0.6f))
                return; // ricochet disc (bouncing indirect fire, infinite ammo)
            if (TrySelect(ref ai, 18, dist > 10f && dist < 22f, 0.7f))
                return; // magma ball (lava pool area denial)
            if (TrySelect(ref ai, 19, dist < 12f, 0.6f))
                return; // gust cannon (knockback displacement, infinite ammo)

            ai.ActiveWeaponSlot = (ai.WeaponSlots.Length > 3 && ai.WeaponSlots[3].WeaponId != null
                && ai.WeaponSlots[3].Ammo != 0
                && !(ai.WeaponSlots[3].EnergyCost > 0f && ai.Energy < ai.WeaponSlots[3].EnergyCost)
                && rng.NextDouble() > 0.5) ? 3 : 0;
        }

        static bool TrySelect(ref PlayerState ai, int slot, bool condition, float rngThreshold)
        {
            if (!condition) return false;
            if (ai.WeaponSlots.Length <= slot) return false;
            if (ai.WeaponSlots[slot].WeaponId == null || ai.WeaponSlots[slot].Ammo == 0) return false;
            if (ai.WeaponSlots[slot].EnergyCost > 0f && ai.Energy < ai.WeaponSlots[slot].EnergyCost) return false;
            if (rngThreshold > 0f && rng.NextDouble() < rngThreshold) return false;
            ai.ActiveWeaponSlot = slot;
            return true;
        }
    }
}
