using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public class ChallengeTrackingTests
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

        static GameState CreateState()
        {
            return GameSimulation.CreateMatch(SmallConfig(), 42);
        }

        // --- #199: WeaponDamage tracking ---

        [Test]
        public void ApplyExplosion_TracksWeaponDamage()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            Vec2 pos = state.Players[1].Position;
            CombatResolver.ApplyExplosion(state, pos, 3f, 50f, 20f, 0, false, "cannon");

            Assert.IsTrue(state.WeaponDamage[0].ContainsKey("cannon"),
                "ApplyExplosion should track weapon damage");
            Assert.Greater(state.WeaponDamage[0]["cannon"], 0f);
        }

        [Test]
        public void ApplyPierceDamage_TracksWeaponDamage()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            CombatResolver.ApplyPierceDamage(state, 1, 30f, 5f,
                state.Players[1].Position, 0, "harpoon");

            Assert.IsTrue(state.WeaponDamage[0].ContainsKey("harpoon"),
                "ApplyPierceDamage should track weapon damage");
            Assert.Greater(state.WeaponDamage[0]["harpoon"], 0f);
        }

        [Test]
        public void WeaponDamage_AccumulatesAcrossMultipleHits()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            CombatResolver.ApplyPierceDamage(state, 1, 10f, 0f,
                state.Players[1].Position, 0, "harpoon");
            float first = state.WeaponDamage[0]["harpoon"];

            state.Players[1].Health = 100f;
            state.Players[1].IsDead = false;
            CombatResolver.ApplyPierceDamage(state, 1, 10f, 0f,
                state.Players[1].Position, 0, "harpoon");

            Assert.Greater(state.WeaponDamage[0]["harpoon"], first,
                "Weapon damage should accumulate across hits");
        }

        [Test]
        public void TrackWeaponDamage_NullWeaponId_NoOp()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            Assert.DoesNotThrow(() =>
                CombatResolver.TrackWeaponDamage(state, 0, null, 50f));
            Assert.AreEqual(0, state.WeaponDamage[0].Count);
        }

        // --- #200: SkillsActivated tracking ---

        [Test]
        public void ActivateSkill_AddsToSkillsActivated()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.SkillsActivated[0].Contains(state.Players[0].SkillSlots[0].Type),
                "ActivateSkill should add skill type to SkillsActivated");
        }

        [Test]
        public void ActivateSkill_MultipleSkills_TracksDistinct()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.AreEqual(2, state.SkillsActivated[0].Count,
                "Two different skills should produce 2 distinct entries");
        }

        [Test]
        public void ActivateSkill_SameSkillTwice_OnlyOneEntry()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            SkillSystem.ActivateSkill(state, 0, 0);
            state.Players[0].SkillSlots[0].CooldownRemaining = 0f;
            state.Players[0].Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillsActivated[0].Count,
                "Same skill used twice should only produce 1 distinct entry (HashSet)");
        }

        [Test]
        public void NoSkillsActivated_SetRemainsEmpty()
        {
            var state = CreateState();
            state.InitWeaponTracking(state.Players.Length);

            Assert.AreEqual(0, state.SkillsActivated[0].Count,
                "SkillsActivated should be empty when no skills are used");
        }
    }
}
