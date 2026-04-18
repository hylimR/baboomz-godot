using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class BiomeModifierTests
    {
        // Seed-to-biome: 0=Grasslands, 1=Desert, 2=Arctic, 3=Volcanic, 4=Candy, 5=Chinatown, 6=Clockwork Foundry, 7=Sunken Ruins

        static GameConfig BaseConfig()
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
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f
            };
        }

        [Test]
        public void Grasslands_TerrainDestructionMultIs1_3()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 0); // Grasslands
            Assert.AreEqual("Grasslands", state.Biome.Name);
            Assert.AreEqual(1.3f, config.TerrainDestructionMult, 0.001f);
        }

        [Test]
        public void Desert_WindChangesFaster()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 1); // Desert
            Assert.AreEqual("Desert", state.Biome.Name);
            Assert.AreEqual(5f, config.WindChangeInterval, 0.001f);
            Assert.AreEqual(4.5f, config.MaxWindStrength, 0.001f);
        }

        [Test]
        public void Arctic_MoveSpeedReduced()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 2); // Arctic
            Assert.AreEqual("Arctic", state.Biome.Name);
            Assert.AreEqual(0.85f, config.MoveSpeedMult, 0.001f);
            Assert.AreEqual(4f, config.FallDamagePerMeter, 0.001f);
            // Player move speed should reflect the multiplier
            Assert.AreEqual(5f * 0.85f, state.Players[0].MoveSpeed, 0.01f);
        }

        [Test]
        public void Volcanic_FireZoneDurationMultIs1_5()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 3); // Volcanic
            Assert.AreEqual("Volcanic", state.Biome.Name);
            Assert.AreEqual(1.5f, config.FireZoneDurationMult, 0.001f);
        }

        [Test]
        public void Candy_CrateSpawnAndEnergyRegen()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 4); // Candy
            Assert.AreEqual("Candy", state.Biome.Name);
            Assert.AreEqual(10f, config.CrateSpawnInterval, 0.001f);
            Assert.AreEqual(13f, config.DefaultEnergyRegen, 0.001f);
        }

        [Test]
        public void Chinatown_KnockbackMultIs1_3()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 5); // Chinatown
            Assert.AreEqual("Chinatown", state.Biome.Name);
            Assert.AreEqual(1.3f, config.KnockbackMult, 0.001f);
        }

        [Test]
        public void ClockworkFoundry_CooldownMultiplierIs0_8()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 6); // Clockwork Foundry
            Assert.AreEqual("Clockwork Foundry", state.Biome.Name);
            Assert.AreEqual(0.8f, config.DefaultCooldownMultiplier, 0.001f);
        }

        [Test]
        public void ClockworkFoundry_CooldownMultiplierResetsOnBiomeSwitch()
        {
            var config = BaseConfig();
            float baseline = config.DefaultCooldownMultiplier;

            // Round 1: Clockwork Foundry reduces cooldown
            BiomeModifiers.Apply(config, TerrainBiome.All[6]); // Clockwork Foundry
            Assert.AreEqual(0.8f, config.DefaultCooldownMultiplier, 0.001f);

            // Round 2: Grasslands — cooldown must reset to baseline
            BiomeModifiers.Apply(config, TerrainBiome.All[0]); // Grasslands
            Assert.AreEqual(baseline, config.DefaultCooldownMultiplier, 0.001f);
        }

        [Test]
        public void SunkenRuins_GravityIs75Percent()
        {
            var config = BaseConfig();
            var state = GameSimulation.CreateMatch(config, 7); // Sunken Ruins
            Assert.AreEqual("Sunken Ruins", state.Biome.Name);
            Assert.AreEqual(9.81f * 0.75f, config.Gravity, 0.01f);
        }

        [Test]
        public void SunkenRuins_GravityResetsOnBiomeSwitch()
        {
            var config = BaseConfig();

            // Round 1: Sunken Ruins reduces gravity
            BiomeModifiers.Apply(config, TerrainBiome.All[7]); // Sunken Ruins
            Assert.AreEqual(9.81f * 0.75f, config.Gravity, 0.01f);

            // Round 2: Grasslands — gravity must reset to baseline
            BiomeModifiers.Apply(config, TerrainBiome.All[0]); // Grasslands
            Assert.AreEqual(9.81f, config.Gravity, 0.01f);
        }

        [Test]
        public void DefaultConfig_MultipliersAreOne()
        {
            var config = new GameConfig();
            Assert.AreEqual(1f, config.TerrainDestructionMult, 0.001f);
            Assert.AreEqual(1f, config.MoveSpeedMult, 0.001f);
            Assert.AreEqual(1f, config.KnockbackMult, 0.001f);
            Assert.AreEqual(1f, config.FireZoneDurationMult, 0.001f);
        }

        [Test]
        public void GetModifierHint_ReturnsHintForAllBiomes()
        {
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Grasslands"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Desert"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Arctic"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Volcanic"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Candy"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Chinatown"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Clockwork Foundry"));
            Assert.IsNotNull(BiomeModifiers.GetModifierHint("Sunken Ruins"));
            Assert.IsNull(BiomeModifiers.GetModifierHint("Unknown"));
        }

        [Test]
        public void Apply_ResetsModifiersFromPreviousBiome()
        {
            // Regression: #252 — biome modifiers persisted across rounds
            var config = BaseConfig();

            // Round 1: Chinatown sets KnockbackMult = 1.3
            BiomeModifiers.Apply(config, TerrainBiome.All[5]); // Chinatown
            Assert.AreEqual(1.3f, config.KnockbackMult, 0.001f);

            // Round 2: Grasslands — KnockbackMult must reset to baseline (1.0)
            BiomeModifiers.Apply(config, TerrainBiome.All[0]); // Grasslands
            Assert.AreEqual(1f, config.KnockbackMult, 0.001f);
            Assert.AreEqual(1.3f, config.TerrainDestructionMult, 0.001f);
        }

        [Test]
        public void Apply_ResetsAllFieldsAcrossMultipleBiomes()
        {
            // Verify no stacking across 3 different biomes
            var config = BaseConfig();

            BiomeModifiers.Apply(config, TerrainBiome.All[1]); // Desert
            Assert.AreEqual(5f, config.WindChangeInterval, 0.001f);

            BiomeModifiers.Apply(config, TerrainBiome.All[4]); // Candy
            Assert.AreEqual(10f, config.WindChangeInterval, 0.001f); // reset from Desert
            Assert.AreEqual(10f, config.CrateSpawnInterval, 0.001f);

            BiomeModifiers.Apply(config, TerrainBiome.All[2]); // Arctic
            Assert.AreEqual(20f, config.CrateSpawnInterval, 0.001f); // reset from Candy
            Assert.AreEqual(0.85f, config.MoveSpeedMult, 0.001f);
        }

        [Test]
        public void Apply_PreservesUserConfiguredBaseline()
        {
            // User/test sets CrateSpawnInterval before first Apply — baseline saves it
            var config = BaseConfig();
            config.CrateSpawnInterval = 2f;

            BiomeModifiers.Apply(config, TerrainBiome.All[5]); // Chinatown
            Assert.AreEqual(2f, config.CrateSpawnInterval, 0.001f); // preserved

            BiomeModifiers.Apply(config, TerrainBiome.All[4]); // Candy
            Assert.AreEqual(10f, config.CrateSpawnInterval, 0.001f); // Candy override

            BiomeModifiers.Apply(config, TerrainBiome.All[0]); // Grasslands
            Assert.AreEqual(2f, config.CrateSpawnInterval, 0.001f); // back to user baseline
        }

    }
}
