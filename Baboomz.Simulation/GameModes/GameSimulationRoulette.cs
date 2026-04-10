using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Roulette mode: each shot assigns a random weapon from the full roster.
    /// Super weapons (HHG, Airstrike, Banana Bomb) have 10% draw chance.
    /// Players receive +10 energy on each weapon draw.
    /// </summary>
    public static partial class GameSimulation
    {
        static readonly string[] SuperWeapons = { "holy_hand_grenade", "airstrike", "banana_bomb" };
        const float RouletteEnergyRefund = 10f;
        const float SuperWeaponChance = 0.10f;

        public static void InitRoulette(GameState state)
        {
            var rng = new Random(state.Seed);

            for (int i = 0; i < state.Players.Length; i++)
            {
                // Set all weapons to infinite ammo (single-use per draw, replaced after fire)
                for (int w = 0; w < state.Players[i].WeaponSlots.Length; w++)
                    state.Players[i].WeaponSlots[w].Ammo = -1;

                // Assign one random starting weapon
                int startSlot = PickRouletteWeapon(state, rng);
                state.Players[i].ActiveWeaponSlot = startSlot;
            }
        }

        /// <summary>
        /// Called after each shot in Roulette mode. Picks a new random weapon
        /// for the player and grants +10 energy.
        /// </summary>
        public static void OnRouletteShot(GameState state, int playerIndex)
        {
            if (state.Config.MatchType != MatchType.Roulette) return;

            ref PlayerState p = ref state.Players[playerIndex];
            var rng = new Random(state.Seed + (int)(state.Time * 1000) + playerIndex + p.ShotsFired);

            int newSlot = PickRouletteWeapon(state, rng);
            p.ActiveWeaponSlot = newSlot;

            // Energy refund to prevent starvation from consecutive expensive weapons
            p.Energy = Math.Min(p.Energy + RouletteEnergyRefund, p.MaxEnergy);
        }

        static int PickRouletteWeapon(GameState state, Random rng)
        {
            var weapons = state.Config.Weapons;
            int total = weapons.Length;
            if (total == 0) return 0;

            // 10% chance to draw a super weapon
            bool drawSuper = rng.NextDouble() < SuperWeaponChance;

            if (drawSuper)
            {
                // Find a super weapon slot
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    int superIdx = rng.Next(SuperWeapons.Length);
                    for (int w = 0; w < total; w++)
                    {
                        if (weapons[w].WeaponId == SuperWeapons[superIdx])
                            return w;
                    }
                }
            }

            // Normal draw: pick any non-super weapon
            int safetyLimit = 100;
            while (safetyLimit-- > 0)
            {
                int slot = rng.Next(total);
                if (weapons[slot].WeaponId == null) continue;

                // Skip super weapons in normal draw
                bool isSuper = false;
                for (int s = 0; s < SuperWeapons.Length; s++)
                {
                    if (weapons[slot].WeaponId == SuperWeapons[s])
                    {
                        isSuper = true;
                        break;
                    }
                }
                if (!isSuper) return slot;
            }

            return 0; // fallback
        }
    }
}
