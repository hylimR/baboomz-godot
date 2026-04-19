using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class AISkillActivationTests
    {
        static GameConfig SmallConfig()
        {
            return new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f,
                Player1SpawnX = -10f,
                Player2SpawnX = 10f,
                SpawnProbeY = 20f,
                DeathBoundaryY = -25f,
                MineCount = 0,
                BarrelCount = 0
            };
        }

        static SkillSlotState MakeSkill(string id, SkillType type, float range = 15f, float value = 5f)
        {
            return new SkillSlotState
            {
                SkillId = id, Type = type,
                EnergyCost = 0f, Cooldown = 0f, Range = range, Value = value, Duration = 3f
            };
        }

        static bool AIActivatesSkill(SkillType type, System.Func<GameState, bool> detector,
            System.Action<GameState> setup = null)
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            state.Players[1].IsAI = true;
            state.Players[1].SkillSlots = new[]
            {
                MakeSkill(type.ToString().ToLower(), type),
                new SkillSlotState { SkillId = null }
            };
            state.Players[1].Energy = 100f;
            state.Players[0].Position = new Vec2(5f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);

            setup?.Invoke(state);

            for (int i = 0; i < 12000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (detector(state)) return true;
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }
            return false;
        }

        [Test]
        public void AI_ActivatesDeflect_WhenProjectileIncoming()
        {
            bool activated = AIActivatesSkill(SkillType.Deflect,
                s => s.Players[1].DeflectTimer > 0f,
                s =>
                {
                    s.Projectiles.Add(new ProjectileState
                    {
                        Id = s.NextProjectileId++,
                        Position = new Vec2(10f, 5f),
                        Velocity = new Vec2(5f, 0f),
                        OwnerIndex = 0, Alive = true,
                        ExplosionRadius = 2f, MaxDamage = 25f
                    });
                });
            Assert.IsTrue(activated, "AI should activate Deflect when projectile is nearby");
        }

        [Test]
        public void AI_ActivatesWarCry_WhenEnemyInRange()
        {
            bool activated = AIActivatesSkill(SkillType.WarCry,
                s => s.Players[1].WarCryTimer > 0f);
            Assert.IsTrue(activated, "AI should activate WarCry when enemy is in range");
        }

        [Test]
        public void AI_ActivatesEnergyDrain_WhenEnemyInRange()
        {
            bool activated = AIActivatesSkill(SkillType.EnergyDrain,
                s => s.SkillEvents.Count > 0,
                s => s.Players[0].Energy = 100f);
            Assert.IsTrue(activated, "AI should activate EnergyDrain when enemy is nearby");
        }

        [Test]
        public void AI_ActivatesSmoke_WhenLowHPAndEnemyNear()
        {
            bool activated = AIActivatesSkill(SkillType.SmokeScreen,
                s => s.SmokeZones.Count > 0,
                s =>
                {
                    s.Players[1].Health = 30f;
                    s.Players[1].MaxHealth = 100f;
                });
            Assert.IsTrue(activated, "AI should activate Smoke when low HP and enemy nearby");
        }

        [Test]
        public void AI_ActivatesMineLay_WhenEnemyInRange()
        {
            bool activated = AIActivatesSkill(SkillType.MineLay,
                s => s.Mines.Count > 0);
            Assert.IsTrue(activated, "AI should place a mine when enemy is in range");
        }

        [Test]
        public void AI_ActivatesDecoy_WhenLowHPAndProjectileIncoming()
        {
            bool activated = AIActivatesSkill(SkillType.Decoy,
                s => s.Players[1].IsInvisible,
                s =>
                {
                    s.Players[1].Health = 30f;
                    s.Players[1].MaxHealth = 100f;
                    s.Projectiles.Add(new ProjectileState
                    {
                        Id = s.NextProjectileId++,
                        Position = new Vec2(10f, 5f),
                        Velocity = new Vec2(5f, 0f),
                        OwnerIndex = 0, Alive = true,
                        ExplosionRadius = 2f, MaxDamage = 25f
                    });
                });
            Assert.IsTrue(activated, "AI should activate Decoy when low HP and projectile incoming");
        }
    }
}
