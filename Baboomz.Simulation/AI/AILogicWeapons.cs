using System;

namespace Baboomz.Simulation
{
    /// <summary>AI weapon selection logic (partial class of AILogic).</summary>
    public static partial class AILogic
    {
        static void SelectWeapon(GameState state, ref PlayerState ai, ref PlayerState target, int index)
        {
            if (state.Config.MatchType == MatchType.ArmsRace) return;
            if (state.Config.MatchType == MatchType.Roulette) return;

            float dist = Vec2.Distance(ai.Position, target.Position);

            // Issue #88: Use WeaponId lookup instead of hardcoded slot indices
            // so AI correctly selects weapons regardless of loadout order.
            if (TrySelectWeapon(ref ai, "holy_hand_grenade",
                dist < 15f && target.Health < target.MaxHealth * 0.3f, 0.4f)) return;
            if (TrySelectWeapon(ref ai, "airstrike",
                target.Health < target.MaxHealth * 0.4f || dist > 25f, 0.5f)) return;
            if (TrySelectWeapon(ref ai, "shotgun", dist < 5f, 0f)) return;
            if (TrySelectWeapon(ref ai, "rocket", dist > 20f, 0f)) return;
            if (TrySelectWeapon(ref ai, "dynamite", dist > 8f && dist < 18f, 0.6f)) return;
            if (TrySelectWeapon(ref ai, "freeze_grenade",
                dist < 15f && target.FreezeTimer <= 0f, 0.5f)) return;
            if (TrySelectWeapon(ref ai, "napalm", dist > 12f && dist < 25f, 0.7f)) return;
            if (TrySelectWeapon(ref ai, "gravity_bomb", dist > 8f && dist < 18f, 0.6f)) return;
            if (TrySelectDrill(state, ref ai, ref target, dist)) return;
            if (TrySelectWeapon(ref ai, "sheep",
                dist < 12f && MathF.Abs(ai.Position.y - target.Position.y) < 3f, 0.6f)) return;
            if (TrySelectWeapon(ref ai, "banana_bomb", dist > 8f, 0.7f)) return;
            if (TrySelectWeapon(ref ai, "lightning_rod", dist > 5f && dist < 20f, 0.5f)) return;
            if (TrySelectWeapon(ref ai, "sticky_bomb", dist > 6f && dist < 20f, 0.5f)) return;
            if (TrySelectWeapon(ref ai, "boomerang", dist > 5f && dist < 18f, 0.6f)) return;
            if (TrySelectWeapon(ref ai, "ricochet_disc", dist > 6f && dist < 20f, 0.6f)) return;
            if (TrySelectWeapon(ref ai, "magma_ball", dist > 10f && dist < 22f, 0.7f)) return;
            if (TrySelectWeapon(ref ai, "gust_cannon", dist < 12f, 0.6f)) return;

            // Fallback: try cluster, then cannon (slot 0 is always cannon)
            if (!TrySelectWeapon(ref ai, "cluster", true, 0.5f))
                ai.ActiveWeaponSlot = 0;
        }

        static bool TrySelectWeapon(ref PlayerState ai, string weaponId,
            bool condition, float rngThreshold)
        {
            if (!condition) return false;
            for (int s = 0; s < ai.WeaponSlots.Length; s++)
            {
                if (ai.WeaponSlots[s].WeaponId != weaponId) continue;
                if (ai.WeaponSlots[s].Ammo == 0) return false;
                if (ai.WeaponSlots[s].EnergyCost > 0f
                    && ai.Energy < ai.WeaponSlots[s].EnergyCost) return false;
                if (rngThreshold > 0f && rng.NextDouble() < rngThreshold) return false;
                ai.ActiveWeaponSlot = s;
                return true;
            }
            return false;
        }

        static bool TrySelectDrill(GameState state, ref PlayerState ai,
            ref PlayerState target, float dist)
        {
            if (dist < 5f || dist > 20f) return false;
            // Drill is useful when LOS is blocked by terrain
            if (!GamePhysics.RaycastTerrain(state.Terrain,
                ai.Position + new Vec2(0f, 0.5f),
                target.Position + new Vec2(0f, 0.5f), out _)) return false;
            if (rng.NextDouble() < 0.5) return false;
            return TrySelectWeapon(ref ai, "drill", true, 0f);
        }
    }
}
