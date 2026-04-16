using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Boss multi-shot tests ---

        [Test]
        public void Boss_MultiShot_FiresAllProjectiles()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as a boss with a single-projectile weapon (cannon = slot 0)
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].FacingDirection = -1;
            state.Players[1].BossType = "iron_sentinel";
            state.Players[1].BossPhase = 1;
            state.Players[1].AimAngle = 45f;
            state.Players[1].AimPower = 15f;
            state.Players[1].Energy = 1000f;
            state.Players[1].ActiveWeaponSlot = 0;

            int projBefore = state.Projectiles.Count;

            // Simulate a 3-shot burst by resetting cooldown before each Fire
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

        // --- Biome hazard tests ---

        [Test]
        public void BiomeHazards_SpawnedAtMatchCreation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.Greater(state.BiomeHazards.Count, 0,
                "Biome hazards should be spawned at match creation");
            Assert.IsTrue(state.BiomeHazards[0].Active);
            Assert.AreEqual(state.Biome.HazardType, state.BiomeHazards[0].Type,
                "Hazard type should match biome");
        }

        [Test]
        public void BiomeHazard_Lava_DamagesPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player and lava at same position
            state.Players[0].Position = new Vec2(0f, 5f);
            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Lava,
                Active = true
            });

            // Ensure terrain exists at this position for hazard to stay active
            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();

            // Tick multiple frames — player will take lava damage each frame they're in range
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Health, healthBefore,
                "Lava hazard should damage player standing in it");
        }

        [Test]
        public void BiomeHazard_Bounce_LaunchesPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on terrain first
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            // Place bounce hazard at player 0's settled position
            Vec2 playerPos = state.Players[0].Position;
            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Bounce,
                Active = true
            });

            // Ensure terrain exists under hazard
            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            float yBefore = state.Players[0].Position.y;

            // Tick a few frames — bounce should launch player
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.y, yBefore + 0.5f,
                "Bounce hazard should launch player upward");
        }

        [Test]
        public void BiomeHazard_Ice_AcceleratesPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player with some horizontal velocity on an ice patch
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].Velocity = new Vec2(3f, 0f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Ice,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            state.Phase = MatchPhase.Playing;
            // Tick multiple frames to accumulate ice sliding
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // Ice should have added momentum — player should have moved
            Assert.AreNotEqual(0f, state.Players[0].Velocity.x,
                "Ice hazard should give player sliding velocity");
        }

        [Test]
        public void BiomeHazard_Lava_SkipsInvulnerable()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsInvulnerable = true;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Lava should not damage invulnerable players");
        }

        [Test]
        public void BiomeHazard_Lava_RespectsArmorMultiplier()
        {
            // Regression test for #327 — lava damage must be reduced by ArmorMultiplier
            // (previously applied raw DPS, bypassing Shield skill)
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;

            // Baseline: unarmored player
            var stateNoArmor = GameSimulation.CreateMatch(config, 42);
            stateNoArmor.Players[0].Position = new Vec2(0f, 5f);
            stateNoArmor.Players[0].ArmorMultiplier = 1f;
            stateNoArmor.BiomeHazards.Clear();
            stateNoArmor.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });
            int pxNa = stateNoArmor.Terrain.WorldToPixelX(0f);
            int pyNa = stateNoArmor.Terrain.WorldToPixelY(4.5f);
            stateNoArmor.Terrain.SetSolid(pxNa, pyNa, true);

            // Armored player (3x armor → 1/3 damage)
            var stateArmored = GameSimulation.CreateMatch(config, 42);
            stateArmored.Players[0].Position = new Vec2(0f, 5f);
            stateArmored.Players[0].ArmorMultiplier = 3f;
            stateArmored.BiomeHazards.Clear();
            stateArmored.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });
            int pxA = stateArmored.Terrain.WorldToPixelX(0f);
            int pyA = stateArmored.Terrain.WorldToPixelY(4.5f);
            stateArmored.Terrain.SetSolid(pxA, pyA, true);

            stateNoArmor.Phase = MatchPhase.Playing;
            stateArmored.Phase = MatchPhase.Playing;

            float noArmorStart = stateNoArmor.Players[0].Health;
            float armoredStart = stateArmored.Players[0].Health;

            for (int i = 0; i < 30; i++)
            {
                GameSimulation.Tick(stateNoArmor, 0.016f);
                GameSimulation.Tick(stateArmored, 0.016f);
            }

            float noArmorDmg = noArmorStart - stateNoArmor.Players[0].Health;
            float armoredDmg = armoredStart - stateArmored.Players[0].Health;

            Assert.Greater(noArmorDmg, 0f, "Unarmored player should take lava damage");
            Assert.Greater(armoredDmg, 0f, "Armored player should still take some lava damage");
            Assert.Less(armoredDmg, noArmorDmg * 0.5f,
                "Armored player (3x) should take substantially less lava damage than unarmored");
        }

        [Test]
        public void BiomeHazard_Lava_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            // Guard against divide-by-zero when ArmorMultiplier=0
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].ArmorMultiplier = 0f;
            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });
            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(float.IsInfinity(state.Players[0].Health),
                "Lava with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(state.Players[0].Health),
                "Lava with ArmorMultiplier=0 should not produce NaN damage");
        }

        [Test]
        public void BiomeHazard_DisabledWhenTerrainDestroyed()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place hazard on terrain
            float hx = 0f;
            float hy = GamePhysics.FindGroundY(state.Terrain, hx, config.SpawnProbeY, 0.1f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(hx, hy),
                Radius = 3f,
                Type = BiomeHazardType.Mud,
                Active = true
            });

            // Destroy terrain under hazard
            int px = state.Terrain.WorldToPixelX(hx);
            int py = state.Terrain.WorldToPixelY(hy - 0.5f);
            state.Terrain.ClearCircleDestructible(px, py, 20);

            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.BiomeHazards[0].Active,
                "Hazard should deactivate when terrain underneath is destroyed");
        }

        [Test]
        public void Chinatown_BiomeExists_InAllArray()
        {
            bool found = false;
            for (int i = 0; i < TerrainBiome.All.Length; i++)
            {
                if (TerrainBiome.All[i].Name == "Chinatown")
                {
                    found = true;
                    Assert.AreEqual(BiomeHazardType.Firecracker, TerrainBiome.All[i].HazardType);
                    Assert.AreEqual(4, TerrainBiome.All[i].HazardCount);
                    break;
                }
            }
            Assert.IsTrue(found, "Chinatown biome should exist in TerrainBiome.All");
        }

        [Test]
        public void BaronCogsworth_PhaseTransition_ResetsAttackTimer_Issue124()
        {
            // Issue #124: entering a new phase left attackTimer holding the previous
            // phase's cadence, so the first shot of Phase 2/3 fired with random drift.
            // The fix resets attackTimer on each transition to that phase's cadence.
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

            // Access internal attackTimer via reflection — it is the field the bug
            // targets and is otherwise test-inaccessible.
            var field = typeof(BossLogic).GetField(
                "attackTimer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(field, "BossLogic.attackTimer field must exist");
            float[] timers = (float[])field.GetValue(null);

            // Simulate Phase 1 leftover state: attackTimer already passed (stale).
            timers[1] = state.Time - 5f;

            // Drop HP below 66% to trigger transition to Phase 2 (BossPhase 1).
            state.Players[1].Health = 120f; // 60% of 200
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Players[1].BossPhase,
                "Boss should have transitioned to Phase 2 (BossPhase=1)");

            // After fix: attackTimer is reset to (state.Time + 4f) for Phase 2 cadence.
            // It must be strictly in the future so the first dual-cannon volley waits
            // a proper 4s cycle instead of firing on stale drift.
            Assert.Greater(timers[1], state.Time,
                "attackTimer must be in the future after phase transition (issue #124)");
            Assert.LessOrEqual(timers[1] - state.Time, 4.1f,
                "attackTimer should be set to ~t+4 for Phase 2 dual-cannon cadence");

            // Drop HP below 33% to trigger transition to Phase 3 (BossPhase 2).
            // Stale the timer again first.
            timers[1] = state.Time - 5f;
            state.Players[1].Health = 60f; // 30% of 200
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(2, state.Players[1].BossPhase,
                "Boss should have transitioned to Phase 3 (BossPhase=2)");
            Assert.Greater(timers[1], state.Time,
                "Phase 3 transition must also reset attackTimer into the future");
            Assert.LessOrEqual(timers[1] - state.Time, 1.6f,
                "attackTimer should be set to ~t+1.5 for Phase 3 rapid-fire cadence");
        }
    }
}
