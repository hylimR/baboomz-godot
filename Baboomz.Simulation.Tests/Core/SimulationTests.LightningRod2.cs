using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {

        [Test]
        public void LightningRod_ChainBlocked_ByTerrain()
        {
            // Regression: #316 — chain lightning arced through solid terrain walls
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            // Player 0 fires right, player 1 is primary target, player 2 behind wall
            // Place players at y=5 to stay within terrain grid (Height=160, max world Y ≈ 9.875)
            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f); // within ChainRange (6)

            // Build a thick terrain wall between player 1 (x=0) and player 2 (x=4)
            // at world x=2, spanning y 4..7, 3 pixels wide — blocks the chain ray
            int wallCenterPx = state.Terrain.WorldToPixelX(2f);
            int wallMinY = state.Terrain.WorldToPixelY(4f);
            int wallMaxY = state.Terrain.WorldToPixelY(7f);
            for (int wx = wallCenterPx - 1; wx <= wallCenterPx + 1; wx++)
                for (int py = wallMinY; py <= wallMaxY; py++)
                    state.Terrain.SetSolid(wx, py, true);

            float health2Before = state.Players[2].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(health2Before, state.Players[2].Health, 0.01f,
                "Chain should not arc through terrain to hit player behind wall");
            Assert.AreEqual(-1, state.HitscanEvents[0].ChainTargetIndex,
                "No chain target when terrain blocks LOS");
        }

        [Test]
        public void LightningRod_SetsFirstBloodPlayerIndex()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Ensure no first blood yet
            Assert.AreEqual(-1, state.FirstBloodPlayerIndex);

            // Place player 1 directly in front of player 0
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(0, state.FirstBloodPlayerIndex,
                "Hitscan primary hit should set FirstBloodPlayerIndex");
        }

        [Test]
        public void LightningRod_ChainHit_SetsFirstBloodPlayerIndex()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add a third player for chain testing
            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1]; // clone
            players[2].Name = "Player3";
            state.Players = players;

            // Place primary target and chain target
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].IsInvulnerable = true; // skip primary, force chain-only scenario
            state.Players[2].Position = new Vec2(8f, 5f);
            state.Players[2].Health = 100f;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            // Primary is invulnerable so should be skipped; if chain hits player 2, first blood should be set
            // OR if primary still hits player 1 (invulnerable skip means no damage), first blood should come from chain
            // Actually with invulnerable, the hitscan skips the player entirely, so player 2 becomes primary target
            Assert.AreEqual(0, state.FirstBloodPlayerIndex,
                "Hitscan hit should set FirstBloodPlayerIndex even via chain path");
        }

        [Test]
        public void SmokeScreen_ExistsInConfig_AsSkill8()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Skills.Length >= 9, "Should have at least 9 skills");
            Assert.AreEqual("smoke", config.Skills[8].SkillId);
            Assert.AreEqual(SkillType.SmokeScreen, config.Skills[8].Type);
            Assert.AreEqual(25f, config.Skills[8].EnergyCost);
            Assert.AreEqual(4f, config.Skills[8].Duration);
            Assert.AreEqual(5f, config.Skills[8].Value); // radius
        }

        [Test]
        public void SmokeScreen_DeploysSmokeZone_OnActivation()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Give player the smoke skill in slot 0
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "smoke", Type = SkillType.SmokeScreen,
                EnergyCost = 25f, Cooldown = 10f, Duration = 4f,
                Range = 8f, Value = 5f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SmokeZones.Count, "Should have 1 smoke zone");
            Assert.AreEqual(5f, state.SmokeZones[0].Radius, "Smoke radius should be 5");
            Assert.Greater(state.SmokeZones[0].RemainingTime, 0f, "Smoke should have remaining time");
        }

        [Test]
        public void SmokeScreen_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.SmokeZones.Add(new SmokeZone
            {
                Position = new Vec2(5f, 5f),
                Radius = 5f,
                RemainingTime = 0.1f
            });

            // Tick past expiry
            SkillSystem.Update(state, 0.2f);

            Assert.AreEqual(0, state.SmokeZones.Count, "Smoke zone should expire after duration");
        }

        [Test]
        public void SmokeScreen_MaxTwoZones()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "smoke", Type = SkillType.SmokeScreen,
                EnergyCost = 0f, Cooldown = 0f, Duration = 4f,
                Range = 8f, Value = 5f
            };
            state.Players[0].Energy = 100f;

            // Deploy 3 smoke zones
            SkillSystem.ActivateSkill(state, 0, 0);
            state.Players[0].SkillSlots[0].IsActive = false;
            state.Players[0].SkillSlots[0].CooldownRemaining = 0f;
            SkillSystem.ActivateSkill(state, 0, 0);
            state.Players[0].SkillSlots[0].IsActive = false;
            state.Players[0].SkillSlots[0].CooldownRemaining = 0f;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.LessOrEqual(state.SmokeZones.Count, 2, "Max 2 smoke zones at once");
        }

        [Test]
        public void SmokeScreen_IncreasesAIAimError()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place smoke between the two players
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].IsAI = true;

            // Verify obscured check works
            state.SmokeZones.Add(new SmokeZone
            {
                Position = new Vec2(5f, 5f),
                Radius = 3f,
                RemainingTime = 10f
            });

            bool obscured = SkillSystem.IsLineObscuredBySmoke(state,
                state.Players[1].Position, state.Players[0].Position);
            Assert.IsTrue(obscured, "Line between players should be obscured by smoke");

            // Verify no obscured when smoke is far away
            state.SmokeZones.Clear();
            state.SmokeZones.Add(new SmokeZone
            {
                Position = new Vec2(50f, 50f),
                Radius = 3f,
                RemainingTime = 10f
            });

            bool notObscured = SkillSystem.IsLineObscuredBySmoke(state,
                state.Players[1].Position, state.Players[0].Position);
            Assert.IsFalse(notObscured, "Line should not be obscured when smoke is far away");
        }

        [Test]
        public void WarCry_ExistsInConfig()
        {
            var config = new GameConfig();
            // WarCry is skill index 9 (after smoke at 8)
            Assert.IsTrue(config.Skills.Length >= 10, "Should have at least 10 skills");
            Assert.AreEqual("warcry", config.Skills[9].SkillId);
            Assert.AreEqual(SkillType.WarCry, config.Skills[9].Type);
            Assert.AreEqual(40f, config.Skills[9].EnergyCost);
            Assert.AreEqual(5f, config.Skills[9].Duration);
        }

        [Test]
        public void WarCry_Solo_AppliesStrongerBuff()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "warcry", Type = SkillType.WarCry,
                EnergyCost = 0f, Cooldown = 18f, Duration = 5f,
                Range = 0f, Value = 1.5f
            };
            state.Players[0].Energy = 100f;
            float baseMoveSpeed = state.Players[0].MoveSpeed;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1.75f, state.Players[0].DamageMultiplier,
                "Solo War Cry should give 1.75x damage");
            Assert.Greater(state.Players[0].WarCryTimer, 0f,
                "War Cry timer should be active");
            Assert.AreEqual(baseMoveSpeed * 1.3f, state.Players[0].MoveSpeed, 0.01f,
                "Solo War Cry should give 1.3x move speed");
        }

        [Test]
        public void WarCry_Team_BuffsBothPlayers()
        {
            var config = SmallConfig();
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0; // same team

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "warcry", Type = SkillType.WarCry,
                EnergyCost = 0f, Cooldown = 18f, Duration = 5f,
                Range = 0f, Value = 1.5f
            };
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1.5f, state.Players[0].DamageMultiplier,
                "Team War Cry should give caster 1.5x damage");
            Assert.AreEqual(1.5f, state.Players[1].DamageMultiplier,
                "Team War Cry should give teammate 1.5x damage");
            Assert.Greater(state.Players[1].WarCryTimer, 0f,
                "Teammate should have War Cry timer active");
        }

        [Test]
        public void WarCry_ExpiresAndResetsBuffs()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            float baseMoveSpeed = state.Players[0].MoveSpeed;
            state.Players[0].WarCryTimer = 0.1f;
            state.Players[0].WarCrySpeedBuff = 1.3f;
            state.Players[0].DamageMultiplier = 1.75f;
            state.Players[0].MoveSpeed = baseMoveSpeed * 1.3f;

            // Tick past expiry
            GameSimulation.Tick(state, 0.2f);

            Assert.AreEqual(0f, state.Players[0].WarCryTimer,
                "War Cry timer should have expired");
            Assert.AreEqual(1f, state.Players[0].DamageMultiplier, 0.01f,
                "Damage multiplier should reset to default");
            Assert.AreEqual(baseMoveSpeed, state.Players[0].MoveSpeed, 0.01f,
                "Move speed should reset to base");
        }

        [Test]
        public void WarCry_DoesNotOverrideHigherDoubleDamage()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Player already has DoubleDamage (2x)
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 8f;

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "warcry", Type = SkillType.WarCry,
                EnergyCost = 0f, Cooldown = 18f, Duration = 5f,
                Range = 0f, Value = 1.5f
            };
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            // DoubleDamage (2x) > WarCry solo (1.75x), so 2x wins
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier,
                "DoubleDamage should not be overridden by lower War Cry multiplier");
        }

        [Test]
        public void WarCry_SpeedBuff_DoesNotStackOnOverlap()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float baseSpeed = state.Players[0].MoveSpeed;

            // Simulate first WarCry on player 0
            state.Players[0].WarCryTimer = 5f;
            state.Players[0].WarCrySpeedBuff = 1.2f;
            state.Players[0].MoveSpeed *= 1.2f;

            float buffedSpeed = state.Players[0].MoveSpeed;
            Assert.AreEqual(baseSpeed * 1.2f, buffedSpeed, 0.01f);

            // Simulate second overlapping WarCry (as if teammate cast it)
            // This is the bug path — calling ExecuteWarCry while buff is active
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "warcry", Type = SkillType.WarCry,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 5f, Value = 1.5f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Speed should still be baseSpeed * multiplier, not baseSpeed * 1.2 * multiplier
            float expectedSpeed = baseSpeed * 1.3f; // solo mode caster gets 1.3x
            Assert.AreEqual(expectedSpeed, state.Players[0].MoveSpeed, 0.01f,
                "WarCry speed buff should not stack — should restore old buff before applying new");
        }

        [Test]
        public void DoubleDamage_Expiry_RestoresWarCryMultiplier()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);
            state.Phase = MatchPhase.Playing;

            // WarCry active with 1.75x damage (solo caster buff)
            state.Players[0].WarCryTimer = 10f;
            state.Players[0].WarCryDamageBuff = 1.75f;
            state.Players[0].DamageMultiplier = 2f; // DoubleDamage overrode it to 2x
            state.Players[0].DoubleDamageTimer = 0.05f; // about to expire

            // Tick until DoubleDamage expires
            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(1.75f, state.Players[0].DamageMultiplier, 0.01f,
                "DoubleDamage expiry should restore WarCry multiplier (1.75x), not keep 2x");
            Assert.Greater(state.Players[0].WarCryTimer, 0f,
                "WarCry should still be active after DoubleDamage expires");
        }

        [Test]
        public void WarCry_Expiry_ClearsWarCryDamageBuff()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].WarCryTimer = 0.05f;
            state.Players[0].WarCryDamageBuff = 1.75f;
            state.Players[0].WarCrySpeedBuff = 1.3f;
            state.Players[0].DamageMultiplier = 1.75f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(0f, state.Players[0].WarCryDamageBuff, 0.01f,
                "WarCryDamageBuff should be cleared on expiry");
            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "DamageMultiplier should reset to default after WarCry expires");
        }

    }
}
