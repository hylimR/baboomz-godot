using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Payload mode logic: tug-of-war minecart pushed by explosions.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitPayload(GameState state)
        {
            var config = state.Config;
            float centerX = 0f;
            float surfaceY = GamePhysics.FindGroundY(state.Terrain, centerX, config.SpawnProbeY, 0.1f);
            int playerCount = state.Players.Length;

            var lives = new int[playerCount];
            for (int i = 0; i < playerCount; i++)
                lives[i] = config.PayloadLivesPerPlayer;

            state.Payload = new PayloadState
            {
                Position = new Vec2(centerX, surfaceY),
                VelocityX = 0f,
                GoalLeftX = config.Player1SpawnX,
                GoalRightX = config.Player2SpawnX,
                StalemateTimer = 0f,
                Friction = config.PayloadFriction,
                MatchTimer = config.PayloadMatchTime,
                RespawnTimers = new float[playerCount],
                LivesRemaining = lives
            };
        }

        static void UpdatePayload(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.Payload) return;

            ref PayloadState payload = ref state.Payload;
            var config = state.Config;

            // Respawn dead players
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (!p.IsDead) continue;
                if (payload.LivesRemaining[i] == 0) continue;

                if (payload.RespawnTimers[i] <= 0f)
                    payload.RespawnTimers[i] = config.PayloadRespawnDelay;

                payload.RespawnTimers[i] -= dt;
                if (payload.RespawnTimers[i] <= 0f)
                    RespawnPayload(state, i);
            }

            // Apply friction
            float frictionFactor = 1f - payload.Friction * dt;
            if (frictionFactor < 0f) frictionFactor = 0f;
            payload.VelocityX *= frictionFactor;

            // Move payload
            payload.Position.x += payload.VelocityX * dt;

            // Stick to terrain surface
            float surfaceY = GamePhysics.FindGroundY(state.Terrain, payload.Position.x,
                payload.Position.y + 5f, 0.1f);
            payload.Position.y = surfaceY;

            // Stalemate detection: if velocity is near zero for too long, reduce friction
            if (MathF.Abs(payload.VelocityX) < state.Config.PayloadStalemateThreshold)
            {
                payload.StalemateTimer += dt;
                if (payload.StalemateTimer >= state.Config.PayloadStalemateTime)
                {
                    payload.Friction = state.Config.PayloadFriction * 0.5f;
                }
            }
            else
            {
                payload.StalemateTimer = 0f;
            }

            // Match timer countdown
            payload.MatchTimer -= dt;

            // Check goal line crossings
            if (payload.Position.x <= payload.GoalLeftX)
            {
                // Payload reached player 1's side — player 2 wins
                state.Phase = MatchPhase.Ended;
                state.WinnerIndex = state.Players.Length > 1 ? 1 : -1;
                return;
            }

            if (payload.Position.x >= payload.GoalRightX)
            {
                // Payload reached player 2's side — player 1 wins
                state.Phase = MatchPhase.Ended;
                state.WinnerIndex = 0;
                return;
            }

            // Time limit: sudden death (zero friction) then tiebreaker
            if (payload.MatchTimer <= 0f)
            {
                payload.Friction = 0f;

                // If velocity is near zero at time-up, decide by position
                if (MathF.Abs(payload.VelocityX) < 0.5f)
                {
                    state.Phase = MatchPhase.Ended;
                    // Winner = player whose goal the payload is closer to the OPPONENT's side
                    // i.e., payload is at positive X = closer to player 2's goal = player 1 winning
                    float distToLeft = MathF.Abs(payload.Position.x - payload.GoalLeftX);
                    float distToRight = MathF.Abs(payload.Position.x - payload.GoalRightX);
                    if (distToRight < distToLeft)
                        state.WinnerIndex = 0; // payload closer to P2 goal = P1 wins
                    else if (distToLeft < distToRight)
                        state.WinnerIndex = state.Players.Length > 1 ? 1 : -1;
                    else
                        state.WinnerIndex = -1; // exact center = draw
                }
            }
        }

        static void RespawnPayload(GameState state, int playerIndex)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            var config = state.Config;

            float spawnX = playerIndex == 0 ? config.Player1SpawnX : config.Player2SpawnX;
            float spawnY = GamePhysics.FindGroundY(state.Terrain, spawnX, config.SpawnProbeY, 0.5f);

            p.IsDead = false;
            p.Health = config.DefaultMaxHealth;
            p.Energy = config.DefaultMaxEnergy;
            p.Position = new Vec2(spawnX, spawnY + 0.5f);
            p.Velocity = Vec2.Zero;
            p.FreezeTimer = 0f;
            p.RetreatTimer = 0f;
            p.ShootCooldownRemaining = 0f;
            p.IsSwimming = false;
            p.SwimTimer = 0f;

            RestoreWeaponAmmo(ref p, config);

            if (state.Payload.LivesRemaining[playerIndex] > 0)
                state.Payload.LivesRemaining[playerIndex]--;
        }

        /// <summary>
        /// Called by CombatResolver when an explosion occurs. Pushes the payload
        /// if the explosion is within push radius.
        /// </summary>
        public static void ApplyPayloadPush(GameState state, Vec2 explosionPos,
            float explosionRadius, float knockbackForce)
        {
            if (state.Config.MatchType != MatchType.Payload) return;

            ref PayloadState payload = ref state.Payload;
            float pushRadius = explosionRadius * state.Config.PayloadPushRadiusMult;
            float dist = Vec2.Distance(explosionPos, payload.Position);

            if (dist > pushRadius) return;

            float ratio = pushRadius > 0f ? 1f - Math.Clamp(dist / pushRadius, 0f, 1f) : 1f;
            float pushForce = knockbackForce * state.Config.PayloadPushMult * ratio;

            // Push direction: horizontal component only (from explosion to payload)
            float dx = payload.Position.x - explosionPos.x;
            float sign = dx >= 0f ? 1f : -1f;

            payload.VelocityX += sign * pushForce;
        }
    }
}
