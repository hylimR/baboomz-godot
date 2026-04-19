using System;

namespace Baboomz.Simulation
{
    /// <summary>Environmental and placed-object skill effects (partial class of SkillSystem).</summary>
    public static partial class SkillSystem
    {
        static void ExecuteEarthquake(GameState state, int casterIndex, float damage)
        {
            int casterTeam = state.Players[casterIndex].TeamIndex;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == casterIndex) continue;
                ref PlayerState target = ref state.Players[i];
                if (target.IsDead || target.IsInvulnerable) continue;
                // Skip teammates in team mode
                if (state.Config.TeamMode && casterTeam >= 0 && target.TeamIndex == casterTeam) continue;
                if (!target.IsGrounded) continue; // only affects grounded players

                float applied = damage * state.Players[casterIndex].DamageMultiplier / MathF.Max(target.ArmorMultiplier, 0.01f);
                target.Health -= applied;
                target.TotalDamageTaken += applied;
                target.Velocity.y = 5f; // bounce players up
                target.LastDamagedByIndex = casterIndex;
                target.LastDamagedByTimer = 5f;
                state.DamageEvents.Add(new DamageEvent
                {
                    TargetIndex = i, Amount = applied, Position = target.Position,
                    SourceIndex = casterIndex
                });

                // Track stats
                state.Players[casterIndex].TotalDamageDealt += applied;
                state.Players[casterIndex].DirectHits++;
                if (applied > state.Players[casterIndex].MaxSingleDamage)
                    state.Players[casterIndex].MaxSingleDamage = applied;
                GameSimulation.OnArmsRaceDamage(state, casterIndex, i);
                if (state.FirstBloodPlayerIndex < 0)
                    state.FirstBloodPlayerIndex = casterIndex;

                if (target.Health <= 0f)
                {
                    target.Health = 0f;
                    target.IsDead = true;
                    GameSimulation.ScoreSurvivalKill(state, i);
                    GameSimulation.DropCtfFlag(state, i);
                    GameSimulation.SpawnHeadhunterTokens(state, i);
                    CombatResolver.TrackKill(state, casterIndex);
                    state.Players[casterIndex].TotalKills++;
                    float killDist = Vec2.Distance(state.Players[casterIndex].Position, target.Position);
                    if (killDist <= 5f)
                        state.Players[casterIndex].CloseRangeKills++;
                }
            }
        }

        static void ExecuteSmokeScreen(GameState state, ref PlayerState p, ref SkillSlotState skill)
        {
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            Vec2 target = p.Position + direction * skill.Range;

            // Cap at 2 active smoke zones
            if (state.SmokeZones.Count >= 2)
                state.SmokeZones.RemoveAt(0);

            state.SmokeZones.Add(new SmokeZone
            {
                Position = target,
                Radius = skill.Value > 0f ? skill.Value : 5f,
                RemainingTime = skill.Duration > 0f ? skill.Duration : 4f
            });
        }

        static void UpdateSmokeZones(GameState state, float dt)
        {
            for (int i = state.SmokeZones.Count - 1; i >= 0; i--)
            {
                var zone = state.SmokeZones[i];
                zone.RemainingTime -= dt;
                if (zone.RemainingTime <= 0f)
                    state.SmokeZones.RemoveAt(i);
                else
                    state.SmokeZones[i] = zone;
            }
        }

        /// <summary>Checks if a line segment from origin to target passes through any active smoke zone.</summary>
        public static bool IsLineObscuredBySmoke(GameState state, Vec2 origin, Vec2 target)
        {
            for (int i = 0; i < state.SmokeZones.Count; i++)
            {
                var zone = state.SmokeZones[i];
                if (PointToSegmentDistanceSq(zone.Position, origin, target) < zone.Radius * zone.Radius)
                    return true;
            }
            return false;
        }

        static float PointToSegmentDistanceSq(Vec2 point, Vec2 a, Vec2 b)
        {
            Vec2 ab = b - a;
            float lenSq = ab.x * ab.x + ab.y * ab.y;
            if (lenSq < 0.0001f)
            {
                float dx0 = point.x - a.x, dy0 = point.y - a.y;
                return dx0 * dx0 + dy0 * dy0;
            }
            float t = Math.Clamp(((point.x - a.x) * ab.x + (point.y - a.y) * ab.y) / lenSq, 0f, 1f);
            float px = a.x + t * ab.x - point.x;
            float py = a.y + t * ab.y - point.y;
            return px * px + py * py;
        }

        static void ExecuteGirder(GameState state, ref PlayerState p, ref SkillSlotState skill)
        {
            // Place a horizontal indestructible platform at aim target
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            float range = Math.Min(skill.Range, 12f);
            Vec2 target = p.Position + direction * range;

            float girderWidth = skill.Value > 0f ? skill.Value : 4f;
            float girderHeight = 0.3f; // thin platform

            int px = state.Terrain.WorldToPixelX(target.x - girderWidth / 2f);
            int py = state.Terrain.WorldToPixelY(target.y);
            int pw = (int)(girderWidth * state.Terrain.PixelsPerUnit);
            int ph = Math.Max(1, (int)(girderHeight * state.Terrain.PixelsPerUnit));

            state.Terrain.FillRectIndestructible(px, py, pw, ph);
        }

        /// <summary>
        /// Mend skill: refill destroyed terrain pixels in a radius around the aim target.
        /// Only fills currently-empty pixels (skips solid + indestructible), and avoids
        /// overlapping with living players so we never crush anyone into fresh earth.
        /// </summary>
        static void ExecuteMend(GameState state, ref PlayerState p, ref SkillSlotState skill)
        {
            float rad = p.AimAngle * MathF.PI / 180f;
            Vec2 direction = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad));
            float range = Math.Min(skill.Range, 12f);
            Vec2 target = p.Position + direction * range;

            float radiusWorld = skill.Value > 0f ? skill.Value : 3f;
            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y);
            int radiusPx = Math.Max(1, (int)(radiusWorld * state.Terrain.PixelsPerUnit));

            // Build per-player bounding boxes (pixel space) to skip pixels under living players
            const float playerHalfWidth = 0.5f;
            const float playerHalfHeight = 1.0f;
            int playerCount = state.Players.Length;
            var playerMinX = new int[playerCount];
            var playerMaxX = new int[playerCount];
            var playerMinY = new int[playerCount];
            var playerMaxY = new int[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                if (state.Players[i].IsDead) { playerMinX[i] = 1; playerMaxX[i] = 0; continue; }
                ref PlayerState other = ref state.Players[i];
                playerMinX[i] = state.Terrain.WorldToPixelX(other.Position.x - playerHalfWidth);
                playerMaxX[i] = state.Terrain.WorldToPixelX(other.Position.x + playerHalfWidth);
                playerMinY[i] = state.Terrain.WorldToPixelY(other.Position.y - 0.1f);
                playerMaxY[i] = state.Terrain.WorldToPixelY(other.Position.y + playerHalfHeight * 2f);
            }

            int minX = Math.Max(0, cx - radiusPx);
            int maxX = Math.Min(state.Terrain.Width - 1, cx + radiusPx);
            int minY = Math.Max(0, cy - radiusPx);
            int maxY = Math.Min(state.Terrain.Height - 1, cy + radiusPx);
            int rSq = radiusPx * radiusPx;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy > rSq) continue;

                    int idx = (y * state.Terrain.Width + x) * 4;
                    if (state.Terrain.Pixels[idx + 3] != 0) continue; // already solid

                    // Skip pixels overlapping any living player's bounding box
                    bool underPlayer = false;
                    for (int pi = 0; pi < playerCount; pi++)
                    {
                        if (x >= playerMinX[pi] && x <= playerMaxX[pi]
                            && y >= playerMinY[pi] && y <= playerMaxY[pi])
                        {
                            underPlayer = true;
                            break;
                        }
                    }
                    if (underPlayer) continue;

                    state.Terrain.Pixels[idx] = 0;
                    state.Terrain.Pixels[idx + 1] = 255; // G (earth)
                    state.Terrain.Pixels[idx + 2] = 0;
                    state.Terrain.Pixels[idx + 3] = 255; // A (solid)
                }
            }
        }

    }
}
