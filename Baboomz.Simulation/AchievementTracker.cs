using System.Collections.Generic;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Checks achievement conditions each tick and emits AchievementEvents.
    /// Pure C# — no Unity dependency. Per-match tracking state is reset via Reset().
    /// Campaign achievements (ca_*) are triggered externally via TryUnlock().
    /// </summary>
    public static class AchievementTracker
    {
        // Persistent unlocked set — loaded from save at startup
        static HashSet<string> _unlocked = new HashSet<string>();

        // Per-match tracking counters
        static int _cannonHits;
        static float _fireDamageTotal;
        static int _warCryKills;
        static bool _playerTookDamage;
        static int _playerShotsFiredAtStart;

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
            _cannonHits = 0;
            _fireDamageTotal = 0f;
            _warCryKills = 0;
            _playerTookDamage = false;
            _playerShotsFiredAtStart = 0;
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

        static void CheckCombatAchievements(GameState state)
        {
            for (int i = 0; i < state.DamageEvents.Count; i++)
            {
                var dmg = state.DamageEvents[i];
                if (dmg.SourceIndex < 0) continue;

                // Track player took damage for cm_6 (from any source)
                if (dmg.TargetIndex == 0 && dmg.Amount > 0f)
                    _playerTookDamage = true;

                bool isPlayer = dmg.SourceIndex == 0;
                if (!isPlayer) continue;

                // cm_1: First Blood
                if (dmg.Amount > 0f)
                    TryUnlock("cm_1", state, 0);

                // cm_7: Overkill — 100+ damage in a single hit
                if (dmg.Amount >= 100f)
                    TryUnlock("cm_7", state, 0);

                // cm_9: Gravity Well — check if target died from fall (tracked elsewhere via knockback+death)
                // This is approximate: if knockback > 0 and target dies at death boundary
                // (Full implementation would need knockback-source tracking; we check death boundary deaths)

                // Track fire zone damage for cm_8
                // (fire zone DamageEvents have SourceIndex = zone owner)

                // Track cannon hits for cm_2
                if (state.Players[0].WeaponSlots.Length > 0 &&
                    state.Players[0].WeaponSlots[state.Players[0].ActiveWeaponSlot].WeaponId == "cannon" &&
                    dmg.TargetIndex != dmg.SourceIndex)
                {
                    _cannonHits++;
                    if (_cannonHits >= 3)
                        TryUnlock("cm_2", state, 0);
                }

                // mi_1: Self-Destruct — player killed self
                if (dmg.SourceIndex == 0 && dmg.TargetIndex == 0 && state.Players[0].IsDead)
                    TryUnlock("mi_1", state, 0);

                // cm_10: Freezer Burn — target is frozen and hit by fire zone damage
                if (dmg.TargetIndex != 0 && dmg.TargetIndex < state.Players.Length &&
                    state.Players[dmg.TargetIndex].FreezeTimer > 0f)
                {
                    // Check if any active fire zone belongs to player 0
                    for (int f = 0; f < state.FireZones.Count; f++)
                    {
                        if (state.FireZones[f].OwnerIndex == 0 && state.FireZones[f].Active)
                        {
                            float dist = Vec2.Distance(state.FireZones[f].Position,
                                state.Players[dmg.TargetIndex].Position);
                            if (dist < state.FireZones[f].Radius)
                            {
                                TryUnlock("cm_10", state, 0);
                                break;
                            }
                        }
                    }
                }
            }

            // cm_3: Chain Reaction — 2+ barrel explosions (barrels chain via CheckBarrels)
            CheckBarrelChain(state);

            // cm_4: Zap Master — hitscan with chain hit
            for (int h = 0; h < state.HitscanEvents.Count; h++)
            {
                var hit = state.HitscanEvents[h];
                if (hit.PrimaryTargetIndex >= 0 && hit.ChainTargetIndex >= 0)
                    TryUnlock("cm_4", state, 0);
            }

            // cm_8: Pyromaniac — accumulate fire zone damage
            for (int i = 0; i < state.DamageEvents.Count; i++)
            {
                var dmg = state.DamageEvents[i];
                if (dmg.SourceIndex != 0) continue;
                // Check if damage came from a fire zone (heuristic: small damage + fire zone exists nearby)
                for (int f = 0; f < state.FireZones.Count; f++)
                {
                    if (state.FireZones[f].OwnerIndex == 0 && state.FireZones[f].Active)
                    {
                        float dist = Vec2.Distance(state.FireZones[f].Position, dmg.Position);
                        if (dist < state.FireZones[f].Radius)
                        {
                            _fireDamageTotal += dmg.Amount;
                            break;
                        }
                    }
                }
            }
            if (_fireDamageTotal >= 200f)
                TryUnlock("cm_8", state, 0);
        }

        static void CheckBarrelChain(GameState state)
        {
            if (_unlocked.Contains("cm_3")) return;
            // Only count barrels that detonated this tick (tracked by CheckBarrels)
            if (state.BarrelDetonationsThisTick >= 2)
                TryUnlock("cm_3", state, 0);
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
