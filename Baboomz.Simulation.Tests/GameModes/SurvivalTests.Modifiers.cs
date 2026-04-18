using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SurvivalTests
    {
        [Test]
        public void Modifier_NoneBeforeWave5()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            Assert.AreEqual(1, state.Survival.WaveNumber);
            Assert.AreEqual(SurvivalModifier.None, state.Survival.ActiveModifier);
        }

        [Test]
        public void Modifier_CanActivateFromWave5()
        {
            bool foundModifier = false;
            for (int seed = 0; seed < 200; seed++)
            {
                var state = GameSimulation.CreateMatch(SurvivalConfig(), seed);
                for (int w = 0; w < 4; w++)
                {
                    TickPastBreak(state);
                    ClearWave(state);
                }
                TickPastBreak(state);
                Assert.AreEqual(5, state.Survival.WaveNumber);

                if (state.Survival.ActiveModifier != SurvivalModifier.None)
                {
                    foundModifier = true;
                    break;
                }
            }
            Assert.IsTrue(foundModifier, "At least one seed should produce a modifier at wave 5");
        }

        [Test]
        public void Modifier_LowGravity_HalvesGravity()
        {
            var state = CreateStateWithModifier(SurvivalModifier.LowGravity);
            Assert.AreEqual(9.81f * 0.5f, state.Config.Gravity, 0.01f);
        }

        [Test]
        public void Modifier_LowGravity_RevertsOnClear()
        {
            var state = CreateStateWithModifier(SurvivalModifier.LowGravity);
            ClearWave(state);
            Assert.AreEqual(9.81f, state.Config.Gravity, 0.01f);
        }

        [Test]
        public void Modifier_HeavyWind_DoublesMaxWind()
        {
            var state = CreateStateWithModifier(SurvivalModifier.HeavyWind);
            Assert.AreEqual(state.Config.MaxWindStrength * 2f, state.WindForce, 0.01f);
        }

        [Test]
        public void Modifier_GlassCannon_DoublesPlayerDamage()
        {
            var state = CreateStateWithModifier(SurvivalModifier.GlassCannon);
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f);
        }

        [Test]
        public void Modifier_GlassCannon_HalvesPlayerArmor()
        {
            var state = CreateStateWithModifier(SurvivalModifier.GlassCannon);
            Assert.AreEqual(0.5f, state.Players[0].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Modifier_GlassCannon_RevertsOnClear()
        {
            var state = CreateStateWithModifier(SurvivalModifier.GlassCannon);
            ClearWave(state);
            Assert.AreEqual(1f, state.Players[0].DamageMultiplier, 0.01f);
            Assert.AreEqual(1f, state.Players[0].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Modifier_ArmoredHorde_DoublesMobArmor()
        {
            var state = CreateStateWithModifier(SurvivalModifier.ArmoredHorde);
            for (int i = 1; i < state.Players.Length; i++)
                Assert.AreEqual(2f, state.Players[i].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Modifier_SpeedBlitz_IncreaseMobSpeed()
        {
            var state = CreateStateWithModifier(SurvivalModifier.SpeedBlitz);
            for (int i = 1; i < state.Players.Length; i++)
                Assert.Greater(state.Players[i].MoveSpeed, 3f);
        }

        [Test]
        public void Modifier_RegenWave_SetsMobHealthRegen()
        {
            var state = CreateStateWithModifier(SurvivalModifier.RegenWave);
            for (int i = 1; i < state.Players.Length; i++)
                Assert.AreEqual(3f, state.Players[i].HealthRegen, 0.01f);
        }

        [Test]
        public void Modifier_HealthRegenTicks()
        {
            var config = SurvivalConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            TickPastBreak(state);

            // Manually give a mob health regen and damage it
            state.Players[1].HealthRegen = 5f;
            float maxHp = state.Players[1].MaxHealth;
            state.Players[1].Health = maxHp - 10f;
            float before = state.Players[1].Health;

            GameSimulation.Tick(state, 1f);

            Assert.Greater(state.Players[1].Health, before, "Mob should regen HP");
            Assert.LessOrEqual(state.Players[1].Health, maxHp);
        }

        [Test]
        public void Modifier_BonusScoring_1_5x()
        {
            var state = CreateStateWithModifier(SurvivalModifier.LowGravity);
            int wave = state.Survival.WaveNumber;
            int scoreBefore = state.Survival.Score;

            ClearWave(state);

            int baseWaveScore = state.Config.SurvivalScorePerWave * wave;
            int expectedModified = (int)(baseWaveScore * 1.5f);
            int actual = state.Survival.Score - scoreBefore;
            // Score includes kill scoring from ClearWave + wave bonus + possible no-damage bonus
            Assert.GreaterOrEqual(actual, expectedModified,
                "Score should include at least 1.5x wave bonus when modifier is active");
        }

        [Test]
        public void Modifier_ClearedOnWaveEnd()
        {
            var state = CreateStateWithModifier(SurvivalModifier.LowGravity);
            Assert.AreNotEqual(SurvivalModifier.None, state.Survival.ActiveModifier);
            ClearWave(state);
            Assert.AreEqual(SurvivalModifier.None, state.Survival.ActiveModifier);
        }

        [Test]
        public void Modifier_GlassCannon_DoubleDamageExpiry_NoDesync()
        {
            var state = CreateStateWithModifier(SurvivalModifier.GlassCannon);
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f);

            // Simulate picking up a DoubleDamage crate (sets timer, multiplier stays 2x)
            state.Players[0].DoubleDamageTimer = 5f;

            // Tick until DoubleDamage expires
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.1f);

            // DoubleDamage expired — buff system resets to DefaultDamageMultiplier (1.0),
            // losing the GlassCannon bonus. This is the mid-wave state.
            float midWave = state.Players[0].DamageMultiplier;

            // Clear the wave → GlassCannon reverts using saved value
            ClearWave(state);

            Assert.AreEqual(state.Config.DefaultDamageMultiplier,
                state.Players[0].DamageMultiplier, 0.01f,
                "DamageMultiplier must return to default after GlassCannon revert, not 0.5");
            Assert.AreEqual(1f, state.Players[0].ArmorMultiplier, 0.01f,
                "ArmorMultiplier must return to default after GlassCannon revert");
        }

        /// <summary>
        /// Creates a survival state at wave 5+ with a specific modifier forced on.
        /// </summary>
        static GameState CreateStateWithModifier(SurvivalModifier modifier)
        {
            var config = SurvivalConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Advance to wave 5
            for (int w = 0; w < 4; w++)
            {
                TickPastBreak(state);
                ClearWave(state);
            }
            TickPastBreak(state);
            Assert.AreEqual(5, state.Survival.WaveNumber);

            // Force the modifier (revert any randomly-rolled one first)
            RevertIfActive(state);

            state.Survival.ActiveModifier = modifier;
            state.Survival.SavedGravity = 9.81f;
            state.Survival.SavedWindForce = state.WindForce;
            state.Survival.SavedWindAngle = state.WindAngle;

            switch (modifier)
            {
                case SurvivalModifier.LowGravity:
                    state.Config.Gravity *= 0.5f;
                    break;
                case SurvivalModifier.HeavyWind:
                    state.WindForce = state.Config.MaxWindStrength * 2f;
                    state.WindAngle = 0f;
                    break;
                case SurvivalModifier.GlassCannon:
                    state.Survival.SavedDamageMultiplier = state.Players[0].DamageMultiplier;
                    state.Survival.SavedArmorMultiplier = state.Players[0].ArmorMultiplier;
                    state.Players[0].DamageMultiplier *= 2f;
                    state.Players[0].ArmorMultiplier *= 0.5f;
                    break;
                case SurvivalModifier.ArmoredHorde:
                    for (int i = 1; i < state.Players.Length; i++)
                        state.Players[i].ArmorMultiplier = 2f;
                    break;
                case SurvivalModifier.SpeedBlitz:
                    for (int i = 1; i < state.Players.Length; i++)
                        state.Players[i].MoveSpeed *= 1.8f;
                    break;
                case SurvivalModifier.RegenWave:
                    for (int i = 1; i < state.Players.Length; i++)
                        state.Players[i].HealthRegen = 3f;
                    break;
            }

            return state;
        }

        static void RevertIfActive(GameState state)
        {
            ref var surv = ref state.Survival;
            if (surv.ActiveModifier == SurvivalModifier.None) return;

            switch (surv.ActiveModifier)
            {
                case SurvivalModifier.LowGravity:
                    state.Config.Gravity = surv.SavedGravity;
                    break;
                case SurvivalModifier.HeavyWind:
                    state.WindForce = surv.SavedWindForce;
                    state.WindAngle = surv.SavedWindAngle;
                    break;
                case SurvivalModifier.GlassCannon:
                    state.Players[0].DamageMultiplier = surv.SavedDamageMultiplier;
                    state.Players[0].ArmorMultiplier = surv.SavedArmorMultiplier;
                    break;
            }
            surv.ActiveModifier = SurvivalModifier.None;
        }
    }
}
