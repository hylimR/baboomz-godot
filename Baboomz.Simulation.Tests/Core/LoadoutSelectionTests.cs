using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class LoadoutSelectionTests
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
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f
            };
        }

        [Test]
        public void CreateMatch_DefaultSkills_TeleportAndDash()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Default: slot0=teleport(0), slot1=dash(3)
            Assert.AreEqual("teleport", state.Players[0].SkillSlots[0].SkillId);
            Assert.AreEqual("dash", state.Players[0].SkillSlots[1].SkillId);
        }

        [Test]
        public void CreateMatch_CustomPlayerSkills_Applied()
        {
            // Pick shield(2) and heal(4) instead of defaults
            var state = GameSimulation.CreateMatch(SmallConfig(), 42, playerSkill0: 2, playerSkill1: 4);
            Assert.AreEqual("shield", state.Players[0].SkillSlots[0].SkillId);
            Assert.AreEqual("heal", state.Players[0].SkillSlots[1].SkillId);
        }

        [Test]
        public void CreateMatch_CustomSkills_DoNotAffectAI()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42, playerSkill0: 2, playerSkill1: 4);
            // AI should NOT have the same skills as the player (they use AILogic.PickLoadout)
            // AI has its own loadout — just verify it has valid skills
            Assert.IsNotNull(state.Players[1].SkillSlots[0].SkillId);
            Assert.IsNotNull(state.Players[1].SkillSlots[1].SkillId);
            Assert.AreEqual(2, state.Players[1].SkillSlots.Length);
        }

        [Test]
        public void CreateMatch_NegativeSkillIndices_FallbackToDefaults()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42, playerSkill0: -1, playerSkill1: -1);
            Assert.AreEqual("teleport", state.Players[0].SkillSlots[0].SkillId);
            Assert.AreEqual("dash", state.Players[0].SkillSlots[1].SkillId);
        }

        [Test]
        public void CreateMatch_AllSkillIndices_Valid()
        {
            var config = SmallConfig();
            for (int i = 0; i < config.Skills.Length; i++)
            {
                int other = (i + 1) % config.Skills.Length;
                var state = GameSimulation.CreateMatch(config, 42, playerSkill0: i, playerSkill1: other);
                Assert.AreEqual(config.Skills[i].SkillId, state.Players[0].SkillSlots[0].SkillId,
                    $"Skill index {i} should map to {config.Skills[i].SkillId}");
                Assert.AreEqual(config.Skills[other].SkillId, state.Players[0].SkillSlots[1].SkillId);
            }
        }

        [Test]
        public void AIPickLoadout_ReturnsTwoDistinctSkills()
        {
            var config = SmallConfig();
            for (int seed = 0; seed < 20; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                Assert.AreEqual(2, loadout.Length, $"Seed {seed}: should return 2 skills");
                Assert.AreNotEqual(loadout[0], loadout[1], $"Seed {seed}: skills should be distinct");
                Assert.GreaterOrEqual(loadout[0], 0);
                Assert.Less(loadout[0], config.Skills.Length);
                Assert.GreaterOrEqual(loadout[1], 0);
                Assert.Less(loadout[1], config.Skills.Length);
            }
        }

        [Test]
        public void AIPickLoadout_DifferentSeedsProduceDifferentLoadouts()
        {
            var config = SmallConfig();
            bool anyDifferent = false;
            int[] first = AILogic.PickLoadout(config, 0);
            for (int seed = 1; seed < 50; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                if (loadout[0] != first[0] || loadout[1] != first[1])
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(anyDifferent, "Different seeds should produce varied AI loadouts");
        }

        [Test]
        public void AIPickLoadout_CanIncludeDeflectAndDecoy()
        {
            var config = SmallConfig();
            bool hasDeflect = false;
            bool hasDecoy = false;
            // Deflect = index 12, Decoy = index 13
            for (int seed = 0; seed < 200; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                if (loadout[0] == 12 || loadout[1] == 12) hasDeflect = true;
                if (loadout[0] == 13 || loadout[1] == 13) hasDecoy = true;
                if (hasDeflect && hasDecoy) break;
            }
            Assert.IsTrue(hasDeflect, "Deflect (index 12) should appear in AI loadouts");
            Assert.IsTrue(hasDecoy, "Decoy (index 13) should appear in AI loadouts");
        }

        [Test]
        public void AIPickLoadout_Easy_FullyRandom()
        {
            // Regression: #275 — Easy difficulty should use random picks, not Normal strategy
            var config = SmallConfig();
            config.AIDifficultyLevel = 0; // Easy
            int[] mobility = { 0, 3, 5, 15 };
            bool hasNonMobility = false;
            for (int seed = 0; seed < 100; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                Assert.AreEqual(2, loadout.Length);
                Assert.AreNotEqual(loadout[0], loadout[1]);
                // Check if slot 0 ever picks a non-mobility skill (Normal always picks mobility for slot 0)
                if (System.Array.IndexOf(mobility, loadout[0]) < 0)
                    hasNonMobility = true;
            }
            Assert.IsTrue(hasNonMobility,
                "Easy difficulty should sometimes pick non-mobility skills for slot 0");
        }

        [Test]
        public void AIPickLoadout_Hard_MobilityPlusDefensive()
        {
            // Regression: #275 — Hard difficulty should use strategic mobility + defensive picks
            var config = SmallConfig();
            config.AIDifficultyLevel = 2; // Hard
            int[] mobility = { 0, 3, 5, 15 };
            int[] defensive = { 2, 4 };
            for (int seed = 0; seed < 50; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                Assert.AreEqual(2, loadout.Length);
                Assert.AreNotEqual(loadout[0], loadout[1]);
                Assert.IsTrue(System.Array.IndexOf(mobility, loadout[0]) >= 0,
                    $"Hard slot 0 should be mobility, got {loadout[0]} (seed={seed})");
                Assert.IsTrue(System.Array.IndexOf(defensive, loadout[1]) >= 0,
                    $"Hard slot 1 should be defensive, got {loadout[1]} (seed={seed})");
            }
        }

        [Test]
        public void AIPickLoadout_Normal_DefaultBehavior()
        {
            // Normal (AIDifficultyLevel=1) should still pick mobility + defensive/utility
            var config = SmallConfig();
            config.AIDifficultyLevel = 1;
            int[] mobility = { 0, 3, 5, 15 };
            for (int seed = 0; seed < 50; seed++)
            {
                int[] loadout = AILogic.PickLoadout(config, seed);
                Assert.AreEqual(2, loadout.Length);
                Assert.AreNotEqual(loadout[0], loadout[1]);
                Assert.IsTrue(System.Array.IndexOf(mobility, loadout[0]) >= 0,
                    $"Normal slot 0 should be mobility, got {loadout[0]} (seed={seed})");
            }
        }

        [Test]
        public void CreateMatch_PlayerSkillEnergyCost_MatchesConfig()
        {
            var config = SmallConfig();
            // Pick jetpack(5) and earthquake(7)
            var state = GameSimulation.CreateMatch(config, 42, playerSkill0: 5, playerSkill1: 7);
            Assert.AreEqual(config.Skills[5].EnergyCost, state.Players[0].SkillSlots[0].EnergyCost, 0.01f);
            Assert.AreEqual(config.Skills[7].EnergyCost, state.Players[0].SkillSlots[1].EnergyCost, 0.01f);
        }

        [Test]
        public void CreateMatch_PlayerSkillCooldown_MatchesConfig()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42, playerSkill0: 5, playerSkill1: 7);
            Assert.AreEqual(config.Skills[5].Cooldown, state.Players[0].SkillSlots[0].Cooldown, 0.01f);
            Assert.AreEqual(config.Skills[7].Cooldown, state.Players[0].SkillSlots[1].Cooldown, 0.01f);
        }

        [Test]
        public void DrillExpiry_TracksSourceWeaponId()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 near the drill's expiry position
            state.Players[1].Position = new Vec2(35f, state.Players[0].Position.y);
            state.Players[1].Health = 100f;

            // Spawn a drill projectile heading right — it will expire after 30 units
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(state.Players[0].Position.x, state.Players[1].Position.y + 0.5f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 25f,
                KnockbackForce = 3f,
                Alive = true,
                IsDrill = true,
                SourceWeaponId = "drill"
            });

            // Tick until drill expires (>30 units traveled)
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // The drill should have expired and the expiry explosion should track weapon hits
            bool hasHit = state.WeaponHits[0].ContainsKey("drill") && state.WeaponHits[0]["drill"] > 0;
            Assert.IsTrue(hasHit, "Drill expiry explosion should track SourceWeaponId in WeaponHits");
        }

        [Test]
        public void AI_LowEnergy_DoesNotSelectExpensiveWeapon()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(15f, 5f);
            state.Players[1].IsAI = true;
            // Set AI energy to 8 — just enough for cannon (cost 8), too low for everything else
            state.Players[1].Energy = 8f;

            // Tick enough frames for AI to attempt weapon selection and firing
            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state, 0.016f);

            // Every weapon the AI selected should have been affordable
            // If the bug is present, AI would select expensive weapons and waste shoot turns
            int slot = state.Players[1].ActiveWeaponSlot;
            float cost = state.Players[1].WeaponSlots[slot].EnergyCost;
            Assert.IsTrue(state.Players[1].Energy >= cost,
                $"AI selected weapon slot {slot} (cost {cost}) but only has {state.Players[1].Energy} energy");
        }

        // --- Balance Cycle 19 regression tests (#204) ---

        [Test]
        public void BalanceCycle19_GustCannon_KnockbackReduced()
        {
            var config = new GameConfig();
            var gust = config.Weapons[19];
            Assert.AreEqual("gust_cannon", gust.WeaponId);
            Assert.AreEqual(20f, gust.KnockbackForce, "Gust Cannon KB should be 20 (reduced from 30)");
            Assert.AreEqual(3f, gust.ShootCooldown, "Gust Cannon cooldown should be 3s (increased from 2.5s)");
        }

        [Test]
        public void BalanceCycle19_GravityBomb_Buffed()
        {
            var config = new GameConfig();
            var gb = config.Weapons[16];
            Assert.AreEqual("gravity_bomb", gb.WeaponId);
            Assert.AreEqual(2, gb.Ammo, "Gravity Bomb ammo should be 2 (buffed from 1)");
            Assert.AreEqual(25f, gb.EnergyCost, "Gravity Bomb energy should be 25 (reduced from 30)");
        }

        [Test]
        public void BalanceCycle19_Decoy_Buffed()
        {
            var config = new GameConfig();
            SkillDef decoy = default;
            foreach (var s in config.Skills)
            {
                if (s.SkillId == "decoy") { decoy = s; break; }
            }
            Assert.AreEqual("decoy", decoy.SkillId);
            Assert.AreEqual(30f, decoy.Value, "Decoy HP should be 30 (buffed from 1)");
            Assert.AreEqual(4f, decoy.Duration, "Decoy duration should be 4s (buffed from 2s)");
            Assert.AreEqual(30f, decoy.EnergyCost, "Decoy energy should be 30 (reduced from 35)");
        }

        // --- AI weapon selection for slots 17-19 (regression for #222) ---

        [Test]
        public void AI_SelectsRicochetDisc_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 17) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 17) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select ricochet disc (slot 17) at medium range");
        }

        [Test]
        public void AI_SelectsMagmaBall_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 18) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(15f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 18) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select magma ball (slot 18) at medium range");
        }

        [Test]
        public void AI_SelectsGustCannon_AtCloseRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 19) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(8f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 19) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select gust cannon (slot 19) at close range");
        }
    }
}
