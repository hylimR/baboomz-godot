using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class TrackDamageStatsTests
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
                DeathBoundaryY = -25f
            };
        }

        [Test]
        public void TrackDamageStats_SetsAllFields()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].TotalDamageDealt = 0f;
            state.Players[0].DirectHits = 0;
            state.Players[0].MaxSingleDamage = 0f;
            state.FirstBloodPlayerIndex = -1;
            state.Players[1].LastDamagedByIndex = -1;

            CombatResolver.TrackDamageStats(state, 0, 1, 25f);

            Assert.AreEqual(25f, state.Players[0].TotalDamageDealt, 0.01f);
            Assert.AreEqual(1, state.Players[0].DirectHits);
            Assert.AreEqual(25f, state.Players[0].MaxSingleDamage, 0.01f);
            Assert.AreEqual(0, state.FirstBloodPlayerIndex);
            Assert.AreEqual(0, state.Players[1].LastDamagedByIndex);
            Assert.AreEqual(5f, state.Players[1].LastDamagedByTimer, 0.01f);
        }

        [Test]
        public void TrackDamageStats_SkipsSelfDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].TotalDamageDealt = 0f;
            state.Players[0].DirectHits = 0;

            CombatResolver.TrackDamageStats(state, 0, 0, 25f);

            Assert.AreEqual(0f, state.Players[0].TotalDamageDealt, 0.01f);
            Assert.AreEqual(0, state.Players[0].DirectHits);
        }

        [Test]
        public void TrackDamageStats_TracksWeaponStats()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            CombatResolver.TrackDamageStats(state, 0, 1, 30f, "bazooka");

            Assert.IsTrue(state.WeaponHits[0].ContainsKey("bazooka"));
            Assert.AreEqual(1, state.WeaponHits[0]["bazooka"]);
            Assert.IsTrue(state.WeaponDamage[0].ContainsKey("bazooka"));
            Assert.AreEqual(30f, state.WeaponDamage[0]["bazooka"], 0.01f);
        }

        [Test]
        public void TrackDamageStats_SkipsWeaponStatsWhenNoWeaponId()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            CombatResolver.TrackDamageStats(state, 0, 1, 30f);

            Assert.AreEqual(0, state.WeaponHits[0].Count);
        }

        [Test]
        public void TrackDamageStats_TracksComboHit()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].ConsecutiveHits = 0;

            CombatResolver.TrackDamageStats(state, 0, 1, 10f);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits);
        }

        [Test]
        public void HookShot_TracksComboHits()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "hookshot", Type = SkillType.HookShot,
                EnergyCost = 0f, Cooldown = 0f, Range = 15f, Value = 10f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].ConsecutiveHits = 0;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits,
                "HookShot should increment ConsecutiveHits via TrackDamageStats");
        }

        [Test]
        public void Earthquake_TracksComboHits()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].ConsecutiveHits = 0;
            state.Players[1].IsGrounded = true;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits,
                "Earthquake should increment ConsecutiveHits via TrackDamageStats");
        }

        [Test]
        public void FireZone_TracksDirectHitsAndMaxDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].DirectHits = 0;
            state.Players[0].MaxSingleDamage = 0f;
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 5f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            GameSimulation.Tick(state, 0.1f);

            Assert.Greater(state.Players[0].DirectHits, 0,
                "FireZone should track DirectHits via TrackDamageStats");
            Assert.Greater(state.Players[0].MaxSingleDamage, 0f,
                "FireZone should track MaxSingleDamage via TrackDamageStats");
        }

        [Test]
        public void FireZone_SetsFirstBloodAndLastDamagedBy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.FirstBloodPlayerIndex = -1;
            state.Players[1].LastDamagedByIndex = -1;
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 5f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(0, state.FirstBloodPlayerIndex,
                "FireZone should set FirstBloodPlayerIndex via TrackDamageStats");
            Assert.AreEqual(0, state.Players[1].LastDamagedByIndex,
                "FireZone should set LastDamagedByIndex via TrackDamageStats");
        }
    }
}
