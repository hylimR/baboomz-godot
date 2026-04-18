using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Freeze + AI interaction ---

        [Test]
        public void Freeze_BlocksAI()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[1].FreezeTimer = 2f;
            int projBefore = state.Projectiles.Count;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Frozen AI should not fire");
        }

        [Test]
        public void Freeze_ZeroesAIVerticalVelocity()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Give AI upward velocity (simulating mid-jump)
            state.Players[1].Velocity = new Vec2(3f, 12f);
            state.Players[1].IsGrounded = false;
            state.Players[1].FreezeTimer = 2f;
            float startY = state.Players[1].Position.y;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[1].Velocity.x, 0.01f,
                "Frozen AI should have zero X velocity");
            Assert.LessOrEqual(state.Players[1].Velocity.y, 0f,
                "Frozen AI should not retain upward Y velocity (#302)");
            Assert.LessOrEqual(state.Players[1].Position.y, startY + 0.01f,
                "Frozen AI should not rise after being frozen mid-jump (#302)");
        }

        [Test]
        public void FindTarget_SkipsTeammates_InTeamMode()
        {
            var config = SmallConfig();
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);

            // CreateMatch makes 2 players; manually expand to 4 for 2v2
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[0]; // copy structure
            players[3] = state.Players[1]; // copy structure
            players[2].Name = "Player3";
            players[3].Name = "Player4";

            // Team 0: players 0,1; Team 1: players 2,3
            players[0].TeamIndex = 0;
            players[1].TeamIndex = 0;
            players[2].TeamIndex = 1;
            players[3].TeamIndex = 1;
            state.Players = players;

            // Player 0 (team 0) should target player 2 or 3 (team 1), never player 1
            int target = AILogic.FindTarget(state, 0);
            Assert.That(target == 2 || target == 3,
                "AI should target enemy team, not teammate. Got index: " + target);

            // Player 2 (team 1) should target player 0 or 1 (team 0), never player 3
            int target2 = AILogic.FindTarget(state, 2);
            Assert.That(target2 == 0 || target2 == 1,
                "AI should target enemy team, not teammate. Got index: " + target2);
        }

        [Test]
        public void FindTarget_IgnoresTeamFilter_InFFA()
        {
            var config = SmallConfig();
            config.TeamMode = false;
            var state = GameSimulation.CreateMatch(config, 42);

            // Even if TeamIndex happens to match, FFA should not filter by team
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;

            int target = AILogic.FindTarget(state, 0);
            Assert.AreEqual(1, target,
                "In FFA mode, team index should be ignored");
        }

        [Test]
        public void FindTarget_DoesNotSkipAll_WhenTeamIndexNegativeOne()
        {
            var config = SmallConfig();
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);

            // All players have default TeamIndex = -1 (unassigned)
            state.Players[0].TeamIndex = -1;
            state.Players[1].TeamIndex = -1;

            // Without the selfTeam >= 0 guard, all players match -1 == -1
            // and FindTarget would skip everyone, returning -1
            int target = AILogic.FindTarget(state, 0);
            Assert.AreEqual(1, target,
                "FindTarget should not skip players when selfTeam is -1 (unassigned)");
        }

        [Test]
        public void AI_DoesNotSelectShotgun_WhenAmmoEmpty()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            // Place AI very close to player (< 5 units) to trigger shotgun preference
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(3f, 5f);
            state.Players[1].IsAI = true;

            // Set shotgun ammo to 0
            state.Players[1].WeaponSlots[1].Ammo = 0;

            // Tick several times to let AI select a weapon and fire
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            // AI should NOT have shotgun selected (slot 1) since ammo is 0
            Assert.AreNotEqual(1, state.Players[1].ActiveWeaponSlot,
                "AI should not select shotgun when ammo is 0");
        }

        [Test]
        public void SkillSystem_BlocksConcurrentDurationSkills()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Give player two duration-based skills: Shield and Heal
            state.Players[0].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "shield", Type = SkillType.Shield,
                    Duration = 5f, Value = 0.5f, Cooldown = 10f
                },
                new SkillSlotState
                {
                    SkillId = "heal", Type = SkillType.Heal,
                    Duration = 3f, Value = 30f, Cooldown = 10f
                }
            };
            state.Players[0].Energy = 100f;

            // Activate first skill (shield)
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive,
                "Shield should activate");

            // Try to activate second skill (heal) while shield is active
            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsFalse(state.Players[0].SkillSlots[1].IsActive,
                "Heal should NOT activate while shield is active");
        }

        [Test]
        public void SkillSystem_AllowsSkillAfterPreviousExpires()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "shield", Type = SkillType.Shield,
                    Duration = 0.1f, Value = 0.5f, Cooldown = 0f
                },
                new SkillSlotState
                {
                    SkillId = "heal", Type = SkillType.Heal,
                    Duration = 3f, Value = 30f, Cooldown = 0f
                }
            };
            state.Players[0].Energy = 100f;

            // Activate shield (very short duration)
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            // Tick until shield expires
            SkillSystem.Update(state, 0.2f);
            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "Shield should have expired");

            // Now heal should activate
            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive,
                "Heal should activate after shield expired");
        }

        [Test]
        public void Teleport_ResetsVelocity()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Give player a teleport skill
            state.Players[0].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "teleport", Type = SkillType.Teleport,
                    Range = 5f, Cooldown = 10f
                }
            };
            state.Players[0].Energy = 100f;

            // Set player falling at high speed
            state.Players[0].Velocity = new Vec2(8f, -15f);

            // Activate teleport
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.001f,
                "Velocity X should be reset after teleport");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.001f,
                "Velocity Y should be reset after teleport");
        }

        [Test]
        public void AI_FallbackWeapon_DoesNotCrash_WithFewerThan4Slots()
        {
            // Config with only 2 weapons (cannon + shotgun) — fewer than 4 slots
            var config = SmallConfig();
            config.Weapons = new[]
            {
                new WeaponDef { WeaponId = "cannon", MaxDamage = 30f, ExplosionRadius = 3f, MaxPower = 20f, Ammo = -1 },
                new WeaponDef { WeaponId = "shotgun", MaxDamage = 10f, ExplosionRadius = 1f, MaxPower = 18f, Ammo = 4, ProjectileCount = 4, SpreadAngle = 15f }
            };
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(15f, 5f);
            state.Players[1].IsAI = true;

            // Tick enough frames for AI to reach fallback weapon selection
            // With only 2 weapon slots, the old code would throw IndexOutOfRangeException
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 120; i++)
                    GameSimulation.Tick(state, 0.016f);
            }, "AI fallback weapon selection should not crash with fewer than 4 weapon slots");

            // AI should fall back to slot 0 (cannon) since slot 3 doesn't exist
            Assert.IsTrue(state.Players[1].ActiveWeaponSlot < state.Players[1].WeaponSlots.Length,
                "AI active weapon slot must be within bounds");
        }

        [Test]
        public void BalanceCheck_ShotgunStats_Issue186()
        {
            var config = new GameConfig();
            var shotgun = config.Weapons[1];
            Assert.AreEqual("shotgun", shotgun.WeaponId);
            // Balance #186: Shotgun buff — MaxDamage 15->18, CD 2.0->1.8, EnergyCost 18->14
            Assert.AreEqual(18f, shotgun.MaxDamage, "Shotgun MaxDamage should be 18");
            Assert.AreEqual(1.8f, shotgun.ShootCooldown, 0.001f, "Shotgun ShootCooldown should be 1.8");
            Assert.AreEqual(14f, shotgun.EnergyCost, "Shotgun EnergyCost should be 14");
        }

        [Test]
        public void BalanceCheck_BananaBombSubDamage_Issue34()
        {
            // Issue #34 conservative bump: MaxDamage 22 -> 26 (per-shot burst 26×6=156,
            // just above #22's 132 without reverting the gate). Ammo stays 1, cooldown stays 4.
            var config = new GameConfig();
            var banana = config.Weapons[11];
            Assert.AreEqual("banana_bomb", banana.WeaponId);
            Assert.AreEqual(26f, banana.MaxDamage, "Banana sub-projectile damage should be 26 (issue #34, was 22)");
            Assert.AreEqual(6, banana.ClusterCount, "Banana should still have 6 sub-projectiles");
        }
    }
}
