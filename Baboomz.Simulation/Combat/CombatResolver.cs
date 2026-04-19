using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Handles explosion effects: terrain destruction, damage falloff,
    /// knockback, stat tracking, and death checks.
    /// </summary>
    public static partial class CombatResolver
    {
        public static void ApplyExplosion(GameState state, Vec2 pos, float radius, float maxDamage,
            float knockback, int ownerIndex, bool destroyIndestructible, string sourceWeaponId = null)
        {
            state.ExplosionEvents.Add(new ExplosionEvent { Position = pos, Radius = radius, OwnerIndex = ownerIndex });

            // Destroy terrain (biome modifier scales crater size)
            int cx = state.Terrain.WorldToPixelX(pos.x);
            int cy = state.Terrain.WorldToPixelY(pos.y);
            int pixelRadius = (int)(radius * state.Terrain.PixelsPerUnit * state.Config.TerrainDestructionMult);

            if (destroyIndestructible)
                state.Terrain.ClearCircle(cx, cy, pixelRadius);
            else
                state.Terrain.ClearCircleDestructible(cx, cy, pixelRadius);

            // Mark scorched edges around crater (2-pixel wide ring)
            state.Terrain.MarkDestructionEdge(cx, cy, pixelRadius, 3);

            // Track terrain destruction for challenge stats
            if (ownerIndex >= 0 && ownerIndex < state.Players.Length)
                state.Players[ownerIndex].TerrainPixelsDestroyed += (int)(MathF.PI * pixelRadius * pixelRadius);

            // Damage players
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.IsDead) continue;

                // Boss invulnerability — skip damage entirely
                if (p.IsInvulnerable) continue;

                float dist = Vec2.Distance(pos, p.Position);
                if (dist > radius) continue;

                float dmgRatio = radius > 0f ? 1f - Math.Clamp(dist / radius, 0f, 1f) : 1f;
                float damage = maxDamage * dmgRatio;
                damage *= (1f / MathF.Max(p.ArmorMultiplier, 0.01f));
                if (damage <= 0f) continue;

                // Freeze Shatter — bonus damage to frozen/petrified targets (not self-damage)
                bool isShatter = p.FreezeTimer > 0f && i != ownerIndex;
                if (isShatter)
                {
                    damage *= state.Config.ShatterMultiplier;
                    p.FreezeTimer = 0f;
                }

                // Shield absorption — Shielders absorb frontal damage with shield
                if (p.ShieldHP > 0f && p.MaxShieldHP > 0f)
                {
                    bool isFrontal = IsFrontalHit(p, pos);
                    if (isFrontal)
                    {
                        float absorbed = MathF.Min(damage, p.ShieldHP);
                        p.ShieldHP -= absorbed;
                        damage -= absorbed;
                        p.ShieldDamageBlocked += absorbed;

                        if (damage <= 0f)
                        {
                            state.DamageEvents.Add(new DamageEvent
                            {
                                TargetIndex = i,
                                Amount = 0f,
                                Position = p.Position,
                                SourceIndex = ownerIndex,
                                IsShatter = isShatter
                            });
                            continue;
                        }
                    }
                }

                p.Health -= damage;
                p.TotalDamageTaken += damage;
                state.DamageEvents.Add(new DamageEvent
                {
                    TargetIndex = i,
                    Amount = damage,
                    Position = p.Position,
                    SourceIndex = ownerIndex,
                    IsShatter = isShatter
                });

                // Track stats
                if (ownerIndex >= 0 && ownerIndex < state.Players.Length && i != ownerIndex)
                {
                    state.Players[ownerIndex].TotalDamageDealt += damage;
                    state.Players[ownerIndex].DirectHits++;
                    if (damage > state.Players[ownerIndex].MaxSingleDamage)
                        state.Players[ownerIndex].MaxSingleDamage = damage;
                    if (state.FirstBloodPlayerIndex < 0)
                        state.FirstBloodPlayerIndex = ownerIndex;

                    // Weapon mastery tracking
                    TrackWeaponHit(state, ownerIndex, sourceWeaponId);
                    TrackWeaponDamage(state, ownerIndex, sourceWeaponId, damage);

                    // Jetpack Ace challenge: hit while jetpacking
                    if (!state.Players[ownerIndex].HitWhileJetpacking)
                    {
                        var ownerSkills = state.Players[ownerIndex].SkillSlots;
                        if (ownerSkills != null)
                            for (int s = 0; s < ownerSkills.Length; s++)
                                if (ownerSkills[s].IsActive && ownerSkills[s].Type == SkillType.Jetpack)
                                { state.Players[ownerIndex].HitWhileJetpacking = true; break; }
                    }

                    // Freeze Tag challenge: hit a frozen enemy
                    if (isShatter)
                        state.Players[ownerIndex].FreezeToHitCombo = true;

                    // Combo tracking — increment consecutive hits
                    TrackHit(state, ownerIndex);
                }

                // Arms Race: advance weapon on dealing damage to opponent
                if (damage > 0f && ownerIndex >= 0 && ownerIndex < state.Players.Length && i != ownerIndex)
                    GameSimulation.OnArmsRaceDamage(state, ownerIndex, i);

                if (ownerIndex >= 0 && ownerIndex < state.Players.Length && i != ownerIndex)
                {
                    p.LastDamagedByIndex = ownerIndex;
                    p.LastDamagedByTimer = 5f;
                }

                // Knockback (biome modifier scales force)
                if (knockback > 0f && dist > 0.01f)
                {
                    Vec2 dir = (p.Position - pos).Normalized;
                    p.Velocity = p.Velocity + dir * (knockback * dmgRatio * state.Config.KnockbackMult);
                }

                if (p.Health <= 0f)
                {
                    p.Health = 0f;
                    p.IsDead = true;
                    GameSimulation.ScoreSurvivalKill(state, i);
                    GameSimulation.DropCtfFlag(state, i);
                    GameSimulation.SpawnHeadhunterTokens(state, i);

                    // Direct hit bonus in Survival: applies when explosion center is
                    // very close to the target (dmgRatio >= 0.75 means dist <= 25% of radius)
                    if (state.Config.MatchType == MatchType.Survival && p.IsMob
                        && dmgRatio >= 0.75f && state.Config.SurvivalScoreDirectHitBonus > 0)
                    {
                        state.Survival.Score += state.Config.SurvivalScoreDirectHitBonus;
                    }

                    if (ownerIndex >= 0 && ownerIndex < state.Players.Length && i != ownerIndex)
                    {
                        TrackKill(state, ownerIndex);
                        TrackWeaponKill(state, ownerIndex, sourceWeaponId);

                        // Track kill distance for Close Quarters challenge
                        state.Players[ownerIndex].TotalKills++;
                        float killDist = Vec2.Distance(state.Players[ownerIndex].Position, p.Position);
                        if (killDist <= 5f)
                            state.Players[ownerIndex].CloseRangeKills++;
                    }
                }
            }

            // Demolition: damage crystals from explosions
            GameSimulation.ApplyCrystalExplosionDamage(state, pos, radius, maxDamage);

            // Payload: push the minecart from explosions
            GameSimulation.ApplyPayloadPush(state, pos, radius, knockback);
        }

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

            // Reset kill window if gap exceeds 3s
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

        /// <summary>
        /// Time-based combo decay: resets hit streak after 2s of no hits,
        /// kill streak after 3s of no kills.
        /// </summary>
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
            // Frontal = explosion is in front of the player (same side as facing)
            // dx == 0 (overhead/below) is NOT frontal — a horizontal shield can't block vertical hits.
            return (player.FacingDirection >= 0 && dx > 0f) ||
                   (player.FacingDirection < 0 && dx < 0f);
        }
    }
}
