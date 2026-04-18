using System.Collections.Generic;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Checks achievement conditions each tick and emits AchievementEvents.
    /// Pure C# — no Unity dependency. Per-match tracking state is reset via Reset().
    /// Campaign achievements (ca_*) are triggered externally via TryUnlock().
    /// </summary>
    public static partial class AchievementTracker
    {
        // Persistent unlocked set — loaded from save at startup
        static HashSet<string> _unlocked = new HashSet<string>();

        // Per-match tracking counters
        static float _fireDamageTotal;
        static int _warCryKills;
        static bool _playerTookDamage;
        static int _playerShotsFiredAtStart;
        static int _lastTerrainPixels; // for cm_5 per-tick delta detection

        public static IReadOnlyCollection<string> Unlocked => _unlocked;

        public static void LoadUnlocked(IEnumerable<string> ids)
        {
            _unlocked.Clear();
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id))
                    _unlocked.Add(id);
        }

        public static void Reset()
        {
            _fireDamageTotal = 0f;
            _warCryKills = 0;
            _playerTookDamage = false;
            _playerShotsFiredAtStart = 0;
            _lastTerrainPixels = 0;
        }

        public static void OnMatchStart(GameState state)
        {
            Reset();
            _playerShotsFiredAtStart = state.Players[0].ShotsFired;
        }

        public static bool IsUnlocked(string id) => _unlocked.Contains(id);

        /// <summary>
        /// Externally trigger an achievement (e.g., campaign completion).
        /// Returns true if newly unlocked.
        /// </summary>
        public static bool TryUnlock(string id, GameState state, int playerIndex)
        {
            if (_unlocked.Contains(id)) return false;
            _unlocked.Add(id);
            state.AchievementEvents.Add(new AchievementEvent
            {
                AchievementId = id,
                PlayerIndex = playerIndex
            });
            return true;
        }

        /// <summary>
        /// Called each tick after all simulation updates. Scans events for achievement conditions.
        /// </summary>
        public static void Update(GameState state)
        {
            if (state.Phase != MatchPhase.Playing && state.Phase != MatchPhase.Ended) return;

            CheckCombatAchievements(state);
            CheckSkillAchievements(state);
            CheckMatchEndAchievements(state);
        }

        static void CheckSkillAchievements(GameState state)
        {
            for (int i = 0; i < state.SkillEvents.Count; i++)
            {
                var skill = state.SkillEvents[i];
                if (skill.PlayerIndex != 0) continue;

                // sm_3: Earthquake! — 2+ enemies hit
                if (skill.Type == SkillType.Earthquake)
                {
                    int earthquakeHits = 0;
                    for (int d = 0; d < state.DamageEvents.Count; d++)
                    {
                        var dmg = state.DamageEvents[d];
                        if (dmg.SourceIndex == 0 && dmg.TargetIndex != 0 && dmg.Amount > 0f)
                            earthquakeHits++;
                    }
                    if (earthquakeHits >= 2)
                        TryUnlock("sm_3", state, 0);
                }
            }

            // sm_4: Shield Wall — block 100+ damage with Shield
            if (state.Players[0].ShieldDamageBlocked >= 100f)
                TryUnlock("sm_4", state, 0);

            // sm_7: War Machine — kills during WarCry
            if (state.Players[0].WarCryTimer > 0f)
            {
                for (int i = 0; i < state.DamageEvents.Count; i++)
                {
                    var dmg = state.DamageEvents[i];
                    if (dmg.SourceIndex == 0 && dmg.TargetIndex != 0 &&
                        dmg.TargetIndex < state.Players.Length &&
                        state.Players[dmg.TargetIndex].IsDead)
                    {
                        _warCryKills++;
                        if (_warCryKills >= 3)
                            TryUnlock("sm_7", state, 0);
                    }
                }
            }

            // sm_8: Jet Fighter — hit while jetpack active
            if (state.Players[0].SkillSlots != null)
            {
                for (int s = 0; s < state.Players[0].SkillSlots.Length; s++)
                {
                    if (state.Players[0].SkillSlots[s].Type == SkillType.Jetpack &&
                        state.Players[0].SkillSlots[s].IsActive)
                    {
                        for (int d = 0; d < state.DamageEvents.Count; d++)
                        {
                            if (state.DamageEvents[d].SourceIndex == 0 &&
                                state.DamageEvents[d].TargetIndex != 0 &&
                                state.DamageEvents[d].Amount > 0f)
                            {
                                TryUnlock("sm_8", state, 0);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        static void CheckMatchEndAchievements(GameState state)
        {
            if (state.Phase != MatchPhase.Ended) return;
            if (state.WinnerIndex != 0) return; // player must win

            // cm_6: Untouchable — win without taking damage
            if (!_playerTookDamage)
                TryUnlock("cm_6", state, 0);

            // mi_2: Pacifist Round — win with 0 shots fired
            if (state.Players[0].ShotsFired == _playerShotsFiredAtStart)
                TryUnlock("mi_2", state, 0);

            // mi_3: Against All Odds — win at 1 HP
            if (state.Players[0].Health <= 1f && state.Players[0].Health > 0f)
                TryUnlock("mi_3", state, 0);
        }

        /// <summary>
        /// Check if a specific weapon kill happened (called after a kill event).
        /// Used for mi_4 (Sheep) and mi_5 (HHG) detection.
        /// </summary>
        public static void OnPlayerKill(GameState state, int killerIndex, int victimIndex,
            string weaponId)
        {
            if (killerIndex != 0) return;

            // mi_4: Sheep Thrills
            if (weaponId == "sheep")
                TryUnlock("mi_4", state, 0);
        }

        /// <summary>
        /// Called when indestructible terrain is destroyed (HHG).
        /// </summary>
        public static void OnIndestructibleDestroyed(GameState state, int playerIndex)
        {
            if (playerIndex != 0) return;
            TryUnlock("mi_5", state, 0);
        }

        /// <summary>
        /// Returns all unlocked achievement IDs as a comma-separated string for persistence.
        /// </summary>
        public static string GetSaveString()
        {
            return string.Join(",", _unlocked);
        }
    }
}
