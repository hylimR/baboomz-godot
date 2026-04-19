using System;

namespace Baboomz.Simulation
{
    /// <summary>Hitscan weapon fire logic (partial class of GameSimulation).</summary>
    public static partial class GameSimulation
    {
        static void FireHitscan(GameState state, int playerIndex, ref PlayerState p,
            WeaponSlotState weapon, float power)
        {
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            Vec2 origin = p.Position + new Vec2(0f, 0.5f);
            float maxRange = power * 3f; // power affects range
            Vec2 target = origin + direction * maxRange;

            // Check terrain hit
            bool terrainHit = GamePhysics.RaycastTerrain(state.Terrain, origin, target, out Vec2 terrainPoint);
            float terrainDist = terrainHit ? Vec2.Distance(origin, terrainPoint) : maxRange;

            // Check player hits along the ray
            int primaryTarget = -1;
            float primaryDist = terrainDist;
            Vec2 primaryHitPoint = terrainHit ? terrainPoint : target;

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == playerIndex) continue;
                ref PlayerState t = ref state.Players[i];
                if (t.IsDead || t.IsInvulnerable) continue;

                Vec2 playerCenter = t.Position + new Vec2(0f, 0.5f);
                float hitRadius = 0.6f;

                // Point-to-line distance for ray hit detection
                Vec2 toPlayer = playerCenter - origin;
                float proj = toPlayer.x * direction.x + toPlayer.y * direction.y;
                if (proj < 0f || proj > primaryDist) continue;

                Vec2 closest = origin + direction * proj;
                float dist = Vec2.Distance(closest, playerCenter);
                if (dist < hitRadius && proj < primaryDist)
                {
                    primaryTarget = i;
                    primaryDist = proj;
                    primaryHitPoint = playerCenter;
                }
            }

            float damage = weapon.MaxDamage * p.DamageMultiplier;

            // Apply primary damage
            if (primaryTarget >= 0)
            {
                ref PlayerState pt = ref state.Players[primaryTarget];
                float applied = damage * (1f / MathF.Max(pt.ArmorMultiplier, 0.01f));

                // Freeze Shatter — bonus damage to frozen/petrified targets
                bool isShatter = pt.FreezeTimer > 0f && primaryTarget != playerIndex;
                if (isShatter)
                {
                    applied *= state.Config.ShatterMultiplier;
                    pt.FreezeTimer = 0f;
                }

                // Shield absorption (frontal hit check)
                if (pt.ShieldHP > 0f && pt.MaxShieldHP > 0f)
                {
                    if (CombatResolver.IsFrontalHit(pt, origin))
                    {
                        float absorbed = MathF.Min(applied, pt.ShieldHP);
                        pt.ShieldHP -= absorbed;
                        applied -= absorbed;
                        pt.ShieldDamageBlocked += absorbed;
                    }
                }

                pt.Health -= applied;
                pt.TotalDamageTaken += applied;
                state.DamageEvents.Add(new DamageEvent
                {
                    TargetIndex = primaryTarget, Amount = applied, Position = pt.Position,
                    SourceIndex = playerIndex, IsShatter = isShatter
                });
                if (applied > 0f)
                {
                    if (state.FirstBloodPlayerIndex < 0)
                        state.FirstBloodPlayerIndex = playerIndex;
                    p.DirectHits++;
                    p.TotalDamageDealt += applied;
                    if (applied > p.MaxSingleDamage) p.MaxSingleDamage = applied;
                    CombatResolver.TrackHit(state, playerIndex);
                    CombatResolver.TrackWeaponHit(state, playerIndex, weapon.WeaponId);
                    CombatResolver.TrackWeaponDamage(state, playerIndex, weapon.WeaponId, applied);

                    // Freeze Tag challenge: hit a frozen enemy
                    if (isShatter)
                        p.FreezeToHitCombo = true;
                }

                // Kill attribution — track who last dealt damage for knockback/fall kills
                pt.LastDamagedByIndex = playerIndex;
                pt.LastDamagedByTimer = 5f;

                if (pt.Health <= 0f)
                {
                    pt.Health = 0f; pt.IsDead = true; ScoreSurvivalKill(state, primaryTarget);
                    DropCtfFlag(state, primaryTarget);
                    SpawnHeadhunterTokens(state, primaryTarget);
                    CombatResolver.TrackKill(state, playerIndex);
                    CombatResolver.TrackWeaponKill(state, playerIndex, weapon.WeaponId);
                    state.Players[playerIndex].TotalKills++;
                    float killDist = Vec2.Distance(p.Position, pt.Position);
                    if (killDist <= 5f)
                        state.Players[playerIndex].CloseRangeKills++;
                }
            }

            // Chain to secondary target
            int chainTarget = -1;
            Vec2 chainHitPoint = Vec2.Zero;
            if (primaryTarget >= 0 && weapon.ChainRange > 0f && weapon.ChainDamage > 0f)
            {
                float closestChainDist = weapon.ChainRange;
                for (int i = 0; i < state.Players.Length; i++)
                {
                    if (i == playerIndex || i == primaryTarget) continue;
                    ref PlayerState ct = ref state.Players[i];
                    if (ct.IsDead || ct.IsInvulnerable) continue;

                    Vec2 ctCenter = ct.Position + new Vec2(0f, 0.5f);
                    if (GamePhysics.RaycastTerrain(state.Terrain, primaryHitPoint, ctCenter, out _))
                        continue;
                    float dist = Vec2.Distance(primaryHitPoint, ctCenter);
                    if (dist < closestChainDist)
                    {
                        closestChainDist = dist;
                        chainTarget = i;
                        chainHitPoint = ctCenter;
                    }
                }

                if (chainTarget >= 0)
                {
                    // Track chain lightning targets for challenge (primary + chain = 2)
                    int chainCount = 2;
                    if (chainCount > p.ChainLightningTargets)
                        p.ChainLightningTargets = chainCount;
                    ref PlayerState ct2 = ref state.Players[chainTarget];
                    float chainApplied = weapon.ChainDamage * p.DamageMultiplier * (1f / MathF.Max(ct2.ArmorMultiplier, 0.01f));

                    // Freeze Shatter — bonus damage to frozen/petrified chain target
                    bool chainShatter = ct2.FreezeTimer > 0f && chainTarget != playerIndex;
                    if (chainShatter)
                    {
                        chainApplied *= state.Config.ShatterMultiplier;
                        ct2.FreezeTimer = 0f;
                    }

                    // Shield absorption for chain target
                    if (ct2.ShieldHP > 0f && ct2.MaxShieldHP > 0f)
                    {
                        if (CombatResolver.IsFrontalHit(ct2, primaryHitPoint))
                        {
                            float absorbed2 = MathF.Min(chainApplied, ct2.ShieldHP);
                            ct2.ShieldHP -= absorbed2;
                            chainApplied -= absorbed2;
                            ct2.ShieldDamageBlocked += absorbed2;
                        }
                    }

                    ct2.Health -= chainApplied;
                    ct2.TotalDamageTaken += chainApplied;
                    state.DamageEvents.Add(new DamageEvent
                    {
                        TargetIndex = chainTarget, Amount = chainApplied, Position = ct2.Position,
                        SourceIndex = playerIndex, IsShatter = chainShatter
                    });
                    if (chainApplied > 0f)
                    {
                        if (state.FirstBloodPlayerIndex < 0)
                            state.FirstBloodPlayerIndex = playerIndex;
                        p.DirectHits++;
                        p.TotalDamageDealt += chainApplied;
                        if (chainApplied > p.MaxSingleDamage) p.MaxSingleDamage = chainApplied;
                        CombatResolver.TrackHit(state, playerIndex);
                        CombatResolver.TrackWeaponHit(state, playerIndex, weapon.WeaponId);
                        CombatResolver.TrackWeaponDamage(state, playerIndex, weapon.WeaponId, chainApplied);

                        if (chainShatter)
                            p.FreezeToHitCombo = true;
                    }

                    // Kill attribution for chain target
                    ct2.LastDamagedByIndex = playerIndex;
                    ct2.LastDamagedByTimer = 5f;

                    if (ct2.Health <= 0f)
                    {
                        ct2.Health = 0f; ct2.IsDead = true; ScoreSurvivalKill(state, chainTarget);
                        DropCtfFlag(state, chainTarget);
                        SpawnHeadhunterTokens(state, chainTarget);
                        CombatResolver.TrackKill(state, playerIndex);
                        CombatResolver.TrackWeaponKill(state, playerIndex, weapon.WeaponId);
                        state.Players[playerIndex].TotalKills++;
                        float chainKillDist = Vec2.Distance(p.Position, ct2.Position);
                        if (chainKillDist <= 5f)
                            state.Players[playerIndex].CloseRangeKills++;
                    }
                }
            }

            state.HitscanEvents.Add(new HitscanEvent
            {
                Origin = origin,
                HitPoint = primaryHitPoint,
                PrimaryTargetIndex = primaryTarget,
                ChainTargetIndex = chainTarget,
                ChainHitPoint = chainHitPoint
            });
        }
    }
}
