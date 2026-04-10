namespace Baboomz.Simulation
{
    /// <summary>
    /// Weapon loadout management: filtering, AI selection, and validation.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        /// <summary>
        /// Applies a weapon loadout by nulling out any slot whose weapon index is
        /// not in the provided loadout indices. Slots not in the loadout become unavailable.
        /// </summary>
        static void ApplyWeaponLoadout(WeaponSlotState[] slots, int[] loadoutIndices)
        {
            // Build a quick lookup: is each slot index in the loadout?
            for (int i = 0; i < slots.Length; i++)
            {
                bool inLoadout = false;
                for (int j = 0; j < loadoutIndices.Length; j++)
                {
                    if (loadoutIndices[j] == i) { inLoadout = true; break; }
                }
                if (!inLoadout)
                    slots[i].WeaponId = null;
            }
        }

        /// <summary>
        /// Returns the default 4-weapon loadout indices for a player (first 4 non-null weapons).
        /// Used when no explicit loadout is provided.
        /// </summary>
        public static int[] GetDefaultLoadout(GameConfig config)
        {
            var result = new System.Collections.Generic.List<int>(4);
            for (int i = 0; i < config.Weapons.Length && result.Count < 4; i++)
            {
                if (config.Weapons[i].WeaponId != null)
                    result.Add(i);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Validates that a loadout has exactly 4 entries and all are valid weapon indices.
        /// Returns false if invalid.
        /// </summary>
        public static bool ValidateLoadout(int[] loadout, GameConfig config)
        {
            if (loadout == null || loadout.Length != 4) return false;
            for (int i = 0; i < loadout.Length; i++)
            {
                if (loadout[i] < 0 || loadout[i] >= config.Weapons.Length) return false;
                if (config.Weapons[loadout[i]].WeaponId == null) return false;
            }
            return true;
        }
    }
}
