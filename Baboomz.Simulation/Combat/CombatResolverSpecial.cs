using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Special explosion types: wind blast (knockback-only) and freeze.
    /// </summary>
    public static partial class CombatResolver
    {
        public static void ApplyWindBlast(GameState state, Vec2 pos, float radius, float knockback, int ownerIndex)
        {
            // No terrain destruction, no explosion VFX — knockback only
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.IsDead) continue;
                if (p.IsInvulnerable) continue;
                if (i == ownerIndex) continue;

                float dist = Vec2.Distance(pos, p.Position);
                if (dist > radius) continue;

                float ratio = radius > 0f ? 1f - Math.Clamp(dist / radius, 0f, 1f) : 1f;

                if (knockback > 0f && dist > 0.01f)
                {
                    Vec2 dir = (p.Position - pos).Normalized;
                    p.Velocity = p.Velocity + dir * (knockback * ratio * state.Config.KnockbackMult);
                }

                if (ownerIndex >= 0 && ownerIndex < state.Players.Length)
                {
                    p.LastDamagedByIndex = ownerIndex;
                    p.LastDamagedByTimer = 5f;
                }

                // Arms Race: gust cannon deals minimum damage so it can advance
                float gustDmg = 0f;
                if (state.Config.MatchType == MatchType.ArmsRace)
                {
                    gustDmg = state.Config.ArmsRaceGustMinDamage;
                    p.Health -= gustDmg;
                    p.TotalDamageTaken += gustDmg;
                }

                state.DamageEvents.Add(new DamageEvent
                {
                    TargetIndex = i,
                    Amount = gustDmg,
                    Position = p.Position,
                    SourceIndex = ownerIndex
                });

                if (gustDmg > 0f && ownerIndex >= 0 && ownerIndex < state.Players.Length)
                    GameSimulation.OnArmsRaceDamage(state, ownerIndex, i);

                // Track as hit for combo system — only when actual damage is dealt
                // (gust cannon deals 0 damage outside ArmsRace, so it should not inflate combos)
                if (gustDmg > 0f && ownerIndex >= 0 && ownerIndex < state.Players.Length)
                {
                    state.Players[ownerIndex].DirectHits++;
                    TrackHit(state, ownerIndex);
                }

                if (gustDmg > 0f && p.Health <= 0f)
                {
                    p.Health = 0f;
                    p.IsDead = true;
                    GameSimulation.ScoreSurvivalKill(state, i);
                    GameSimulation.DropCtfFlag(state, i);
                    GameSimulation.SpawnHeadhunterTokens(state, i);
                    if (ownerIndex >= 0 && ownerIndex < state.Players.Length && i != ownerIndex)
                    {
                        TrackKill(state, ownerIndex);
                        state.Players[ownerIndex].TotalKills++;
                        float killDist = Vec2.Distance(state.Players[ownerIndex].Position, p.Position);
                        if (killDist <= 5f)
                            state.Players[ownerIndex].CloseRangeKills++;
                    }
                }
            }

            // Payload: wind blast pushes payload too
            GameSimulation.ApplyPayloadPush(state, pos, radius, knockback);
        }

        public static void ApplyPierceDamage(GameState state, int targetIndex, float damage,
            float knockback, Vec2 hitPos, int ownerIndex, string sourceWeaponId = null)
        {
            ref PlayerState p = ref state.Players[targetIndex];
            if (p.IsDead || p.IsInvulnerable) return;

            damage *= (1f / MathF.Max(p.ArmorMultiplier, 0.01f));
            if (damage <= 0f) return;

            bool isShatter = p.FreezeTimer > 0f && targetIndex != ownerIndex;
            if (isShatter)
            {
                damage *= state.Config.ShatterMultiplier;
                p.FreezeTimer = 0f;
            }

            // Shield blocks pierce — caller checks ShieldHP to decide whether to stop
            if (p.ShieldHP > 0f && p.MaxShieldHP > 0f)
            {
                bool isFrontal = IsFrontalHit(p, hitPos);
                if (isFrontal)
                {
                    float absorbed = MathF.Min(damage, p.ShieldHP);
                    p.ShieldHP -= absorbed;
                    p.ShieldDamageBlocked += absorbed;
                    damage -= absorbed;

                    if (damage <= 0f)
                    {
                        state.DamageEvents.Add(new DamageEvent
                        {
                            TargetIndex = targetIndex,
                            Amount = 0f,
                            Position = p.Position,
                            SourceIndex = ownerIndex,
                            IsShatter = isShatter
                        });
                        return;
                    }
                }
            }

            p.Health -= damage;
            p.TotalDamageTaken += damage;
            state.DamageEvents.Add(new DamageEvent
            {
                TargetIndex = targetIndex,
                Amount = damage,
                Position = p.Position,
                SourceIndex = ownerIndex,
                IsShatter = isShatter
            });

            if (ownerIndex >= 0 && ownerIndex < state.Players.Length && targetIndex != ownerIndex)
            {
                state.Players[ownerIndex].TotalDamageDealt += damage;
                state.Players[ownerIndex].DirectHits++;
                if (damage > state.Players[ownerIndex].MaxSingleDamage)
                    state.Players[ownerIndex].MaxSingleDamage = damage;
                if (state.FirstBloodPlayerIndex < 0)
                    state.FirstBloodPlayerIndex = ownerIndex;

                TrackWeaponHit(state, ownerIndex, sourceWeaponId);
                TrackWeaponDamage(state, ownerIndex, sourceWeaponId, damage);
                TrackHit(state, ownerIndex);
            }

            if (damage > 0f && ownerIndex >= 0 && ownerIndex < state.Players.Length && targetIndex != ownerIndex)
                GameSimulation.OnArmsRaceDamage(state, ownerIndex, targetIndex);

            // Kill attribution — track who last dealt damage for knockback/fall kills
            if (ownerIndex >= 0 && ownerIndex < state.Players.Length && targetIndex != ownerIndex)
            {
                p.LastDamagedByIndex = ownerIndex;
                p.LastDamagedByTimer = 5f;
            }

            if (knockback > 0f)
            {
                Vec2 dir = (p.Position - hitPos).Normalized;
                p.Velocity = p.Velocity + dir * (knockback * state.Config.KnockbackMult);
            }

            if (p.Health <= 0f)
            {
                p.Health = 0f;
                p.IsDead = true;
                GameSimulation.ScoreSurvivalKill(state, targetIndex);
                GameSimulation.DropCtfFlag(state, targetIndex);
                GameSimulation.SpawnHeadhunterTokens(state, targetIndex);

                if (ownerIndex >= 0 && ownerIndex < state.Players.Length && targetIndex != ownerIndex)
                {
                    TrackKill(state, ownerIndex);
                    TrackWeaponKill(state, ownerIndex, sourceWeaponId);
                    state.Players[ownerIndex].TotalKills++;
                    float killDist = Vec2.Distance(state.Players[ownerIndex].Position, p.Position);
                    if (killDist <= 5f)
                        state.Players[ownerIndex].CloseRangeKills++;
                }
            }
        }

        public static void ApplyFreezeExplosion(GameState state, Vec2 pos, float radius, float freezeDuration, int ownerIndex)
        {
            // No ExplosionEvent here — the follow-up ApplyExplosion call handles it
            int cx = state.Terrain.WorldToPixelX(pos.x);
            int cy = state.Terrain.WorldToPixelY(pos.y);
            int pixelRadius = (int)(radius * state.Terrain.PixelsPerUnit);
            state.Terrain.ClearCircleDestructible(cx, cy, pixelRadius);

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == ownerIndex) continue;
                ref PlayerState p = ref state.Players[i];
                if (p.IsDead || p.IsInvulnerable) continue;
                float dist = Vec2.Distance(pos, p.Position);
                if (dist > radius) continue;
                p.FreezeTimer = freezeDuration;
                p.Velocity = Vec2.Zero;
            }
        }
    }
}
