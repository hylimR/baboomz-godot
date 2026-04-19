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

    }
}
