using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Target Practice mode: spawn targets, detect hits from explosions,
    /// manage scoring/bonuses, respawn targets, and end on timer.
    /// </summary>
    public static class TargetPractice
    {
        public static void Init(GameState state, Random rng)
        {
            var cfg = state.Config;
            state.TargetTimeRemaining = cfg.TargetPracticeRoundDuration;
            state.TargetScore = 0;
            state.TargetConsecutiveHits = 0;
            state.TargetLastHitTime = -10f;
            state.Targets.Clear();

            // Make player solo — infinite ammo, zero energy cost
            ref PlayerState p = ref state.Players[0];
            for (int i = 0; i < p.WeaponSlots.Length; i++)
            {
                if (p.WeaponSlots[i].WeaponId == null) continue;
                p.WeaponSlots[i].Ammo = -1;
                p.WeaponSlots[i].EnergyCost = 0f;
            }

            // Spawn static near targets
            for (int i = 0; i < cfg.TargetStaticNearCount; i++)
                SpawnTarget(state, rng, TargetType.StaticNear);

            // Spawn static mid targets
            for (int i = 0; i < cfg.TargetStaticMidCount; i++)
                SpawnTarget(state, rng, TargetType.StaticMid);

            // Spawn static far target
            for (int i = 0; i < cfg.TargetStaticFarCount; i++)
                SpawnTarget(state, rng, TargetType.StaticFar);

            // Spawn moving targets
            for (int i = 0; i < cfg.TargetMovingHorizontalCount; i++)
                SpawnTarget(state, rng, TargetType.MovingHorizontal);
            for (int i = 0; i < cfg.TargetMovingVerticalCount; i++)
                SpawnTarget(state, rng, TargetType.MovingVertical);
        }

        public static void Update(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.TargetPractice) return;

            state.TargetHitEvents.Clear();
            state.TargetTimeRemaining -= dt;

            if (state.TargetTimeRemaining <= 0f)
            {
                state.Phase = MatchPhase.Ended;
                state.WinnerIndex = 0; // solo mode, player always "wins"
                return;
            }

            UpdateMovingTargets(state, dt);
            CheckExplosionHits(state);
            UpdateRespawnTimers(state, dt);
        }

        static void UpdateMovingTargets(GameState state, float dt)
        {
            var cfg = state.Config;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                if (!t.Active) continue;

                if (t.Type == TargetType.MovingHorizontal)
                {
                    t.MovePhase += dt;
                    float baseX = state.Config.Player1SpawnX;
                    float offsetX = MathF.Sin(t.MovePhase * cfg.TargetMoveSpeedH) * cfg.TargetMoveAmplitude;
                    t.Position = new Vec2(baseX + offsetX, t.Position.y);
                    state.Targets[i] = t;
                }
                else if (t.Type == TargetType.MovingVertical)
                {
                    t.MovePhase += dt;
                    float offsetY = MathF.Abs(MathF.Sin(t.MovePhase * cfg.TargetMoveSpeedV)) * cfg.TargetMoveAmplitude;
                    t.Position = new Vec2(t.Position.x, t.SpawnY + offsetY);
                    state.Targets[i] = t;
                }
            }
        }

        static void CheckExplosionHits(GameState state)
        {
            var cfg = state.Config;

            for (int e = 0; e < state.ExplosionEvents.Count; e++)
            {
                var explosion = state.ExplosionEvents[e];

                for (int i = 0; i < state.Targets.Count; i++)
                {
                    var t = state.Targets[i];
                    if (!t.Active) continue;

                    float dist = Vec2.Distance(explosion.Position, t.Position);
                    if (dist <= explosion.Radius + cfg.TargetRadius)
                    {
                        int points = t.Points;

                        // Streak bonus
                        state.TargetConsecutiveHits++;
                        if (state.TargetConsecutiveHits >= cfg.TargetStreakThreshold)
                            points += cfg.TargetStreakBonus;

                        // Long range bonus
                        float playerDist = Vec2.Distance(state.Players[0].Position, t.Position);
                        if (playerDist >= cfg.TargetLongRangeDistance)
                            points += cfg.TargetLongRangeBonus;

                        // Speed bonus
                        float timeSinceLastHit = state.Time - state.TargetLastHitTime;
                        if (timeSinceLastHit <= cfg.TargetSpeedBonusWindow && state.TargetLastHitTime > 0f)
                            points += cfg.TargetSpeedBonus;

                        state.TargetScore += points;
                        state.TargetLastHitTime = state.Time;

                        state.TargetHitEvents.Add(new TargetHitEvent
                        {
                            TargetIndex = i,
                            Points = points,
                            Position = t.Position
                        });

                        // Deactivate and start respawn timer
                        t.Active = false;
                        t.RespawnTimer = cfg.TargetRespawnTime;
                        state.Targets[i] = t;
                    }
                }
            }
        }

        static void UpdateRespawnTimers(GameState state, float dt)
        {
            var rng = new Random(state.Seed + (int)(state.Time * 100));

            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                if (t.Active) continue;

                t.RespawnTimer -= dt;
                if (t.RespawnTimer <= 0f)
                {
                    Vec2 newPos = PickTargetPosition(state, rng, t.Type);
                    t.Position = newPos;
                    t.Active = true;
                    t.RespawnTimer = 0f;
                    t.MovePhase = (float)rng.NextDouble() * MathF.PI * 2f;
                    t.SpawnY = newPos.y;
                }
                state.Targets[i] = t;
            }
        }

        /// <summary>
        /// Reset streak when player fires and misses (no explosion hit this tick
        /// but an explosion occurred). Called from Tick after CheckExplosionHits.
        /// </summary>
        public static void ResetStreakOnMiss(GameState state)
        {
            if (state.Config.MatchType != MatchType.TargetPractice) return;
            // If there were explosions but no target hits, reset streak
            if (state.ExplosionEvents.Count > 0 && state.TargetHitEvents.Count == 0)
                state.TargetConsecutiveHits = 0;
        }

        static void SpawnTarget(GameState state, Random rng, TargetType type)
        {
            Vec2 pos = PickTargetPosition(state, rng, type);
            int points = GetPoints(state.Config, type);

            state.Targets.Add(new TargetState
            {
                Position = pos,
                Type = type,
                Points = points,
                Active = true,
                RespawnTimer = 0f,
                MovePhase = (float)rng.NextDouble() * MathF.PI * 2f,
                SpawnY = pos.y
            });
        }

        static int GetPoints(GameConfig cfg, TargetType type)
        {
            switch (type)
            {
                case TargetType.StaticNear: return cfg.TargetNearPoints;
                case TargetType.StaticMid: return cfg.TargetMidPoints;
                case TargetType.StaticFar: return cfg.TargetFarPoints;
                case TargetType.MovingHorizontal:
                case TargetType.MovingVertical:
                    return cfg.TargetMovingPoints;
                default: return 50;
            }
        }

        static Vec2 PickTargetPosition(GameState state, Random rng, TargetType type)
        {
            var cfg = state.Config;
            float playerX = cfg.Player1SpawnX;
            float halfMap = cfg.MapWidth / 2f;

            float minDist, maxDist;
            switch (type)
            {
                case TargetType.StaticNear:
                    minDist = 3f;
                    maxDist = cfg.TargetNearMaxDist;
                    break;
                case TargetType.StaticMid:
                    minDist = cfg.TargetNearMaxDist;
                    maxDist = cfg.TargetMidMaxDist;
                    break;
                case TargetType.StaticFar:
                    minDist = cfg.TargetMidMaxDist;
                    maxDist = halfMap - 5f;
                    break;
                case TargetType.MovingHorizontal:
                    minDist = 8f;
                    maxDist = cfg.TargetMidMaxDist;
                    break;
                case TargetType.MovingVertical:
                    minDist = 8f;
                    maxDist = cfg.TargetMidMaxDist;
                    break;
                default:
                    minDist = 5f;
                    maxDist = 15f;
                    break;
            }

            // Pick a random distance in range, random direction
            float dist = minDist + (float)rng.NextDouble() * (maxDist - minDist);
            float sign = rng.Next(2) == 0 ? 1f : -1f;
            float x = playerX + dist * sign;

            // Clamp to map bounds
            float mapMin = -halfMap + 3f;
            float mapMax = halfMap - 3f;
            x = Math.Clamp(x, mapMin, mapMax);

            float y = GamePhysics.FindGroundY(state.Terrain, x, cfg.SpawnProbeY);

            // Moving targets float above ground
            if (type == TargetType.MovingHorizontal || type == TargetType.MovingVertical)
                y += 3f;

            return new Vec2(x, y);
        }
    }
}
