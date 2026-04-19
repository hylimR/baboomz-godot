using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Gravity Bomb vortex pull guard tests (#329) ---

        [Test]
        public void GravityBomb_DoesNotPullInvulnerablePlayer()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb high above terrain where LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player 1 within pull radius, high above terrain, and make invulnerable
            state.Players[1].Position = new Vec2(4f, 15f);
            state.Players[1].Velocity = Vec2.Zero;
            state.Players[1].IsInvulnerable = true;

            float startX = state.Players[1].Position.x;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            float moved = MathF.Abs(state.Players[1].Position.x - startX);
            Assert.Less(moved, 0.5f,
                "Invulnerable player should not be pulled by gravity bomb vortex");
        }

        [Test]
        public void GravityBomb_DoesNotPullTeammateInTeamMode()
        {
            var config = SmallConfig();
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Put both players on same team
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;

            // Place bomb high above terrain where LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place teammate within pull radius
            state.Players[1].Position = new Vec2(4f, 15f);
            state.Players[1].Velocity = Vec2.Zero;

            float startX = state.Players[1].Position.x;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            float moved = MathF.Abs(state.Players[1].Position.x - startX);
            Assert.Less(moved, 0.5f,
                "Teammate should not be pulled by gravity bomb vortex in team mode");
        }

        [Test]
        public void GravityBomb_PullsEnemyInTeamMode()
        {
            var config = SmallConfig();
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Put players on different teams
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Place bomb high above terrain where LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place enemy within pull radius
            state.Players[1].Position = new Vec2(4f, 15f);
            state.Players[1].Velocity = Vec2.Zero;

            float startX = state.Players[1].Position.x;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[1].Position.x, startX,
                "Enemy on different team should still be pulled by gravity bomb vortex");
        }
    }
}
