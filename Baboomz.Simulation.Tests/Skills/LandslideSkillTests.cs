using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class LandslideSkillTests
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

        static SkillDef FindSkill(GameConfig config, SkillType type)
        {
            for (int i = 0; i < config.Skills.Length; i++)
                if (config.Skills[i].Type == type) return config.Skills[i];
            throw new Exception("Skill not found: " + type);
        }

        static void SetSkillSlot(ref SkillSlotState slot, SkillDef def)
        {
            slot = new SkillSlotState
            {
                SkillId = def.SkillId,
                Type = def.Type,
                EnergyCost = def.EnergyCost,
                Cooldown = def.Cooldown,
                Duration = def.Duration,
                Range = def.Range,
                Value = def.Value
            };
        }

        [Test]
        public void Landslide_ClearsTerrainColumn()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var def = FindSkill(config, SkillType.Landslide);

            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], def);
            p.Energy = 100f;
            p.AimAngle = 0f;
            p.FacingDirection = 1;

            float range = Math.Min(def.Range, 12f);
            float targetX = p.Position.x + range;
            int cx = state.Terrain.WorldToPixelX(targetX);
            int cy = state.Terrain.WorldToPixelY(p.Position.y);
            state.Terrain.FillRect(cx - 15, cy - 30, 30, 60);

            int solidBefore = 0;
            for (int y = cy - 10; y <= cy + 10; y++)
            {
                int idx = (y * state.Terrain.Width + cx) * 4;
                if (idx >= 0 && idx + 3 < state.Terrain.Pixels.Length && state.Terrain.Pixels[idx + 3] != 0)
                    solidBefore++;
            }
            Assert.Greater(solidBefore, 0, "Should have solid terrain before landslide");

            SkillSystem.ActivateSkill(state, 0, 0);

            int solidAfter = 0;
            for (int y = cy - 10; y <= cy + 10; y++)
            {
                int idx = (y * state.Terrain.Width + cx) * 4;
                if (idx >= 0 && idx + 3 < state.Terrain.Pixels.Length && state.Terrain.Pixels[idx + 3] != 0)
                    solidAfter++;
            }
            Assert.Less(solidAfter, solidBefore, "Landslide should clear terrain pixels");
        }

        [Test]
        public void Landslide_ConsumesEnergy()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var def = FindSkill(config, SkillType.Landslide);

            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], def);
            p.Energy = 100f;
            float startEnergy = p.Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[0].Energy, startEnergy, "Landslide should consume energy");
        }

        [Test]
        public void Landslide_StartsCooldown()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var def = FindSkill(config, SkillType.Landslide);

            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], def);
            p.Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].SkillSlots[0].CooldownRemaining, 0f,
                "Landslide should start cooldown after use");
        }

        [Test]
        public void Landslide_ConfigValues_Correct()
        {
            var config = SmallConfig();
            var def = FindSkill(config, SkillType.Landslide);

            Assert.AreEqual(35f, def.EnergyCost);
            Assert.AreEqual(12f, def.Cooldown);
            Assert.AreEqual(10f, def.Range);
            Assert.AreEqual(6f, def.Value);
        }
    }
}
