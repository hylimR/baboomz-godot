namespace Baboomz.Simulation
{
    /// <summary>
    /// Arms Race mode: cycle through all weapons, first to score a hit with each wins.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitArmsRace(GameState state)
        {
            state.ArmsRace = new ArmsRaceState
            {
                CurrentWeaponIndex = new int[state.Players.Length],
                DisableCrates = true,
                DisableSuddenDeath = true
            };

            for (int i = 0; i < state.Players.Length; i++)
            {
                state.ArmsRace.CurrentWeaponIndex[i] = 0;

                // Infinite ammo for all weapons in Arms Race
                for (int w = 0; w < state.Players[i].WeaponSlots.Length; w++)
                    state.Players[i].WeaponSlots[w].Ammo = -1;

                // Lock to first weapon
                state.Players[i].ActiveWeaponSlot = 0;
            }
        }

        public static void UpdateArmsRace(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.ArmsRace) return;

            // Force each player's weapon to their current Arms Race weapon
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (state.Players[i].IsDead) continue;
                int weaponIdx = state.ArmsRace.CurrentWeaponIndex[i];
                if (weaponIdx < state.Players[i].WeaponSlots.Length)
                    state.Players[i].ActiveWeaponSlot = weaponIdx;
            }

            // Timer-based end
            if (state.Time >= state.Config.ArmsRaceMaxTime)
                EndArmsRaceByTimer(state);
        }

        /// <summary>
        /// Called when a player deals damage to an opponent. Advances the attacker's
        /// weapon if the damage came from their current Arms Race weapon.
        /// </summary>
        public static void OnArmsRaceDamage(GameState state, int attackerIndex, int targetIndex)
        {
            if (state.Config.MatchType != MatchType.ArmsRace) return;
            if (state.ArmsRace.CurrentWeaponIndex == null) return;
            if (attackerIndex < 0 || attackerIndex >= state.Players.Length) return;
            if (attackerIndex == targetIndex) return; // self-damage does not advance

            int currentWeapon = state.ArmsRace.CurrentWeaponIndex[attackerIndex];
            int totalWeapons = state.Config.Weapons.Length;

            // Already completed all weapons
            if (currentWeapon >= totalWeapons) return;

            // Advance to next weapon
            state.ArmsRace.CurrentWeaponIndex[attackerIndex] = currentWeapon + 1;

            // Check for win — completed the final weapon
            if (currentWeapon + 1 >= totalWeapons)
            {
                state.Phase = MatchPhase.Ended;
                state.WinnerIndex = attackerIndex;
            }
        }

        static void EndArmsRaceByTimer(GameState state)
        {
            state.Phase = MatchPhase.Ended;

            int bestPlayer = -1;
            int bestWeapon = -1;
            float bestDamage = -1f;

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (state.Players[i].IsMob) continue;
                int wi = state.ArmsRace.CurrentWeaponIndex[i];
                float dmg = state.Players[i].TotalDamageDealt;

                if (wi > bestWeapon || (wi == bestWeapon && dmg > bestDamage))
                {
                    bestPlayer = i;
                    bestWeapon = wi;
                    bestDamage = dmg;
                }
            }

            state.WinnerIndex = bestPlayer;
        }
    }
}
