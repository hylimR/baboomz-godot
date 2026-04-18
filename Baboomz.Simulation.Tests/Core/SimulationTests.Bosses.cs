using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void Boss_MultiShot_FiresAllProjectiles()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].FacingDirection = -1;
            state.Players[1].BossType = "iron_sentinel";
            state.Players[1].BossPhase = 1;
            state.Players[1].AimAngle = 45f;
            state.Players[1].AimPower = 15f;
            state.Players[1].Energy = 1000f;
            state.Players[1].ActiveWeaponSlot = 0;

            int projBefore = state.Projectiles.Count;

            for (int s = 0; s < 3; s++)
            {
                state.Players[1].AimAngle = 45f + (s - 1) * 7f;
                state.Players[1].AimPower = 15f;
                state.Players[1].ShootCooldownRemaining = 0f;
                GameSimulation.Fire(state, 1);
            }

            Assert.AreEqual(projBefore + 3, state.Projectiles.Count,
                "Boss 3-shot burst should create 3 projectiles when cooldown is reset between shots");
        }

        [Test]
        public void BaronCogsworth_PhaseTransition_ResetsAttackTimer_Issue124()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].BossType = "baron_cogsworth";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 200f;
            state.Players[1].Health = 200f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42, state.Players.Length);

            var field = typeof(BossLogic).GetField(
                "attackTimer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(field, "BossLogic.attackTimer field must exist");
            float[] timers = (float[])field.GetValue(null);

            timers[1] = state.Time - 5f;

            state.Players[1].Health = 120f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Players[1].BossPhase,
                "Boss should have transitioned to Phase 2 (BossPhase=1)");

            Assert.Greater(timers[1], state.Time,
                "attackTimer must be in the future after phase transition (issue #124)");
            Assert.LessOrEqual(timers[1] - state.Time, 4.1f,
                "attackTimer should be set to ~t+4 for Phase 2 dual-cannon cadence");

            timers[1] = state.Time - 5f;
            state.Players[1].Health = 60f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(2, state.Players[1].BossPhase,
                "Boss should have transitioned to Phase 3 (BossPhase=2)");
            Assert.Greater(timers[1], state.Time,
                "Phase 3 transition must also reset attackTimer into the future");
            Assert.LessOrEqual(timers[1] - state.Time, 1.6f,
                "attackTimer should be set to ~t+1.5 for Phase 3 rapid-fire cadence");
        }

        [Test]
        public void ForgeColossus_ArmorReset_FiresOnlyOnce()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].BossType = "forge_colossus";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 200f;
            state.Players[1].Health = 200f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42, state.Players.Length);

            state.Players[1].Health = 140f;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Players[1].BossPhase);
            Assert.AreEqual(2f, state.Players[1].ArmorMultiplier, 0.01f,
                "Boss should have 2x armor after phase 1");

            for (int i = 0; i < 700; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1f, state.Players[1].ArmorMultiplier, 0.01f,
                "Armor should reset to 1x after timer expires");

            state.Players[1].ArmorMultiplier = 1.5f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1.5f, state.Players[1].ArmorMultiplier, 0.01f,
                "Armor modification after reset should not be overwritten by repeated reset");
        }
    }
}
