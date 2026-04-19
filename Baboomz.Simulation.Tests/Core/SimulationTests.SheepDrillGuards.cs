using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void SheepProjectile_SkipsInvulnerableTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[1].IsInvulnerable = true;
            Vec2 target = state.Players[1].Position + new Vec2(0f, 0.5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = target + new Vec2(-0.3f, 0f),
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 5f,
                IsSheep = true
            });

            GameSimulation.Tick(state, 0.016f);

            bool sheepAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
                if (state.Projectiles[i].IsSheep && state.Projectiles[i].Alive) sheepAlive = true;
            Assert.IsTrue(sheepAlive, "Sheep should not despawn on invulnerable target");
        }

        [Test]
        public void SheepProjectile_SkipsTeammate()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;
            Vec2 target = state.Players[1].Position + new Vec2(0f, 0.5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = target + new Vec2(-0.3f, 0f),
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 5f,
                IsSheep = true
            });

            GameSimulation.Tick(state, 0.016f);

            bool sheepAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
                if (state.Projectiles[i].IsSheep && state.Projectiles[i].Alive) sheepAlive = true;
            Assert.IsTrue(sheepAlive, "Sheep should not hit teammate in team mode");
        }

        [Test]
        public void DrillProjectile_SkipsInvulnerableTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[1].IsInvulnerable = true;
            Vec2 target = state.Players[1].Position + new Vec2(0f, 0.5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = target + new Vec2(-0.3f, 0f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 40f,
                KnockbackForce = 8f,
                Alive = true,
                IsDrill = true,
                DrillRange = 30f
            });

            GameSimulation.Tick(state, 0.016f);

            bool drillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
                if (state.Projectiles[i].IsDrill && state.Projectiles[i].Alive) drillAlive = true;
            Assert.IsTrue(drillAlive, "Drill should not despawn on invulnerable target");
        }

        [Test]
        public void DrillProjectile_SkipsTeammate()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;
            Vec2 target = state.Players[1].Position + new Vec2(0f, 0.5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = target + new Vec2(-0.3f, 0f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 40f,
                KnockbackForce = 8f,
                Alive = true,
                IsDrill = true,
                DrillRange = 30f
            });

            GameSimulation.Tick(state, 0.016f);

            bool drillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
                if (state.Projectiles[i].IsDrill && state.Projectiles[i].Alive) drillAlive = true;
            Assert.IsTrue(drillAlive, "Drill should not hit teammate in team mode");
        }
    }
}
