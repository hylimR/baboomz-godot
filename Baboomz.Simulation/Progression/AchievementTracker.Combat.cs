namespace Baboomz.Simulation
{
    /// <summary>
    /// Combat-related achievement checks: damage events, barrel chains,
    /// hitscan chains, fire zone accumulation, terrain destruction.
    /// </summary>
    public static partial class AchievementTracker
    {
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

                // cm_2: Cannon Master — 3+ cannon hits (use WeaponHits dict, not ActiveWeaponSlot)
                if (state.WeaponHits != null && state.WeaponHits.Length > 0
                    && state.WeaponHits[0].TryGetValue("cannon", out int cannonHits)
                    && cannonHits >= 3)
                    TryUnlock("cm_2", state, 0);

                // mi_1: Self-Destruct — player killed self
                if (dmg.SourceIndex == 0 && dmg.TargetIndex == 0 && state.Players[0].IsDead)
                    TryUnlock("mi_1", state, 0);

                // cm_10: Freezer Burn — target is frozen and hit by fire zone damage
                if (dmg.TargetIndex != 0 && dmg.TargetIndex < state.Players.Length &&
                    state.Players[dmg.TargetIndex].FreezeTimer > 0f)
                {
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

            // cm_3: Chain Reaction — 2+ barrel explosions
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

            // cm_5: Demolition Expert — 500+ terrain pixels destroyed in one tick
            int currentPixels = state.Players[0].TerrainPixelsDestroyed;
            int delta = currentPixels - _lastTerrainPixels;
            _lastTerrainPixels = currentPixels;
            if (delta >= 500)
                TryUnlock("cm_5", state, 0);
        }

        static void CheckBarrelChain(GameState state)
        {
            if (_unlocked.Contains("cm_3")) return;
            if (state.BarrelDetonationsThisTick >= 2)
                TryUnlock("cm_3", state, 0);
        }
    }
}
