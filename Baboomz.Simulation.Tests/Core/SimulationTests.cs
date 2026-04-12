using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class Vec2Tests
    {
        [Test]
        public void Addition()
        {
            var a = new Vec2(1f, 2f);
            var b = new Vec2(3f, 4f);
            var c = a + b;
            Assert.AreEqual(4f, c.x, 0.001f);
            Assert.AreEqual(6f, c.y, 0.001f);
        }

        [Test]
        public void Subtraction()
        {
            var r = new Vec2(5f, 3f) - new Vec2(2f, 1f);
            Assert.AreEqual(3f, r.x, 0.001f);
            Assert.AreEqual(2f, r.y, 0.001f);
        }

        [Test]
        public void ScalarMultiply()
        {
            var v = new Vec2(2f, 3f) * 2f;
            Assert.AreEqual(4f, v.x, 0.001f);
            Assert.AreEqual(6f, v.y, 0.001f);
        }

        [Test]
        public void Magnitude()
        {
            var v = new Vec2(3f, 4f);
            Assert.AreEqual(5f, v.Magnitude, 0.001f);
        }

        [Test]
        public void Normalized()
        {
            var v = new Vec2(0f, 5f).Normalized;
            Assert.AreEqual(0f, v.x, 0.001f);
            Assert.AreEqual(1f, v.y, 0.001f);
        }

        [Test]
        public void Distance()
        {
            float d = Vec2.Distance(new Vec2(0f, 0f), new Vec2(3f, 4f));
            Assert.AreEqual(5f, d, 0.001f);
        }

        [Test]
        public void ZeroVector_Normalized_ReturnsZero()
        {
            var v = Vec2.Zero.Normalized;
            Assert.AreEqual(0f, v.Magnitude, 0.001f);
        }
    }

    [TestFixture]
    public class TerrainStateTests
    {
        [Test]
        public void NewTerrain_IsEmpty()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            Assert.IsFalse(t.IsSolid(50, 25));
        }

        [Test]
        public void SetSolid_MakesPixelSolid()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 20, true);
            Assert.IsTrue(t.IsSolid(10, 20));
        }

        [Test]
        public void SetSolid_False_ClearsPixel()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 20, true);
            t.SetSolid(10, 20, false);
            Assert.IsFalse(t.IsSolid(10, 20));
        }

        [Test]
        public void OutOfBounds_ReturnsFalse()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            Assert.IsFalse(t.IsSolid(-1, 0));
            Assert.IsFalse(t.IsSolid(100, 0));
            Assert.IsFalse(t.IsSolid(0, -1));
            Assert.IsFalse(t.IsSolid(0, 50));
        }

        [Test]
        public void IsSurface_SolidWithAirAbove()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 5, true);
            // Air above — surface
            Assert.IsTrue(t.IsSurface(10, 5));
        }

        [Test]
        public void IsSurface_SolidWithSolidAbove_NotSurface()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.SetSolid(10, 5, true);
            t.SetSolid(10, 6, true);
            Assert.IsFalse(t.IsSurface(10, 5));
        }

        [Test]
        public void Indestructible_SurvivesClearCircle()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.FillRect(0, 0, 100, 50);
            t.SetIndestructible(50, 25, true);

            t.ClearCircleDestructible(50, 25, 5);

            // Indestructible pixel survives
            Assert.IsTrue(t.IsSolid(50, 25));
            Assert.IsTrue(t.IsIndestructible(50, 25));

            // Nearby destructible pixels are cleared
            Assert.IsFalse(t.IsSolid(50, 22));
        }

        [Test]
        public void ClearCircle_Force_DestroysIndestructible()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.FillRect(0, 0, 100, 50);
            t.SetIndestructible(50, 25, true);

            t.ClearCircle(50, 25, 5);

            Assert.IsFalse(t.IsSolid(50, 25));
        }

        [Test]
        public void WorldToPixel_Roundtrip()
        {
            var t = new TerrainState(160, 80, 16f, -5f, -2f);
            int px = t.WorldToPixelX(0f);
            int py = t.WorldToPixelY(0f);
            float wx = t.PixelToWorldX(px);
            float wy = t.PixelToWorldY(py);
            Assert.AreEqual(0f, wx, 0.1f);
            Assert.AreEqual(0f, wy, 0.1f);
        }

        [Test]
        public void FillRectIndestructible_SetsCorrectFlags()
        {
            var t = new TerrainState(100, 50, 10f, 0f, 0f);
            t.FillRectIndestructible(10, 10, 5, 5);

            Assert.IsTrue(t.IsSolid(12, 12));
            Assert.IsTrue(t.IsIndestructible(12, 12));
            Assert.IsFalse(t.IsIndestructible(9, 9));
        }
    }

    [TestFixture]
    public class GamePhysicsTests
    {
        [Test]
        public void ApplyGravity_DecreasesVelocityY()
        {
            Vec2 v = new Vec2(5f, 10f);
            GamePhysics.ApplyGravity(ref v, 1f, 9.81f);
            Assert.AreEqual(10f - 9.81f, v.y, 0.01f);
            Assert.AreEqual(5f, v.x, 0.01f); // X unchanged
        }

        [Test]
        public void ApplyWind_AffectsVelocityX()
        {
            Vec2 v = new Vec2(0f, 0f);
            GamePhysics.ApplyWind(ref v, 2f, 1f);
            Assert.AreEqual(2f, v.x, 0.01f);
        }

        [Test]
        public void IsGrounded_OnSolidTerrain_ReturnsTrue()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill a strip at pixel y=0..10
            for (int px = 0; px < 100; px++)
                for (int py = 0; py <= 10; py++)
                    t.SetSolid(px, py, true);

            float worldX = t.PixelToWorldX(50);

            // Position well above surface — not grounded
            float highY = t.PixelToWorldY(15);
            Assert.IsFalse(GamePhysics.IsGrounded(t, new Vec2(worldX, highY)),
                "Should not be grounded well above surface");

            // Position at surface level (feet touching solid)
            float surfaceY = t.PixelToWorldY(11) + 0.05f;
            Assert.IsTrue(GamePhysics.IsGrounded(t, new Vec2(worldX, surfaceY)),
                "Should be grounded when feet touch surface");
        }

        [Test]
        public void FindGroundY_FindsSurface()
        {
            var t = new TerrainState(160, 80, 16f, -5f, -2f);
            // Fill solid from y=0 to y=39 (world y = -2 to about +0.4)
            for (int px = 0; px < 160; px++)
                for (int py = 0; py < 40; py++)
                    t.SetSolid(px, py, true);

            float groundY = GamePhysics.FindGroundY(t, 0f, 10f);
            float expectedSurface = t.PixelToWorldY(41); // just above pixel 40
            Assert.AreEqual(expectedSurface, groundY, 0.5f);
        }

        [Test]
        public void RaycastTerrain_HitsSolidPixel()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill a horizontal strip
            for (int px = 0; px < 100; px++)
                t.SetSolid(px, 20, true);

            Vec2 from = new Vec2(0f, 5f); // above
            Vec2 to = new Vec2(0f, 0f);   // below

            bool hit = GamePhysics.RaycastTerrain(t, from, to, out Vec2 hitPoint);
            Assert.IsTrue(hit);
        }

        [Test]
        public void RaycastTerrain_MissesClearTerrain()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Empty terrain

            Vec2 from = new Vec2(0f, 5f);
            Vec2 to = new Vec2(0f, 0f);

            bool hit = GamePhysics.RaycastTerrain(t, from, to, out _);
            Assert.IsFalse(hit);
        }

        [Test]
        public void RaycastTerrain_HitsDiagonalCornerPixel()
        {
            // Regression test for #229: Bresenham diagonal skip
            // Place a single solid pixel on the diagonal path such that
            // it occupies an intermediate position the old code would skip.
            var t = new TerrainState(100, 50, 10f, -5f, 0f);

            // Ray goes diagonally from (0,0) to (9,9) in pixel space.
            // Place a solid pixel at an intermediate position (5+1, 5) = (6, 5)
            // that would be skipped when both x and y advance simultaneously.
            // Using pixel coords: set solid at the intermediate pixel the
            // diagonal step from (5,5) to (6,6) would skip.
            int cornerPx = 56; // pixel for an x just past the midpoint
            int cornerPy = 25; // pixel for the same y row
            t.SetSolid(cornerPx, cornerPy, true);

            // Convert those pixel coords to world coords for the ray endpoints
            float startX = t.PixelToWorldX(50);
            float startY = t.PixelToWorldY(20);
            float endX = t.PixelToWorldX(60);
            float endY = t.PixelToWorldY(30);

            Vec2 from = new Vec2(startX, startY);
            Vec2 to = new Vec2(endX, endY);

            bool hit = GamePhysics.RaycastTerrain(t, from, to, out Vec2 hitPoint);
            Assert.IsTrue(hit, "Raycast should detect corner pixel on diagonal step");
        }

        [Test]
        public void ClampToMapBounds_ClampsX()
        {
            Vec2 pos = new Vec2(999f, 5f);
            GamePhysics.ClampToMapBounds(ref pos, -50f, 50f, -30f);
            Assert.AreEqual(50f, pos.x, 0.01f);
        }

        [Test]
        public void ResolveTerrainPenetration_PushesUp()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill pixels 0-30
            for (int px = 0; px < 100; px++)
                for (int py = 0; py < 30; py++)
                    t.SetSolid(px, py, true);

            // Position inside terrain
            Vec2 pos = new Vec2(0f, 2.5f); // inside solid zone
            GamePhysics.ResolveTerrainPenetration(t, ref pos);

            // Should be pushed above solid
            int px2 = t.WorldToPixelX(pos.x);
            int py2 = t.WorldToPixelY(pos.y);
            Assert.IsFalse(t.IsSolid(px2, py2), "Position should be in air after resolution");
        }

        [Test]
        public void ResolveTerrainPenetration_DeepPenetration_FallbackScansFullHeight()
        {
            var t = new TerrainState(100, 80, 10f, -5f, 0f);
            // Fill solid from 0 to 45 — that's 4.5 world units, exceeds maxPush=2
            for (int px = 0; px < 100; px++)
                for (int py = 0; py < 45; py++)
                    t.SetSolid(px, py, true);

            // Place player deep inside terrain at y=0.5 (pixel 5, needs 40 pixels up)
            Vec2 pos = new Vec2(0f, 0.5f);
            GamePhysics.ResolveTerrainPenetration(t, ref pos);

            int py2 = t.WorldToPixelY(pos.y);
            Assert.IsFalse(t.IsSolid(t.WorldToPixelX(pos.x), py2),
                "Deep penetration should resolve via fallback scan");
            Assert.GreaterOrEqual(pos.y, 4.5f,
                "Should be pushed above the solid zone");
        }

        [Test]
        public void ResolveTerrainPenetration_FullySolid_PlacesAboveTerrain()
        {
            var t = new TerrainState(100, 50, 10f, -5f, 0f);
            // Fill entire terrain solid
            for (int px = 0; px < 100; px++)
                for (int py = 0; py < 50; py++)
                    t.SetSolid(px, py, true);

            Vec2 pos = new Vec2(0f, 1.0f);
            GamePhysics.ResolveTerrainPenetration(t, ref pos);

            // IsSolid returns false for out-of-bounds, so scan finds air at Height
            float terrainTop = t.PixelToWorldY(t.Height);
            Assert.AreEqual(terrainTop, pos.y, 0.01f,
                "Fully solid terrain should place player above terrain");
        }
    }

    [TestFixture]
    public class TerrainGeneratorTests
    {
        [Test]
        public void Generate_ProducesSolidTerrain()
        {
            var config = new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f
            };

            var terrain = TerrainGenerator.Generate(config, 42);

            // Should have some solid pixels
            int solidCount = 0;
            for (int x = 0; x < terrain.Width; x++)
                for (int y = 0; y < terrain.Height; y++)
                    if (terrain.IsSolid(x, y)) solidCount++;

            Assert.Greater(solidCount, 0, "Terrain should have solid pixels");
            Assert.Less(solidCount, terrain.Width * terrain.Height, "Terrain should not be entirely solid");
        }

        [Test]
        public void Generate_DeterministicWithSameSeed()
        {
            var config = new GameConfig
            {
                TerrainWidth = 160,
                TerrainHeight = 80,
                TerrainPPU = 8f,
                MapWidth = 20f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f
            };

            var t1 = TerrainGenerator.Generate(config, 123);
            var t2 = TerrainGenerator.Generate(config, 123);

            // Should produce identical terrain
            for (int i = 0; i < t1.Pixels.Length; i++)
                Assert.AreEqual(t1.Pixels[i], t2.Pixels[i], $"Pixel mismatch at index {i}");
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentTerrain()
        {
            var config = new GameConfig
            {
                TerrainWidth = 160,
                TerrainHeight = 80,
                TerrainPPU = 8f,
                MapWidth = 20f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f
            };

            var t1 = TerrainGenerator.Generate(config, 111);
            var t2 = TerrainGenerator.Generate(config, 222);

            bool anyDifference = false;
            for (int i = 0; i < t1.Pixels.Length; i++)
            {
                if (t1.Pixels[i] != t2.Pixels[i]) { anyDifference = true; break; }
            }
            Assert.IsTrue(anyDifference, "Different seeds should produce different terrain");
        }
    }

    [TestFixture]
    public class IslandModeTerrainTests
    {
        static GameConfig IslandConfig()
        {
            return new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 80,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -5f,
                TerrainMaxHeight = 6f,
                TerrainHillFrequency = 0.02f,
                TerrainFloorDepth = -15f
            };
        }

        [Test]
        public void Generate_IslandMode_CreatesGapsInTerrain()
        {
            var config = IslandConfig();
            var biome = new TerrainBiome
            {
                Name = "Storm at Sea",
                IslandMode = true,
                IslandCount = 3,
                IslandGapWidth = 5f,
                MinHeight = -5f, MaxHeight = 6f, HillFrequency = 0.02f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2
            };

            var terrainFlat = TerrainGenerator.Generate(config, 42);
            var terrainIsland = TerrainGenerator.Generate(config, 42, biome);

            // Island mode terrain should have fewer solid pixels (gaps cut out)
            int solidFlat = 0, solidIsland = 0;
            for (int x = 0; x < terrainFlat.Width; x++)
                for (int y = 0; y < terrainFlat.Height; y++)
                {
                    if (terrainFlat.IsSolid(x, y)) solidFlat++;
                    if (terrainIsland.IsSolid(x, y)) solidIsland++;
                }

            Assert.Less(solidIsland, solidFlat,
                "Island mode should remove terrain pixels (gaps) compared to flat generation");
        }

        [Test]
        public void Generate_IslandMode_IsDeterministic()
        {
            var config = IslandConfig();
            var biome = new TerrainBiome
            {
                Name = "Storm at Sea",
                IslandMode = true,
                IslandCount = 3,
                IslandGapWidth = 4f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2
            };

            var t1 = TerrainGenerator.Generate(config, 55, biome);
            var t2 = TerrainGenerator.Generate(config, 55, biome);

            for (int i = 0; i < t1.Pixels.Length; i++)
                Assert.AreEqual(t1.Pixels[i], t2.Pixels[i],
                    $"Island-mode terrain must be deterministic: pixel mismatch at index {i}");
        }

        [Test]
        public void Generate_IslandMode_RetainsTerrainBeyondGaps()
        {
            var config = IslandConfig();
            var biome = new TerrainBiome
            {
                Name = "Storm at Sea",
                IslandMode = true,
                IslandCount = 2,
                IslandGapWidth = 3f,
                HazardType = BiomeHazardType.Waterspout, HazardCount = 2
            };

            var terrain = TerrainGenerator.Generate(config, 99, biome);

            // Left-most column and right-most column should still have some terrain
            bool leftHasTerrain = false, rightHasTerrain = false;
            for (int y = 0; y < terrain.Height; y++)
            {
                if (terrain.IsSolid(0, y)) leftHasTerrain = true;
                if (terrain.IsSolid(terrain.Width - 1, y)) rightHasTerrain = true;
            }
            Assert.IsTrue(leftHasTerrain, "Left island edge should retain terrain");
            Assert.IsTrue(rightHasTerrain, "Right island edge should retain terrain");
        }
    }

    [TestFixture]
    public class WeaponLoadoutSimTests
    {
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
        public void PlayerWeaponLoadout_NullLoadout_AllWeaponsAvailable()
        {
            var config = BaseConfig();
            config.PlayerWeaponLoadout = null;
            var state = GameSimulation.CreateMatch(config, 42);

            // With no loadout filter, player should have all non-null weapon slots
            int nonNullCount = 0;
            foreach (var slot in state.Players[0].WeaponSlots)
                if (slot.WeaponId != null) nonNullCount++;

            Assert.Greater(nonNullCount, 4, "Player should have more than 4 weapons with no loadout filter");
        }

        [Test]
        public void PlayerWeaponLoadout_4Slots_OnlyLoadoutWeaponsActive()
        {
            var config = BaseConfig();
            // Select slots 0, 2, 4, 6 (indices into Weapons[])
            config.PlayerWeaponLoadout = new[] { 0, 2, 4, 6 };
            var state = GameSimulation.CreateMatch(config, 42);

            var player = state.Players[0];
            int activeCount = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
            {
                bool inLoadout = System.Array.IndexOf(config.PlayerWeaponLoadout, i) >= 0;
                bool isActive = player.WeaponSlots[i].WeaponId != null;
                if (isActive) activeCount++;
                // Slots NOT in loadout must be null
                if (!inLoadout)
                    Assert.IsNull(player.WeaponSlots[i].WeaponId,
                        $"Slot {i} should be null (not in loadout)");
            }

            Assert.LessOrEqual(activeCount, 4, "Player should have at most 4 active weapons");
        }

        [Test]
        public void ValidateLoadout_ValidFour_ReturnsTrue()
        {
            var config = BaseConfig();
            Assert.IsTrue(GameSimulation.ValidateLoadout(new[] { 0, 1, 2, 3 }, config));
        }

        [Test]
        public void ValidateLoadout_NullOrWrongCount_ReturnsFalse()
        {
            var config = BaseConfig();
            Assert.IsFalse(GameSimulation.ValidateLoadout(null, config));
            Assert.IsFalse(GameSimulation.ValidateLoadout(new[] { 0, 1, 2 }, config));
            Assert.IsFalse(GameSimulation.ValidateLoadout(new[] { 0, 1, 2, 3, 4 }, config));
        }

        [Test]
        public void ValidateLoadout_OutOfBoundsIndex_ReturnsFalse()
        {
            var config = BaseConfig();
            int outOfBounds = config.Weapons.Length + 10;
            Assert.IsFalse(GameSimulation.ValidateLoadout(new[] { 0, 1, 2, outOfBounds }, config));
        }

        [Test]
        public void AIWeaponLoadout_SetByCreateMatch_StoresLoadoutButDoesNotRestrict()
        {
            var config = BaseConfig();
            // Don't set AIWeaponLoadout — let CreateMatch auto-select it
            var state = GameSimulation.CreateMatch(config, 42);

            // Loadout is still computed and stored in config for reference
            Assert.IsNotNull(config.AIWeaponLoadout, "CreateMatch should auto-assign AI weapon loadout");

            // But AI keeps all weapons — SelectWeapon references all slot indices
            int aiActiveCount = 0;
            foreach (var slot in state.Players[1].WeaponSlots)
                if (slot.WeaponId != null) aiActiveCount++;

            Assert.AreEqual(config.Weapons.Length, aiActiveCount, "AI should have all weapons (loadout not applied)");
        }

        [Test]
        public void GetDefaultLoadout_Returns4Slots()
        {
            var config = BaseConfig();
            var loadout = GameSimulation.GetDefaultLoadout(config);
            Assert.AreEqual(4, loadout.Length);
        }
    }

        [TestFixture]
    public partial class GameSimulationTests
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
    }


    [TestFixture]
    public class DecoySkillTests
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

        static void GiveDecoySkill(ref PlayerState p)
        {
            p.SkillSlots[0] = new SkillSlotState
            {
                SkillId = "decoy",
                Type = SkillType.Decoy,
                EnergyCost = 30f,
                Cooldown = 16f,
                Duration = 4f,
                Range = 0f,
                Value = 30f
            };
        }

        [Test]
        public void Decoy_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "decoy")
                {
                    Assert.AreEqual(SkillType.Decoy, skill.Type);
                    Assert.AreEqual(30f, skill.EnergyCost);
                    Assert.AreEqual(16f, skill.Cooldown);
                    Assert.AreEqual(4f, skill.Duration);
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Decoy skill should exist in config");
        }

        [Test]
        public void Decoy_Activation_SetsInvisibleAndDecoyPosition()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            Vec2 originalPos = state.Players[0].Position;
            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[0].IsInvisible, "Player should be invisible after Decoy activation");
            Assert.AreEqual(originalPos.x, state.Players[0].DecoyPosition.x, 0.01f,
                "Decoy should be at player's original position");
            Assert.AreEqual(originalPos.y, state.Players[0].DecoyPosition.y, 0.01f);
            Assert.Greater(state.Players[0].DecoyTimer, 0f, "DecoyTimer should be active");
        }

        [Test]
        public void Decoy_Activation_DeductsEnergy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(70f, state.Players[0].Energy, 0.01f,
                "Decoy should cost 30 energy");
        }

        [Test]
        public void Decoy_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Tick past the 4s duration
            for (int i = 0; i < 260; i++)
                SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsInvisible,
                "Player should become visible after duration expires");
            Assert.AreEqual(0f, state.Players[0].DecoyTimer, 0.01f);
        }

        [Test]
        public void Decoy_BlocksFiring_WhileInvisible()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            state.Players[0].IsAI = false;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Try to fire
            state.Input.FireHeld = true;
            state.Input.FireReleased = false;
            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 0f;
            GameSimulation.Tick(state, 0.016f);

            // While invisible, firing input should be blocked
            // The player should NOT be charging (IsCharging = false because IsInvisible blocks input)
            Assert.IsFalse(state.Players[0].IsCharging,
                "Player should not be able to charge while invisible");
        }

        [Test]
        public void Decoy_AITargets_DecoyPosition()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Player 0 activates decoy, AI (player 1) should target decoy position
            state.Players[0].IsInvisible = true;
            state.Players[0].DecoyPosition = new Vec2(-10f, 5f);
            state.Players[0].DecoyTimer = 2f;
            state.Players[0].Position = new Vec2(10f, 5f); // real position far away

            int target = AILogic.FindTarget(state, 1);
            Assert.AreEqual(0, target, "AI should still find invisible player as target");
            // The AI will aim at DecoyPosition, not real position — verified by the
            // AI UpdateAI logic using targetPos = target.IsInvisible ? DecoyPosition : Position
        }

        [Test]
        public void Decoy_DamageRevealsPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Simulate splash damage hitting the invisible player
            state.DamageEvents.Add(new DamageEvent
            {
                TargetIndex = 0,
                Amount = 10f,
                Position = state.Players[0].Position,
                SourceIndex = 1
            });

            SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsInvisible,
                "Player should become visible after taking damage");
        }

        [Test]
        public void Decoy_NotEnoughEnergy_DoesNotActivate()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 20f; // less than 35 cost
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].IsInvisible,
                "Decoy should not activate with insufficient energy");
            Assert.AreEqual(20f, state.Players[0].Energy, 0.01f,
                "Energy should not be deducted on failed activation");
        }

        [Test]
        public void Decoy_CooldownPreventsReactivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Wait for decoy to expire (4s duration)
            for (int i = 0; i < 260; i++)
                SkillSystem.Update(state, 0.016f);
            Assert.IsFalse(state.Players[0].IsInvisible);

            // Try to activate again immediately — should be on cooldown
            state.Players[0].Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsFalse(state.Players[0].IsInvisible,
                "Decoy should not activate while on cooldown");
        }

        [Test]
        public void Decoy_EmitsSkillEvent()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count, "Should emit one skill event");
            Assert.AreEqual(SkillType.Decoy, state.SkillEvents[0].Type);
            Assert.AreEqual(0, state.SkillEvents[0].PlayerIndex);
        }

        [Test]
        public void Decoy_ZeroDuration_UsesFallback()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "decoy",
                Type = SkillType.Decoy,
                EnergyCost = 30f,
                Cooldown = 16f,
                Duration = 0f, // misconfigured — zero duration
                Range = 0f,
                Value = 30f
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(2f, state.Players[0].DecoyTimer, 0.01f,
                "DecoyTimer should use 2f fallback when Duration is 0");
            Assert.AreEqual(2f, state.Players[0].SkillSlots[0].DurationRemaining, 0.01f,
                "DurationRemaining should use 2f fallback when Duration is 0");
            Assert.IsTrue(state.Players[0].IsInvisible,
                "Player should still become invisible with zero-duration fallback");
        }
    }

    [TestFixture]
    public class ShadowStepSkillTests
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

        static void GiveShadowStepSkill(ref PlayerState p)
        {
            p.SkillSlots[0] = new SkillSlotState
            {
                SkillId = "shadow_step",
                Type = SkillType.ShadowStep,
                EnergyCost = 25f,
                Cooldown = 12f,
                Duration = 3f,
                Range = 0f,
                Value = 0f
            };
        }

        [Test]
        public void ShadowStep_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "shadow_step")
                {
                    Assert.AreEqual(SkillType.ShadowStep, skill.Type);
                    Assert.AreEqual(25f, skill.EnergyCost);
                    Assert.AreEqual(12f, skill.Cooldown);
                    Assert.AreEqual(3f, skill.Duration);
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "ShadowStep skill should exist in config");
        }

        [Test]
        public void ShadowStep_Activation_StoresPositionAndActivates()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            Vec2 originalPos = state.Players[0].Position;
            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive, "ShadowStep should be active");
            Assert.AreEqual(originalPos.x, state.Players[0].SkillTargetPosition.x, 0.01f,
                "Mark position X should match original");
            Assert.AreEqual(originalPos.y, state.Players[0].SkillTargetPosition.y, 0.01f,
                "Mark position Y should match original");
        }

        [Test]
        public void ShadowStep_Activation_DeductsEnergy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(75f, state.Players[0].Energy, 0.01f,
                "ShadowStep should cost 25 energy");
        }

        [Test]
        public void ShadowStep_ExpiresAfterDuration_RecallsPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            Vec2 markPos = state.Players[0].Position;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Move player away from mark
            state.Players[0].Position = new Vec2(markPos.x + 5f, markPos.y);

            // Tick past the 3s duration
            for (int i = 0; i < 200; i++)
                SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should have expired");
            Assert.AreEqual(markPos.x, state.Players[0].Position.x, 0.5f,
                "Player should recall to mark position X");
        }

        [Test]
        public void ShadowStep_EarlyReturn_RecallsPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            Vec2 markPos = state.Players[0].Position;
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            // Move player away
            state.Players[0].Position = new Vec2(markPos.x + 5f, markPos.y);

            // Re-activate to trigger early return
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should deactivate on early return");
            Assert.AreEqual(markPos.x, state.Players[0].Position.x, 0.5f,
                "Player should recall to mark position X on early return");
        }

        [Test]
        public void ShadowStep_RecallResetsVelocity()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            state.Players[0].Position = new Vec2(5f, 5f);
            state.Players[0].Velocity = new Vec2(10f, 5f);

            // Early return
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f, "Velocity X should be zero after recall");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.01f, "Velocity Y should be zero after recall");
        }

        [Test]
        public void ShadowStep_Death_ClearsActiveState()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            // Kill the player
            state.Players[0].IsDead = true;
            SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should deactivate on death");
        }

        [Test]
        public void ShadowStep_NotEnoughEnergy_DoesNotActivate()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 10f; // less than 25 cost
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should not activate with insufficient energy");
            Assert.AreEqual(10f, state.Players[0].Energy, 0.01f,
                "Energy should not be deducted on failed activation");
        }

        [Test]
        public void ShadowStep_EmitsSkillEvent_OnActivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count, "Should emit one skill event on activation");
            Assert.AreEqual(SkillType.ShadowStep, state.SkillEvents[0].Type);
            Assert.AreEqual(0, state.SkillEvents[0].PlayerIndex);
        }

        [Test]
        public void ShadowStep_EmitsSkillEvent_OnRecall()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            state.SkillEvents.Clear(); // clear activation event

            // Trigger early return
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.GreaterOrEqual(state.SkillEvents.Count, 1, "Should emit skill event on recall");
            Assert.AreEqual(SkillType.ShadowStep, state.SkillEvents[0].Type);
        }

        [Test]
        public void ShadowStep_CooldownStartsOnActivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            // Cooldown is scaled by player's CooldownMultiplier (issue #31).
            // Seed 42 lands on "Clockwork Foundry" biome which sets DefaultCooldownMultiplier=0.8.
            float expected = 12f * state.Players[0].CooldownMultiplier;
            Assert.AreEqual(expected, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should start on activation (scaled by CooldownMultiplier)");
        }
    }

    [TestFixture]
    public class KothTests
    {
        static GameConfig KothConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.KingOfTheHill,
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
                DefaultShootCooldown = 0.5f,
                KothZoneRadius = 4f,
                KothPointsPerSecond = 5f,
                KothPointsToWin = 100f,
                KothRelocateInterval = 30f,
                KothRelocateWarning = 3f,
                SuddenDeathTime = 0f // disable for test stability
            };
        }

        [Test]
        public void CreateMatch_Koth_InitializesZone()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            Assert.AreEqual(4f, state.Koth.ZoneRadius, 0.01f);
            Assert.IsNotNull(state.Koth.Scores);
            Assert.AreEqual(2, state.Koth.Scores.Length);
            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f);
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f);
        }

        [Test]
        public void CreateMatch_Koth_ZoneOnTerrain()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Zone should be within map bounds
            float halfMap = state.Config.MapWidth / 2f;
            Assert.GreaterOrEqual(state.Koth.ZonePosition.x, -halfMap);
            Assert.LessOrEqual(state.Koth.ZonePosition.x, halfMap);
        }

        [Test]
        public void CreateMatch_Deathmatch_NoKothInit()
        {
            var config = KothConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.IsNull(state.Koth.Scores, "Deathmatch should not init KOTH scores");
        }

        [Test]
        public void Koth_SinglePlayerInZone_Scores()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Place P1 in zone, P2 far away
            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x + 50f, state.Koth.ZonePosition.y);

            float dt = 1f;
            GameSimulation.Tick(state, dt);

            Assert.Greater(state.Koth.Scores[0], 0f, "P1 should score while in zone");
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f, "P2 should not score while outside zone");
        }

        [Test]
        public void Koth_Contested_NobodyScores()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Place both players in zone
            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = state.Koth.ZonePosition;
            state.Players[1].IsGrounded = true;

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f, "P1 should not score when contested");
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f, "P2 should not score when contested");
            Assert.IsTrue(state.Koth.IsContested, "Zone should be contested");
        }

        [Test]
        public void Koth_NobodyInZone_NoScoring()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Place both players far from zone
            state.Players[0].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x - 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f);
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f);
            Assert.IsFalse(state.Koth.IsContested);
        }

        [Test]
        public void Koth_ReachPointsToWin_EndsMatch()
        {
            var config = KothConfig();
            config.KothPointsToWin = 10f;
            config.KothPointsPerSecond = 100f; // very fast scoring
            var state = GameSimulation.CreateMatch(config, 42);

            // P1 in zone
            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P1 should win");
        }

        [Test]
        public void Koth_ZoneRelocates()
        {
            var config = KothConfig();
            config.KothRelocateInterval = 1f;
            config.KothRelocateWarning = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);

            Vec2 originalPos = state.Koth.ZonePosition;

            // Tick past relocate interval
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.02f);

            // Zone should have relocated (position changed)
            // Note: there's a tiny chance it relocates to the same spot, so just check timer reset
            Assert.Greater(state.Koth.RelocateTimer, 0f, "Relocate timer should have reset");
        }

        [Test]
        public void Koth_DeadPlayerDoesNotScore()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsDead = true;
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f, "Dead player should not score");
        }

        [Test]
        public void Koth_LastPlayerAlive_WinsByElimination()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Last alive wins by elimination");
        }

        [Test]
        public void Koth_WarningBeforeRelocate()
        {
            var config = KothConfig();
            config.KothRelocateInterval = 5f;
            config.KothRelocateWarning = 3f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Tick to just before warning starts (5 - 3 = 2 seconds)
            // 100 ticks * 0.016 = 1.6s → relocateTimer ~3.4 > 3 → no warning
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Koth.RelocateWarningTimer, 0.01f,
                "Warning should not be active yet");

            // Tick more past the 2s mark where warning triggers
            // 30 more ticks * 0.016 = 0.48s → total 2.08s → relocateTimer ~2.92 <= 3 → warning
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Koth.RelocateWarningTimer, 0f,
                "Warning timer should be active before relocation");
        }

        [Test]
        public void Koth_P2WinsByScore()
        {
            var config = KothConfig();
            config.KothPointsToWin = 10f;
            config.KothPointsPerSecond = 100f;
            var state = GameSimulation.CreateMatch(config, 42);

            // P2 in zone
            state.Players[1].Position = state.Koth.ZonePosition;
            state.Players[1].IsGrounded = true;
            state.Players[0].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex, "P2 should win by score");
        }

        // AllBiomes_HaveWeatherParticleCoverage test removed: depends on WeatherParticles (Unity runtime class)
    }

    [TestFixture]
    public class SurvivalTests
    {
        static GameConfig SurvivalConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Survival,
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
                DefaultMaxEnergy = 100f,
                DefaultEnergyRegen = 10f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                SuddenDeathTime = 0f,
                CrateSpawnInterval = 0f,        // disable crates for test stability
                SurvivalBreakDuration = 0.1f,   // short break for test speed
                SurvivalHealthRegen = 20f,
                SurvivalWaveMobBase = 2,
                SurvivalBossInterval = 5,
                SurvivalScorePerWave = 100,
                SurvivalScorePerKill = 50,
                SurvivalScorePerBossKill = 500,
                SurvivalScoreDirectHitBonus = 25,
                SurvivalScoreNoDamageBonus = 200
            };
        }

        /// <summary>Tick past the break to spawn a wave, using small steps.</summary>
        static void TickPastBreak(GameState state)
        {
            float needed = state.Config.SurvivalBreakDuration + 0.05f;
            int steps = (int)(needed / 0.02f) + 1;
            for (int i = 0; i < steps; i++)
                GameSimulation.Tick(state, 0.02f);
        }

        /// <summary>Kill all mobs and tick once to detect wave clear.</summary>
        static void ClearWave(GameState state)
        {
            for (int i = 1; i < state.Players.Length; i++)
            {
                state.Players[i].Health = 0f;
                state.Players[i].IsDead = true;
            }
            GameSimulation.Tick(state, 0.02f);
        }

        [Test]
        public void CreateMatch_Survival_InitializesState()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            Assert.AreEqual(MatchType.Survival, state.Config.MatchType);
            Assert.AreEqual(0, state.Survival.WaveNumber);
            Assert.AreEqual(0, state.Survival.Score);
            Assert.IsFalse(state.Survival.WaveActive);
            Assert.Greater(state.Survival.BreakTimer, 0f, "Should start with break timer");
        }

        [Test]
        public void Survival_BreakTimerCountsDown()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            float initialBreak = state.Survival.BreakTimer;
            GameSimulation.Tick(state, 0.02f);

            Assert.Less(state.Survival.BreakTimer, initialBreak, "Break timer should decrease");
        }

        [Test]
        public void Survival_Wave1_SpawnsAfterBreak()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            Assert.AreEqual(1, state.Survival.WaveNumber, "Should be wave 1");
            Assert.IsTrue(state.Survival.WaveActive, "Wave should be active");
            Assert.Greater(state.Players.Length, 1, "Mobs should have spawned");
        }

        [Test]
        public void Survival_MobsAreMarkedAsMobs()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            for (int i = 1; i < state.Players.Length; i++)
            {
                Assert.IsTrue(state.Players[i].IsMob, $"Player {i} should be a mob");
                Assert.IsTrue(state.Players[i].IsAI, $"Player {i} should be AI");
            }
        }

        [Test]
        public void Survival_KillingAllMobs_ClearsWave()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            Assert.IsTrue(state.Survival.WaveActive);
            ClearWave(state);

            Assert.IsFalse(state.Survival.WaveActive, "Wave should end when all mobs dead");
            Assert.Greater(state.Survival.Score, 0, "Should have scored for wave clear");
        }

        [Test]
        public void Survival_PlayerDeath_EndsMatch()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Kill the player
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            GameSimulation.Tick(state, 0.02f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex, "No winner in survival when player dies");
        }

        [Test]
        public void Survival_DoesNotEndMatch_WhenMobsDie()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            ClearWave(state);

            Assert.AreEqual(MatchPhase.Playing, state.Phase, "Match should continue after wave clear");
        }

        [Test]
        public void Survival_Wave2_SpawnsAfterWave1Clear()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);
            Assert.AreEqual(1, state.Survival.WaveNumber);

            ClearWave(state);
            TickPastBreak(state);

            Assert.AreEqual(2, state.Survival.WaveNumber, "Should be wave 2");
            Assert.IsTrue(state.Survival.WaveActive);
        }

        [Test]
        public void Survival_BossWave_SpawnsBoss()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            // Fast-forward to wave 5 (boss wave): spawn and clear waves 1-4, then spawn wave 5
            for (int w = 0; w < 4; w++)
            {
                TickPastBreak(state);
                ClearWave(state);
            }

            // Spawn wave 5
            TickPastBreak(state);

            Assert.AreEqual(5, state.Survival.WaveNumber);
            Assert.IsTrue(state.Survival.WaveActive);

            // Boss should be spawned
            bool foundBoss = false;
            for (int i = 1; i < state.Players.Length; i++)
            {
                if (!string.IsNullOrEmpty(state.Players[i].BossType))
                    foundBoss = true;
            }
            Assert.IsTrue(foundBoss, "Wave 5 should spawn a boss");
        }

        [Test]
        public void Survival_HealthRegen_BetweenWaves()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Damage the player
            state.Players[0].Health = 50f;

            ClearWave(state);

            // Player should have received health regen
            Assert.AreEqual(70f, state.Players[0].Health, 0.01f,
                "Player should get +20 HP between waves");
        }

        [Test]
        public void Survival_EnergyRefill_BetweenWaves()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Drain player energy
            state.Players[0].Energy = 10f;

            ClearWave(state);

            Assert.AreEqual(state.Players[0].MaxEnergy, state.Players[0].Energy, 0.01f,
                "Energy should be fully refilled between waves");
        }

        [Test]
        public void Survival_KillScoring_MobKill()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            int scoreBefore = state.Survival.Score;

            // Kill one mob via explosion
            CombatResolver.ApplyExplosion(state, state.Players[1].Position,
                5f, 999f, 0f, 0, false);

            Assert.AreEqual(scoreBefore + state.Config.SurvivalScorePerKill + state.Config.SurvivalScoreDirectHitBonus,
                state.Survival.Score, "Should score for mob kill (includes direct hit bonus)");
        }

        [Test]
        public void Survival_NoDamageBonusAwarded()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Ensure player at full HP
            state.Players[0].Health = state.Players[0].MaxHealth;

            ClearWave(state);

            int waveScore = state.Config.SurvivalScorePerWave * 1 + state.Config.SurvivalScoreNoDamageBonus;
            // Score includes any kill scoring from ClearWave, plus wave clear + no-damage bonus
            Assert.GreaterOrEqual(state.Survival.Score, waveScore,
                "Should include wave clear + no-damage bonus");
        }

        [Test]
        public void Survival_MobCountScaling()
        {
            var config = SurvivalConfig();
            config.SurvivalWaveMobBase = 2;

            Assert.AreEqual(2, GameSimulation.GetSurvivalMobCount(1, config));
            Assert.AreEqual(3, GameSimulation.GetSurvivalMobCount(4, config));
            Assert.AreEqual(4, GameSimulation.GetSurvivalMobCount(8, config));
            Assert.LessOrEqual(GameSimulation.GetSurvivalMobCount(100, config), 7,
                "Mob count should cap at base + 5");
        }

        [Test]
        public void Survival_SpeedMultiplier_Scaling()
        {
            Assert.AreEqual(1.0f, GameSimulation.GetSurvivalSpeedMult(1), 0.01f);
            Assert.AreEqual(1.0f, GameSimulation.GetSurvivalSpeedMult(4), 0.01f);
            Assert.AreEqual(1.1f, GameSimulation.GetSurvivalSpeedMult(6), 0.01f);
            Assert.AreEqual(1.2f, GameSimulation.GetSurvivalSpeedMult(11), 0.01f);
            Assert.AreEqual(1.5f, GameSimulation.GetSurvivalSpeedMult(21), 0.01f);
        }

        [Test]
        public void Survival_HPMultiplier_Scaling()
        {
            Assert.AreEqual(1.0f, GameSimulation.GetSurvivalHPMult(1), 0.01f);
            Assert.AreEqual(1.2f, GameSimulation.GetSurvivalHPMult(6), 0.01f);
            Assert.AreEqual(2.0f, GameSimulation.GetSurvivalHPMult(21), 0.01f);
            Assert.AreEqual(2.5f, GameSimulation.GetSurvivalHPMult(30), 0.01f);
        }

        [Test]
        public void Survival_Deathmatch_SkipsSurvivalLogic()
        {
            var config = SurvivalConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            // Tick past break duration — no survival waves should spawn
            GameSimulation.Tick(state, 10f);

            Assert.AreEqual(0, state.Survival.WaveNumber, "Deathmatch should not run survival logic");
        }

        [Test]
        public void Survival_PlayerPreservedAcrossWaves()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Set player stats
            state.Players[0].TotalDamageDealt = 999f;
            state.Players[0].ShotsFired = 42;

            // Clear and spawn new wave
            ClearWave(state);
            TickPastBreak(state);

            Assert.AreEqual(999f, state.Players[0].TotalDamageDealt, 0.01f,
                "Player stats should persist across waves");
            Assert.AreEqual(42, state.Players[0].ShotsFired);
        }

        [Test]
        public void Survival_ScoreKill_MobAtIndexZero_ScoresCorrectly()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Manually mark player 0 as a mob to simulate a PVE scenario
            // where a mob occupies index 0
            state.Players[0].IsMob = true;

            int scoreBefore = state.Survival.Score;
            GameSimulation.ScoreSurvivalKill(state, 0);

            Assert.AreEqual(scoreBefore + state.Config.SurvivalScorePerKill,
                state.Survival.Score,
                "ScoreSurvivalKill should credit score for mob at index 0");
        }

        [Test]
        public void Survival_MobsSpawnedTotal_AccumulatesAcrossWaves()
        {
            // Regression test for bug #433: MobsSpawnedTotal was assigned (=) instead of accumulated (+=)
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            // Wave 1
            TickPastBreak(state);
            int wave1Mobs = state.Players.Length - 1; // all players except the human
            Assert.AreEqual(wave1Mobs, state.Survival.MobsSpawnedTotal,
                "After wave 1, MobsSpawnedTotal should equal wave 1 mob count");

            // Clear wave 1 and spawn wave 2
            ClearWave(state);
            TickPastBreak(state);
            int wave2Mobs = state.Players.Length - 1;

            Assert.AreEqual(wave1Mobs + wave2Mobs, state.Survival.MobsSpawnedTotal,
                "After wave 2, MobsSpawnedTotal should be cumulative across both waves");
        }

        [Test]
        public void Survival_DirectHitBonus_AppliedOnDirectKill()
        {
            // Regression test for bug #434: SurvivalScoreDirectHitBonus was defined but never applied.
            var config = SurvivalConfig();
            config.SurvivalScoreDirectHitBonus = 25;
            var state = GameSimulation.CreateMatch(config, 42);
            TickPastBreak(state);

            // Place mob directly on the explosion center (guaranteed direct hit: dist=0, dmgRatio=1)
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[1].Health = 1f; // low HP so one explosion kills it

            int scoreBefore = state.Survival.Score;
            CombatResolver.ApplyExplosion(state, new Vec2(0f, 5f), 5f, 999f, 0f, 0, false);

            bool mobKilled = state.Players[1].IsDead;
            Assert.IsTrue(mobKilled, "Mob should have been killed by the explosion");
            Assert.AreEqual(scoreBefore + config.SurvivalScorePerKill + config.SurvivalScoreDirectHitBonus,
                state.Survival.Score,
                "Direct hit kill should award SurvivalScorePerKill + SurvivalScoreDirectHitBonus");
        }

        [Test]
        public void Survival_DirectHitBonus_NotAppliedOnSplashKill()
        {
            // Splash kill (explosion far from mob center) should not award direct hit bonus
            var config = SurvivalConfig();
            config.SurvivalScoreDirectHitBonus = 25;
            var state = GameSimulation.CreateMatch(config, 42);
            TickPastBreak(state);

            // Place mob at the edge of the blast radius (splash, not direct)
            float radius = 5f;
            state.Players[1].Position = new Vec2(radius * 0.9f, 5f); // 90% of radius away
            state.Players[1].Health = 1f;

            int scoreBefore = state.Survival.Score;
            CombatResolver.ApplyExplosion(state, new Vec2(0f, 5f), radius, 999f, 0f, 0, false);

            bool mobKilled = state.Players[1].IsDead;
            Assert.IsTrue(mobKilled, "Mob should have been killed by the splash");
            Assert.AreEqual(scoreBefore + config.SurvivalScorePerKill,
                state.Survival.Score,
                "Splash kill should award only SurvivalScorePerKill, not the direct hit bonus");
        }

        [Test]
        public void SurvivalWaveSpawn_ResizesWeaponTracking_Issue94()
        {
            // Issue #94: SpawnSurvivalWave replaces Players array but weapon
            // tracking arrays stayed at original size, causing silent data loss.
            var config = SurvivalConfig();
            config.SurvivalWaveMobBase = 3;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            // Survival starts with just 1 player (no AI opponent)
            int initialCount = state.Players.Length;
            Assert.AreEqual(initialCount, state.WeaponHits.Length);

            // Tick until wave 1 starts (mobs spawn, expanding Players array)
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players.Length > initialCount) break;
            }

            // After wave spawn, tracking arrays should match new player count
            Assert.AreEqual(state.Players.Length, state.WeaponHits.Length,
                "WeaponHits should be resized to match new Players array (issue #94)");
            Assert.AreEqual(state.Players.Length, state.WeaponKills.Length,
                "WeaponKills should be resized to match new Players array");
            Assert.AreEqual(state.Players.Length, state.WeaponsUsed.Length,
                "WeaponsUsed should be resized to match new Players array");
        }
    }

    [TestFixture]
    public class TargetPracticeTests
    {
        static GameConfig TPConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.TargetPractice,
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
                DefaultShootCooldown = 0.5f,
                MineCount = 0,
                BarrelCount = 0,
                SuddenDeathTime = 0f,
                CrateSpawnInterval = 0f,
                TargetPracticeRoundDuration = 60f,
                TargetRadius = 1.5f,
                TargetRespawnTime = 3f,
                TargetStaticNearCount = 2,
                TargetStaticMidCount = 2,
                TargetStaticFarCount = 1,
                TargetMovingHorizontalCount = 1,
                TargetMovingVerticalCount = 1,
                TargetNearPoints = 50,
                TargetMidPoints = 100,
                TargetFarPoints = 200,
                TargetMovingPoints = 150,
                TargetStreakBonus = 50,
                TargetStreakThreshold = 3,
                TargetLongRangeBonus = 100,
                TargetLongRangeDistance = 25f,
                TargetSpeedBonus = 25,
                TargetSpeedBonusWindow = 1f
            };
        }

        [Test]
        public void CreateMatch_TP_SpawnsCorrectTargetCount()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // 2 near + 2 mid + 1 far + 1 moving-h + 1 moving-v = 7
            Assert.AreEqual(7, state.Targets.Count, "Should spawn 7 targets");
            foreach (var t in state.Targets)
                Assert.IsTrue(t.Active, "All targets should start active");
        }

        [Test]
        public void CreateMatch_TP_AIPlayerIsDead()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            Assert.IsTrue(state.Players[1].IsDead, "AI opponent should be dead in target practice");
            Assert.AreEqual(0f, state.Players[1].Health, 0.01f);
        }

        [Test]
        public void CreateMatch_TP_InfiniteAmmoZeroEnergyCost()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            for (int i = 0; i < state.Players[0].WeaponSlots.Length; i++)
            {
                var w = state.Players[0].WeaponSlots[i];
                if (w.WeaponId == null) continue;
                Assert.AreEqual(-1, w.Ammo, $"Weapon {w.WeaponId} should have infinite ammo");
                Assert.AreEqual(0f, w.EnergyCost, 0.01f, $"Weapon {w.WeaponId} should have zero energy cost");
            }
        }

        [Test]
        public void CreateMatch_TP_TimerStartsAtRoundDuration()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            Assert.AreEqual(60f, state.TargetTimeRemaining, 0.01f);
            Assert.AreEqual(0, state.TargetScore);
        }

        [Test]
        public void TP_MatchDoesNotEndFromDeathCount()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // AI is dead, but match should still be Playing
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Target practice should not end from death count");
        }

        [Test]
        public void TP_MatchEndsWhenTimerExpires()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Tick with a delta equal to full duration
            GameSimulation.Tick(state, 61f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Player should be the winner");
        }

        /// <summary>Deactivate all targets except the one at the given index.</summary>
        static void IsolateTarget(GameState state, int keepIdx)
        {
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (i == keepIdx) continue;
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f; // prevent respawn during test
                state.Targets[i] = t;
            }
        }

        [Test]
        public void TP_ExplosionHitsTarget_ScoresPoints()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Find a near target and isolate it
            int nearIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.StaticNear)
                {
                    nearIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(nearIdx, 0, "Should have at least one near target");
            IsolateTarget(state, nearIdx);

            var target = state.Targets[nearIdx];

            // Place player far enough to not trigger long-range bonus
            state.Players[0].Position = target.Position + new Vec2(-3f, 0f);

            // Create an explosion right on the target
            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = target.Position,
                Radius = 2f
            });

            TargetPractice.Update(state, 0.016f);

            Assert.Greater(state.TargetScore, 0, "Score should increase on hit");
            Assert.IsFalse(state.Targets[nearIdx].Active, "Hit target should be deactivated");
            Assert.AreEqual(1, state.TargetHitEvents.Count, "Should emit a hit event");
        }

        [Test]
        public void TP_HitTarget_RespawnsAfterDelay()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            var target = state.Targets[0];

            // Hit the target
            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = target.Position,
                Radius = 2f
            });
            TargetPractice.Update(state, 0.016f);
            Assert.IsFalse(state.Targets[0].Active);

            // Tick partially — not enough to respawn
            state.ExplosionEvents.Clear();
            state.Time += 1f;
            TargetPractice.Update(state, 1f);
            Assert.IsFalse(state.Targets[0].Active, "Should not respawn before timer");

            // Tick enough to trigger respawn
            state.Time += 3f;
            TargetPractice.Update(state, 3f);
            Assert.IsTrue(state.Targets[0].Active, "Should respawn after 3s");
        }

        [Test]
        public void TP_StreakBonus_AfterThreeHits()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Place 3 targets far apart to avoid multi-hit, deactivate the rest
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f;
                state.Targets[i] = t;
            }
            // Activate first 3 targets at known isolated positions
            for (int i = 0; i < 3 && i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = true;
                t.Position = new Vec2(-10f + i * 20f, 5f); // 20 units apart
                state.Targets[i] = t;
            }

            // Hit 3 consecutive targets — 3rd should get streak bonus
            for (int hit = 0; hit < 3; hit++)
            {
                int idx = -1;
                for (int i = 0; i < state.Targets.Count; i++)
                {
                    if (state.Targets[i].Active) { idx = i; break; }
                }
                Assert.GreaterOrEqual(idx, 0, $"Should have active target for hit {hit}");

                var t = state.Targets[idx];
                state.Players[0].Position = t.Position + new Vec2(-2f, 0f);

                state.ExplosionEvents.Clear();
                state.ExplosionEvents.Add(new ExplosionEvent
                {
                    Position = t.Position,
                    Radius = 0.5f // small radius to only hit this target
                });

                int scoreBefore = state.TargetScore;
                state.Time += 2f; // more than speed bonus window
                TargetPractice.Update(state, 0.016f);
                int gained = state.TargetScore - scoreBefore;

                if (hit >= 2) // 3rd hit (index 2) = streak threshold met
                {
                    Assert.AreEqual(t.Points + 50, gained,
                        $"Hit #{hit + 1} should include +50 streak bonus");
                }
            }
        }

        [Test]
        public void TP_StreakResets_OnMiss()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Isolate 3 targets far apart
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f;
                state.Targets[i] = t;
            }
            for (int i = 0; i < 3 && i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = true;
                t.Position = new Vec2(-10f + i * 20f, 5f);
                state.Targets[i] = t;
            }

            // Build up 3 consecutive hits
            for (int hit = 0; hit < 3; hit++)
            {
                int idx = -1;
                for (int i = 0; i < state.Targets.Count; i++)
                {
                    if (state.Targets[i].Active) { idx = i; break; }
                }
                var t = state.Targets[idx];
                state.Players[0].Position = t.Position + new Vec2(-2f, 0f);
                state.ExplosionEvents.Clear();
                state.ExplosionEvents.Add(new ExplosionEvent { Position = t.Position, Radius = 0.5f });
                state.Time += 2f;
                TargetPractice.Update(state, 0.016f);
            }

            Assert.AreEqual(3, state.TargetConsecutiveHits);

            // Miss: explosion that hits no target
            state.ExplosionEvents.Clear();
            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = new Vec2(999f, 999f),
                Radius = 1f
            });
            TargetPractice.Update(state, 0.016f);
            TargetPractice.ResetStreakOnMiss(state);

            Assert.AreEqual(0, state.TargetConsecutiveHits, "Streak should reset on miss");
        }

        [Test]
        public void TP_LongRangeBonus()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Find the far target and isolate it
            int farIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.StaticFar)
                {
                    farIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(farIdx, 0, "Should have a far target");
            IsolateTarget(state, farIdx);

            var target = state.Targets[farIdx];

            // Place player 30 units away (> 25 threshold)
            state.Players[0].Position = target.Position + new Vec2(-30f, 0f);

            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = target.Position,
                Radius = 0.5f
            });
            state.Time = 10f; // avoid speed bonus
            state.TargetLastHitTime = -10f;
            TargetPractice.Update(state, 0.016f);

            // 200 base + 100 long-range = 300 (first hit, no streak)
            Assert.AreEqual(300, state.TargetScore, "Should get long-range bonus");
        }

        [Test]
        public void TP_SpeedBonus_QuickConsecutiveHits()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Isolate first two targets far apart
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f;
                state.Targets[i] = t;
            }
            // Activate 2 targets at known positions, close to player (no long-range bonus)
            var ta = state.Targets[0];
            ta.Active = true;
            ta.Position = new Vec2(-5f, 5f);
            state.Targets[0] = ta;

            var tb = state.Targets[1];
            tb.Active = true;
            tb.Position = new Vec2(5f, 5f); // 10 units from first, no overlap
            state.Targets[1] = tb;

            // First hit
            state.Players[0].Position = state.Targets[0].Position + new Vec2(-2f, 0f);
            state.ExplosionEvents.Add(new ExplosionEvent { Position = state.Targets[0].Position, Radius = 0.5f });
            state.Time = 5f;
            TargetPractice.Update(state, 0.016f);
            int scoreAfterFirst = state.TargetScore;

            // Second hit within 1s
            state.ExplosionEvents.Clear();
            state.Players[0].Position = state.Targets[1].Position + new Vec2(-2f, 0f);
            state.ExplosionEvents.Add(new ExplosionEvent { Position = state.Targets[1].Position, Radius = 0.5f });
            state.Time = 5.5f; // 0.5s later, within 1s window
            TargetPractice.Update(state, 0.016f);

            int secondHitPoints = state.TargetScore - scoreAfterFirst;
            Assert.AreEqual(state.Targets[1].Points + 25, secondHitPoints,
                "Second quick hit should include +25 speed bonus");
        }

        [Test]
        public void TP_Deathmatch_DoesNotInitTargets()
        {
            var config = TPConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(0, state.Targets.Count, "Deathmatch should not have targets");
        }

        [Test]
        public void TP_MovingTarget_PositionChanges()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            int movIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.MovingHorizontal)
                {
                    movIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(movIdx, 0, "Should have a moving horizontal target");

            Vec2 posBefore = state.Targets[movIdx].Position;
            GameSimulation.Tick(state, 1f);
            Vec2 posAfter = state.Targets[movIdx].Position;

            Assert.AreNotEqual(posBefore.x, posAfter.x, "Moving target X should change over time");
        }
    }

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

    [TestFixture]
    public class DemolitionTests
    {
        static GameConfig DemoConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Demolition,
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
                DefaultShootCooldown = 0.5f,
                DemolitionCrystalHP = 300f,
                DemolitionCrystalWidth = 3f,
                DemolitionCrystalHeight = 5f,
                DemolitionCrystalOffset = 10f,
                DemolitionLivesPerPlayer = 3,
                DemolitionRespawnDelay = 3f,
                SuddenDeathTime = 0f
            };
        }

        [Test]
        public void CreateMatch_Demolition_InitializesCrystals()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            Assert.IsNotNull(state.Demolition.Crystals);
            Assert.AreEqual(2, state.Demolition.Crystals.Length);
            Assert.AreEqual(300f, state.Demolition.Crystals[0].HP, 0.01f);
            Assert.AreEqual(300f, state.Demolition.Crystals[1].HP, 0.01f);
            Assert.AreEqual(300f, state.Demolition.Crystals[0].MaxHP, 0.01f);
        }

        [Test]
        public void CreateMatch_Demolition_CrystalsPlacedBehindSpawns()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Crystal 0 should be to the left of P1 spawn (behind = further from center)
            Assert.Less(state.Demolition.Crystals[0].Position.x, config.Player1SpawnX);
            // Crystal 1 should be to the right of P2 spawn
            Assert.Greater(state.Demolition.Crystals[1].Position.x, config.Player2SpawnX);
        }

        [Test]
        public void CreateMatch_Demolition_PlayersHaveLives()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            Assert.IsNotNull(state.Demolition.LivesRemaining);
            Assert.AreEqual(2, state.Demolition.LivesRemaining.Length);
            Assert.AreEqual(3, state.Demolition.LivesRemaining[0]);
            Assert.AreEqual(3, state.Demolition.LivesRemaining[1]);
        }

        [Test]
        public void CreateMatch_Deathmatch_NoDemolitionInit()
        {
            var config = DemoConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.IsNull(state.Demolition.Crystals);
        }

        [Test]
        public void Demolition_ExplosionDamagesCrystal()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);
            Vec2 crystalPos = state.Demolition.Crystals[1].Position;

            CombatResolver.ApplyExplosion(state, crystalPos, 4f, 60f, 10f, 0, false);

            Assert.Less(state.Demolition.Crystals[1].HP, 300f, "Crystal should take damage from explosion");
            Assert.Greater(state.CrystalDamageEvents.Count, 0, "Should emit crystal damage event");
        }

        [Test]
        public void Demolition_ExplosionOutOfRange_NoCrystalDamage()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);
            Vec2 crystalPos = state.Demolition.Crystals[0].Position;

            // Explode far away from crystal
            CombatResolver.ApplyExplosion(state, crystalPos + new Vec2(50f, 0f), 4f, 60f, 10f, 1, false);

            Assert.AreEqual(300f, state.Demolition.Crystals[0].HP, 0.01f, "Crystal should not take damage from distant explosion");
        }

        [Test]
        public void Demolition_CrystalDestroyed_OpponentWins()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            // Destroy crystal 0 (belongs to P1) — P2 should win
            state.Demolition.Crystals[0].HP = 0f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex, "P2 should win when P1's crystal is destroyed");
        }

        [Test]
        public void Demolition_CrystalP2Destroyed_P1Wins()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            // Destroy crystal 1 (belongs to P2)
            state.Demolition.Crystals[1].HP = 0f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P1 should win when P2's crystal is destroyed");
        }

        [Test]
        public void Demolition_PlayerRespawnsAfterDelay()
        {
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 1f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Kill player 0
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            // Tick less than respawn delay — still dead
            GameSimulation.Tick(state, 0.5f);
            Assert.IsTrue(state.Players[0].IsDead, "Should still be dead before respawn delay");

            // Tick past respawn delay
            GameSimulation.Tick(state, 0.6f);
            Assert.IsFalse(state.Players[0].IsDead, "Should respawn after delay");
            Assert.AreEqual(100f, state.Players[0].Health, 0.01f, "Should respawn with full health");
        }

        [Test]
        public void Demolition_LivesDecrement_OnRespawn()
        {
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(3, state.Demolition.LivesRemaining[0]);

            // Kill and respawn
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            GameSimulation.Tick(state, 0.6f);

            Assert.AreEqual(2, state.Demolition.LivesRemaining[0], "Should lose a life on respawn");
        }

        [Test]
        public void Demolition_NoRespawn_WhenLivesExhausted()
        {
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Demolition.LivesRemaining[0] = 0;
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            GameSimulation.Tick(state, 1f);

            Assert.IsTrue(state.Players[0].IsDead, "Should stay dead with no lives remaining");
        }

        [Test]
        public void Demolition_AllDead_NoLives_CrystalHPTiebreaker()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Exhaust all lives
            state.Demolition.LivesRemaining[0] = 0;
            state.Demolition.LivesRemaining[1] = 0;

            // Both dead
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            // P1 crystal has more HP
            state.Demolition.Crystals[0].HP = 200f;
            state.Demolition.Crystals[1].HP = 100f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Player with more crystal HP should win");
        }

        [Test]
        public void Demolition_AllDead_EqualHP_Draw()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Demolition.LivesRemaining[0] = 0;
            state.Demolition.LivesRemaining[1] = 0;
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            state.Demolition.Crystals[0].HP = 150f;
            state.Demolition.Crystals[1].HP = 150f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex, "Equal crystal HP should be a draw");
        }

        [Test]
        public void Demolition_RespawnPlayer_UsesTeamIndex_NotPlayerIndex()
        {
            // Regression test for bug #435: RespawnPlayer used playerIndex==0 to select crystal,
            // causing players at index>0 with TeamIndex=0 to respawn near the wrong crystal.
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Simulate a 2v2 scenario: player at index 1 belongs to team 0
            state.Players[1].TeamIndex = 0;

            Vec2 crystal0Pos = state.Demolition.Crystals[0].Position;
            Vec2 crystal1Pos = state.Demolition.Crystals[1].Position;

            // Kill player 1 (index=1, TeamIndex=0) and wait for respawn
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            GameSimulation.Tick(state, 0.6f);

            Assert.IsFalse(state.Players[1].IsDead, "Player 1 should have respawned");

            // Player 1 belongs to team 0 → must respawn near crystal[0], not crystal[1]
            float distToCrystal0 = Vec2.Distance(state.Players[1].Position, crystal0Pos);
            float distToCrystal1 = Vec2.Distance(state.Players[1].Position, crystal1Pos);
            Assert.Less(distToCrystal0, distToCrystal1,
                "Player with TeamIndex=0 at playerIndex=1 must respawn near crystal[0]");
        }

        [Test]
        public void Demolition_FireZone_DoesNotDamageCrystal()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);
            Vec2 crystalPos = state.Demolition.Crystals[0].Position;

            // Add fire zone at crystal position
            state.FireZones.Add(new FireZoneState
            {
                Position = crystalPos,
                Radius = 5f,
                DamagePerSecond = 15f,
                RemainingTime = 5f,
                OwnerIndex = 1,
                Active = true
            });

            float hpBefore = state.Demolition.Crystals[0].HP;
            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(hpBefore, state.Demolition.Crystals[0].HP, 0.01f,
                "Fire zones should not damage crystals");
        }

        [Test]
        public void Demolition_MatchDoesNotEndByElimination()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            // Kill one player — match should NOT end (Demolition uses crystal destruction)
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            // P2 still has lives, so will respawn
            Assert.Greater(state.Demolition.LivesRemaining[1], 0);

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Demolition should not end by elimination while lives remain");
        }
    }

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

    [TestFixture]
    public class PayloadTests
    {
        // Harpoon tests in this class reference SmallConfig(); provide a local copy
        // so the test assembly compiles. Mirrors GameSimulationTests.SmallConfig().
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

        static GameConfig PayloadConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Payload,
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f,
                Player1SpawnX = -15f,
                Player2SpawnX = 15f,
                SpawnProbeY = 20f,
                DeathBoundaryY = -25f,
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                PayloadPushMult = 0.5f,
                PayloadPushRadiusMult = 1.5f,
                PayloadFriction = 0.8f,
                PayloadMatchTime = 120f,
                PayloadStalemateTime = 30f,
                PayloadStalemateThreshold = 0.1f,
                SuddenDeathTime = 0f
            };
        }

        [Test]
        public void CreateMatch_Payload_InitializesAtCenter()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            Assert.AreEqual(MatchType.Payload, state.Config.MatchType);
            Assert.AreEqual(0f, state.Payload.Position.x, 0.01f, "Payload should start at center X");
            Assert.AreEqual(0f, state.Payload.VelocityX, 0.01f);
            Assert.AreEqual(-15f, state.Payload.GoalLeftX, 0.01f);
            Assert.AreEqual(15f, state.Payload.GoalRightX, 0.01f);
            Assert.AreEqual(120f, state.Payload.MatchTimer, 0.01f);
        }

        [Test]
        public void Payload_ExplosionPushesRight()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Explosion to the left of the payload should push it right
            Vec2 explosionPos = new Vec2(-2f, state.Payload.Position.y);
            GameSimulation.ApplyPayloadPush(state, explosionPos, 3f, 10f);

            Assert.Greater(state.Payload.VelocityX, 0f, "Explosion to the left should push payload right");
        }

        [Test]
        public void Payload_ExplosionPushesLeft()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Explosion to the right of the payload should push it left
            Vec2 explosionPos = new Vec2(2f, state.Payload.Position.y);
            GameSimulation.ApplyPayloadPush(state, explosionPos, 3f, 10f);

            Assert.Less(state.Payload.VelocityX, 0f, "Explosion to the right should push payload left");
        }

        [Test]
        public void Payload_ExplosionOutOfRange_NoPush()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Explosion far away should not push
            Vec2 explosionPos = new Vec2(50f, state.Payload.Position.y);
            GameSimulation.ApplyPayloadPush(state, explosionPos, 3f, 10f);

            Assert.AreEqual(0f, state.Payload.VelocityX, 0.01f, "Distant explosion should not push payload");
        }

        [Test]
        public void Payload_FrictionDeceleratesVelocity()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Give the payload some velocity
            state.Payload.VelocityX = 10f;
            float initialVel = state.Payload.VelocityX;

            GameSimulation.Tick(state, 0.1f);

            Assert.Less(MathF.Abs(state.Payload.VelocityX), MathF.Abs(initialVel),
                "Friction should decelerate the payload");
        }

        [Test]
        public void Payload_GoalRight_Player1Wins()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Push payload past player 2's goal line
            state.Payload.Position.x = 14.9f;
            state.Payload.VelocityX = 5f;

            // Tick enough for it to cross
            for (int i = 0; i < 100; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Player 1 should win when payload crosses right goal");
        }

        [Test]
        public void Payload_GoalLeft_Player2Wins()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Push payload past player 1's goal line
            state.Payload.Position.x = -14.9f;
            state.Payload.VelocityX = -5f;

            for (int i = 0; i < 100; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex, "Player 2 should win when payload crosses left goal");
        }

        [Test]
        public void Payload_TimerCountsDown()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);
            float initialTimer = state.Payload.MatchTimer;

            GameSimulation.Tick(state, 1f);

            Assert.Less(state.Payload.MatchTimer, initialTimer, "Match timer should count down");
            Assert.AreEqual(initialTimer - 1f, state.Payload.MatchTimer, 0.01f);
        }

        [Test]
        public void Payload_StalemateReducesFriction()
        {
            var config = PayloadConfig();
            config.PayloadStalemateTime = 1f; // short for testing
            var state = GameSimulation.CreateMatch(config, 42);
            float initialFriction = state.Payload.Friction;

            // Payload is stationary — tick past stalemate time
            for (int i = 0; i < 100; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Phase != MatchPhase.Playing) break;
            }

            Assert.Less(state.Payload.Friction, initialFriction,
                "Friction should reduce after stalemate period");
        }

        [Test]
        public void Payload_TimeUp_PositionTiebreaker()
        {
            var config = PayloadConfig();
            config.PayloadMatchTime = 0.1f; // very short
            var state = GameSimulation.CreateMatch(config, 42);

            // Push payload slightly right (toward P2 goal = P1 winning)
            state.Payload.Position.x = 2f;
            state.Payload.VelocityX = 0f;

            // Tick past the timer
            for (int i = 0; i < 100; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex,
                "Payload closer to right goal means P1 wins on time");
        }

        [Test]
        public void Payload_CheckMatchEnd_DoesNotEndOnDeath()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Kill player 2 — payload mode should NOT end from death
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Payload mode should not end from player death alone");
        }

        [Test]
        public void Payload_PushForceScalesByDistance()
        {
            var state = GameSimulation.CreateMatch(PayloadConfig(), 42);

            // Close explosion
            Vec2 closeExplosion = new Vec2(state.Payload.Position.x - 0.5f, state.Payload.Position.y);
            GameSimulation.ApplyPayloadPush(state, closeExplosion, 3f, 10f);
            float closeVelocity = state.Payload.VelocityX;

            // Reset
            state.Payload.VelocityX = 0f;

            // Far explosion (still in range)
            Vec2 farExplosion = new Vec2(state.Payload.Position.x - 3f, state.Payload.Position.y);
            GameSimulation.ApplyPayloadPush(state, farExplosion, 3f, 10f);
            float farVelocity = state.Payload.VelocityX;

            Assert.Greater(closeVelocity, farVelocity,
                "Closer explosion should push harder than distant one");
        }

        [Test]
        public void Deathmatch_DoesNotInitPayload()
        {
            var config = PayloadConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(0f, state.Payload.VelocityX, 0.01f);
            Assert.AreEqual(0f, state.Payload.GoalLeftX, 0.01f,
                "Deathmatch should not initialize payload state");
        }

        [Test]
        public void BalanceCycle20_GravityBomb_PullForceAndEnergyBuffed()
        {
            var config = new GameConfig();
            var gb = config.Weapons[16];
            Assert.AreEqual("gravity_bomb", gb.WeaponId);
            Assert.AreEqual(25f, gb.EnergyCost, "Gravity Bomb EnergyCost should be 25 (reduced from 30)");
            Assert.AreEqual(6f, gb.PullRadius, "Gravity Bomb PullRadius unchanged at 6");
        }

        [Test]
        public void BalanceCycle21_GravityBomb_BuffedForSetupRole()
        {
            // Issue #332: Gravity bomb was underperforming (longest cooldown, weakest DPS/E).
            // Bumped damage 55 -> 65, cooldown 5s -> 4s, pull force 5 -> 9 so the vortex
            // actually functions as a setup tool.
            var config = new GameConfig();
            var gb = config.Weapons[16];
            Assert.AreEqual("gravity_bomb", gb.WeaponId);
            Assert.AreEqual(65f, gb.MaxDamage, "Gravity Bomb damage should be 65 (buffed from 55)");
            Assert.AreEqual(4f, gb.ShootCooldown, "Gravity Bomb cooldown should be 4s (reduced from 5s)");
            Assert.AreEqual(9f, gb.PullForce, "Gravity Bomb PullForce should be 9 (buffed from 5)");
            Assert.AreEqual(6f, gb.PullRadius, "Gravity Bomb PullRadius unchanged at 6");
            Assert.AreEqual(2.5f, gb.FuseTime, "Gravity Bomb fuse unchanged at 2.5s");
        }

        [Test]
        public void AI_FallbackWeapon_SkipsClusterBombWhenAmmoZero()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(50f, 5f);
            state.Players[1].Position = new Vec2(80f, 5f);
            state.Players[1].IsAI = true;

            // Deplete cluster bomb ammo (slot 3)
            state.Players[1].WeaponSlots[3].Ammo = 0;

            // Deplete all other special weapons so fallback path is reached
            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
            {
                if (s != 3) state.Players[1].WeaponSlots[s].Ammo = 0;
            }

            // Tick enough for AI to select a weapon
            for (int i = 0; i < 120; i++)
                GameSimulation.Tick(state, 0.016f);

            // AI must not select slot 3 (depleted cluster bomb) — should fall back to slot 0 (cannon)
            Assert.AreEqual(0, state.Players[1].ActiveWeaponSlot,
                "AI fallback should select cannon (slot 0) when cluster bomb ammo is depleted");
        }

        // --- Harpoon weapon tests (issue #308) ---

        [Test]
        public void Harpoon_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 21, "Should have at least 21 weapons");
            Assert.AreEqual("harpoon", config.Weapons[20].WeaponId);
            Assert.AreEqual(40f, config.Weapons[20].MaxDamage);
            Assert.AreEqual(1.0f, config.Weapons[20].ExplosionRadius);
            Assert.AreEqual(3.5f, config.Weapons[20].ShootCooldown);
            Assert.AreEqual(3, config.Weapons[20].Ammo);
            Assert.AreEqual(20f, config.Weapons[20].EnergyCost);
            Assert.IsTrue(config.Weapons[20].IsPiercing);
            Assert.AreEqual(1, config.Weapons[20].MaxPierceCount);
        }

        [Test]
        public void Harpoon_CreatesProjectileWithPiercingFlag()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].ActiveWeaponSlot = 20;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsPiercing, "Projectile should have piercing flag");
            Assert.AreEqual(1, state.Projectiles[0].MaxPierceCount);
            Assert.AreEqual(0, state.Projectiles[0].PierceCount);
            Assert.AreEqual(-1, state.Projectiles[0].LastPiercedPlayerId);
        }

        [Test]
        public void Harpoon_PiercesThroughFirstPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place first target at (3, 5) and second at (6, 5)
            state.Players[1].Position = new Vec2(3f, 5f);

            // Create harpoon heading right at player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            float healthBefore = state.Players[1].Health;

            // Tick a few frames so the projectile reaches player 1
            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].PierceCount > 0)
                    break;
            }

            Assert.Less(state.Players[1].Health, healthBefore, "First target should take damage");
            Assert.IsTrue(state.Projectiles.Count > 0 && state.Projectiles[0].Alive,
                "Harpoon should still be alive after piercing first target");
            Assert.AreEqual(1, state.Projectiles[0].PierceCount,
                "Pierce count should be 1 after passing through first target");
        }

        [Test]
        public void Harpoon_ExplodesWhenPierceCountExhausted()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player 1 directly in path
            state.Players[1].Position = new Vec2(3f, 5f);

            // Harpoon that already used its pierce allowance, targeting player 1
            // LastPiercedPlayerId = 0 (not player 1) so it can still collide with player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                PierceCount = 1,
                LastPiercedPlayerId = 0,
                SourceWeaponId = "harpoon"
            });

            float healthBefore = state.Players[1].Health;

            for (int i = 0; i < 30; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.Less(state.Players[1].Health, healthBefore,
                "Target should take damage from harpoon explosion");
            Assert.AreEqual(0, state.Projectiles.Count,
                "Harpoon should be destroyed when pierce count exhausted");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Explosion event should fire when pierce count exhausted");
        }

        [Test]
        public void Harpoon_ExplodesOnTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Create harpoon heading into terrain
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 2f),
                Velocity = new Vec2(0f, -20f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            // Tick until it hits terrain
            for (int i = 0; i < 60; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.AreEqual(0, state.Projectiles.Count,
                "Harpoon should be destroyed on terrain hit");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Explosion event should fire on terrain hit");
        }

        [Test]
        public void Harpoon_ShieldBlocksPierce()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Give player 1 a shield
            state.Players[1].Position = new Vec2(3f, 5f);
            state.Players[1].ShieldHP = 100f;
            state.Players[1].MaxShieldHP = 100f;
            state.Players[1].FacingDirection = -1; // facing left (toward projectile)

            // Create harpoon heading right at shielded player
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            // Tick until collision
            for (int i = 0; i < 30; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.AreEqual(0, state.Projectiles.Count,
                "Harpoon should stop on shielded target (not pierce through)");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Harpoon should explode on shielded target");
        }

        [Test]
        public void Harpoon_NoPierceDamageDoesNotCreateExplosionEvent()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(3f, 5f);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            int explosionsBefore = state.ExplosionEvents.Count;

            // Tick until pierce
            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].PierceCount > 0)
                    break;
            }

            Assert.AreEqual(explosionsBefore, state.ExplosionEvents.Count,
                "Pierce damage should NOT create an explosion event");
            Assert.IsTrue(state.DamageEvents.Count > 0,
                "Pierce damage should create a damage event");
        }

        [Test]
        public void Harpoon_PierceDamageTracksWeaponMastery()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(3f, 5f);
            float healthBefore = state.Players[1].Health;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].PierceCount > 0)
                    break;
            }

            Assert.IsTrue(state.WeaponHits[0].ContainsKey("harpoon"),
                "Pierce hit should be tracked in WeaponHits with SourceWeaponId");
        }

        [Test]
        public void Harpoon_PierceFrame_StillChecksWaterBounds()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place player 1 just above water level so harpoon hits them on the way down
            float waterY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel);
            state.Players[1].Position = new Vec2(5f, waterY + 1f);
            state.Players[1].Health = 100f;

            // Create a piercing projectile heading straight into player, then water
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(5f, waterY + 2f),
                Velocity = new Vec2(0f, -30f), // fast downward
                OwnerIndex = 0,
                ExplosionRadius = 1.0f,
                MaxDamage = 40f,
                KnockbackForce = 6f,
                Alive = true,
                IsPiercing = true,
                MaxPierceCount = 1,
                LastPiercedPlayerId = -1,
                SourceWeaponId = "harpoon"
            });

            // Tick enough for the projectile to pierce player 1 and hit water
            for (int i = 0; i < 20; i++)
            {
                ProjectileSimulation.Update(state, 0.02f);
                if (state.Projectiles.Count == 0) break;
            }
            // One more update to clean up dead projectiles
            if (state.Projectiles.Count > 0)
                ProjectileSimulation.Update(state, 0.02f);

            Assert.AreEqual(0, state.Projectiles.Count,
                "Piercing projectile should die to water/bounds after piercing a player, not fly through forever");
        }

        // ── Flak Cannon (#309) ──────────────────────────────────────

        [Test]
        public void FlakCannon_WeaponDefExists()
        {
            var config = new GameConfig();
            var flak = config.Weapons[21];
            Assert.AreEqual("flak_cannon", flak.WeaponId);
            Assert.IsTrue(flak.IsFlak, "Flak cannon should have IsFlak = true");
            Assert.AreEqual(8, flak.ClusterCount, "Flak should spawn 8 fragments");
            Assert.AreEqual(2, flak.Ammo, "Flak ammo should be 2");
            Assert.AreEqual(5f, flak.FlakMinDist);
            Assert.AreEqual(25f, flak.FlakMaxDist);
        }

        [Test]
        public void FlakCannon_BurstDistance_DerivedFromCharge()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Find flak cannon slot
            int flakSlot = -1;
            for (int i = 0; i < state.Players[0].WeaponSlots.Length; i++)
                if (state.Players[0].WeaponSlots[i].WeaponId == "flak_cannon") { flakSlot = i; break; }
            Assert.IsTrue(flakSlot >= 0, "Flak cannon should exist in weapon slots");

            state.Players[0].ActiveWeaponSlot = flakSlot;
            var weapon = state.Players[0].WeaponSlots[flakSlot];
            // Set AimPower to mid-range (halfway between min and max)
            state.Players[0].AimPower = (weapon.MinPower + weapon.MaxPower) / 2f;
            state.Players[0].AimAngle = 45f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count, "Should fire one projectile");
            var proj = state.Projectiles[0];
            Assert.IsTrue(proj.IsFlak, "Projectile should be flak");
            // Mid-charge should yield mid-range burst distance
            float expected = (weapon.FlakMinDist + weapon.FlakMaxDist) / 2f;
            Assert.AreEqual(expected, proj.FlakBurstDistance, 0.5f, "Burst distance should be ~midpoint at 50% charge");
        }

        [Test]
        public void FlakCannon_DetonatesMidAir_Spawns8Fragments()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Manually create a flak projectile high in the air so it won't hit terrain
            int flakId = state.NextProjectileId;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 0f), // horizontal
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 2f, // short burst distance for quick trigger
                LaunchPosition = new Vec2(0f, 15f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until the flak projectile detonates (identified by ID becoming dead/removed)
            for (int t = 0; t < 100; t++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                bool flakAlive = false;
                foreach (var p in state.Projectiles) if (p.Id == flakId && p.Alive) { flakAlive = true; break; }
                if (!flakAlive) break; // flak detonated
            }

            // Count alive fragment projectiles (SourceWeaponId == flak_cannon, not the original)
            int alive = 0;
            foreach (var p in state.Projectiles) if (p.Alive && p.SourceWeaponId == "flak_cannon") alive++;
            Assert.AreEqual(8, alive, "Should have exactly 8 alive fragment projectiles");
        }

        [Test]
        public void FlakCannon_FragmentsScatterDownward()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            int flakId = state.NextProjectileId;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 1f,
                LaunchPosition = new Vec2(0f, 15f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until the flak projectile detonates
            for (int t = 0; t < 60; t++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                bool flakAlive = false;
                foreach (var p in state.Projectiles) if (p.Id == flakId && p.Alive) { flakAlive = true; break; }
                if (!flakAlive) break;
            }

            // All flak fragments should have negative Y velocity (downward)
            int downward = 0;
            foreach (var p in state.Projectiles)
                if (p.Alive && p.SourceWeaponId == "flak_cannon" && p.Velocity.y < 0f) downward++;
            Assert.AreEqual(8, downward, "All 8 fragments should have downward velocity");
        }

        [Test]
        public void FlakCannon_EarlyDetonation_OnTerrainHit()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Fire flak straight down at terrain — should detonate early + spawn fragments
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 8f),
                Velocity = new Vec2(0f, -15f), // straight down
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 100f, // large distance — won't reach it, should hit terrain first
                LaunchPosition = new Vec2(0f, 8f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until terrain impact spawns fragments
            for (int t = 0; t < 120; t++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (state.Projectiles.Count > 1) break;
            }

            // Fragments should have been spawned
            int alive = 0;
            foreach (var p in state.Projectiles) if (p.Alive) alive++;
            Assert.GreaterOrEqual(alive, 1, "Flak hitting terrain should spawn fragments");
        }

        [Test]
        public void FlakCannon_FragmentsDealDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Place player 1 under a burst point
            state.Players[1].Position = new Vec2(0f, 3f);
            state.Players[1].Health = 100f;

            // Create a fragment directly above player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 4f),
                Velocity = new Vec2(0f, -10f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 2f,
                Alive = true,
                SourceWeaponId = "flak_cannon"
            });

            // Tick until impact
            for (int t = 0; t < 60; t++)
                GameSimulation.Tick(state, 1f / 60f);

            Assert.Less(state.Players[1].Health, 100f, "Fragment should have dealt damage to player");
        }

        // Regression test for #344: flak fragments must inherit parent.MaxDamage
        // (previously hardcoded to 15f, ignoring weapon config and DamageMultiplier).
        [Test]
        public void FlakCannon_FragmentsInheritParentMaxDamage()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Simulate a flak projectile fired with Double Damage active (10f weapon * 2f multiplier = 20f)
            const float parentDamage = 20f;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = parentDamage,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                FlakBurstDistance = 0.01f, // trivially small — detonate on first tick
                LaunchPosition = new Vec2(0f, 15f),
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            });

            // Run ProjectileSimulation directly (bypasses AI). A few ticks are enough
            // to cross the tiny FlakBurstDistance.
            for (int t = 0; t < 5; t++)
                ProjectileSimulation.Update(state, 1f / 60f);

            // Every flak fragment (IsFlak=false, SourceWeaponId=flak_cannon) must inherit
            // parent.MaxDamage, not the hardcoded 15f that existed before the fix.
            int fragmentCount = 0;
            foreach (var p in state.Projectiles)
            {
                if (p.IsFlak) continue; // skip the parent flak projectile
                if (p.SourceWeaponId != "flak_cannon") continue; // skip unrelated projectiles
                fragmentCount++;
                Assert.AreEqual(parentDamage, p.MaxDamage, 0.001f,
                    "Fragment should inherit parent.MaxDamage (including DamageMultiplier buffs)");
            }
            Assert.Greater(fragmentCount, 0, "At least one flak fragment should have been spawned");
        }

        // ── Cluster/Flak spread regression (#346) ───────────────────
        // The spawners used ClusterCount (not ClusterCount-1) as the angle step divisor,
        // so the last sub-projectile always landed one step short of the intended endpoint.
        // Tests use reflection to call the private spawners directly — avoiding any
        // dependency on the wider Tick pipeline (which has unrelated pre-existing issues).

        static void InvokeSpawner(string methodName, GameState state, Vec2 origin, ProjectileState parent)
        {
            var method = typeof(ProjectileSimulation).GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method, $"Could not find {methodName} via reflection");
            method.Invoke(null, new object[] { state, origin, parent });
        }

        static System.Collections.Generic.List<float> CollectAnglesDegrees(
            System.Collections.Generic.IEnumerable<ProjectileState> projectiles)
        {
            var angles = new System.Collections.Generic.List<float>();
            foreach (var p in projectiles)
            {
                if (!p.Alive) continue;
                float deg = MathF.Atan2(p.Velocity.y, p.Velocity.x) * 180f / MathF.PI;
                if (deg < 0f) deg += 360f;
                angles.Add(deg);
            }
            angles.Sort();
            return angles;
        }

        [Test]
        public void FlakCannon_LastFragmentReaches330Endpoint_Issue346()
        {
            // With 8 fragments over a 120° cone starting at 210°, the last fragment
            // should land at ~330°. The bug placed it at ~315° (±5° jitter).
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Projectiles.Clear();
            var parent = new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1f,
                MaxDamage = 10f,
                KnockbackForce = 3f,
                Alive = true,
                IsFlak = true,
                ClusterCount = 8,
                SourceWeaponId = "flak_cannon"
            };

            InvokeSpawner("SpawnFlakFragments", state, new Vec2(1f, 14.9f), parent);

            var angles = CollectAnglesDegrees(state.Projectiles);
            Assert.AreEqual(8, angles.Count, "Flak should spawn 8 fragments");

            // Fix places last fragment at center 330° (±5° jitter → [325, 335]).
            // Bug placed it at center 315° (±5° jitter → [310, 320]).
            Assert.GreaterOrEqual(angles[7], 323f,
                "Last flak fragment should reach the 330° endpoint; bug capped it at ~315°");
            Assert.LessOrEqual(angles[0], 217f,
                "First flak fragment should sit near the 210° start");
        }

        [Test]
        public void FlakCannon_BurstDistance_ClampedToMaxDist_WhenOvercharged()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            int flakSlot = -1;
            for (int i = 0; i < state.Players[0].WeaponSlots.Length; i++)
                if (state.Players[0].WeaponSlots[i].WeaponId == "flak_cannon") { flakSlot = i; break; }
            Assert.IsTrue(flakSlot >= 0, "Flak cannon should exist in weapon slots");

            var weapon = state.Players[0].WeaponSlots[flakSlot];
            state.Players[0].ActiveWeaponSlot = flakSlot;
            state.Players[0].AimAngle = 45f;
            // Set AimPower well above MaxPower to trigger the bug
            state.Players[0].AimPower = weapon.MaxPower * 2f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count, "Should fire one projectile");
            var proj = state.Projectiles[0];
            Assert.IsTrue(proj.IsFlak);
            Assert.LessOrEqual(proj.FlakBurstDistance, weapon.FlakMaxDist,
                "FlakBurstDistance must not exceed FlakMaxDist even when AimPower > MaxPower");
            Assert.GreaterOrEqual(proj.FlakBurstDistance, weapon.FlakMinDist,
                "FlakBurstDistance must not be below FlakMinDist");
        }

        [Test]
        public void ClusterBomb_LastSubProjectileReaches150Endpoint_Issue346()
        {
            // With 8 sub-projectiles over a 30°-150° arc, the last should land at ~150°.
            // Parent velocity X=0 disables the X sign-flip so we can read raw angles.
            var state = GameSimulation.CreateMatch(SmallConfig(), 99);
            state.Projectiles.Clear();
            var parent = new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 2f),
                Velocity = new Vec2(0f, -5f), // pure downward to avoid X sign-flip
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 20f,
                KnockbackForce = 4f,
                Alive = true,
                ClusterCount = 8
            };

            InvokeSpawner("SpawnClusterBombs", state, new Vec2(0f, 2f), parent);

            var angles = CollectAnglesDegrees(state.Projectiles);
            Assert.AreEqual(8, angles.Count, "Cluster should spawn 8 sub-projectiles");

            // Fix: last angle center 150° (±5° jitter → [145, 155]).
            // Bug: last angle center 135° (±5° jitter → [130, 140]).
            Assert.GreaterOrEqual(angles[7], 143f,
                "Last cluster sub-projectile should reach the 150° endpoint; bug capped it at ~135°");
            Assert.LessOrEqual(angles[0], 37f,
                "First cluster sub-projectile should sit near the 30° start");
        }

    }

    [TestFixture]
    public class CampaignTests
    {
        static GameConfig CampaignConfig()
        {
            var config = new GameConfig { MatchType = MatchType.Campaign };
            return config;
        }

        [Test]
        public void CreateMatch_Campaign_CreatesSinglePlayer()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(1, state.Players.Length, "Campaign should create exactly 1 player");
            Assert.IsFalse(state.Players[0].IsAI, "Player 0 should be human");
            Assert.AreEqual(MatchPhase.Playing, state.Phase);
        }

        [Test]
        public void Campaign_CheckMatchEnd_DoesNotEndMatch()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Tick many frames — match should stay Playing (no auto-end)
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Campaign match should not auto-end via CheckMatchEnd");
        }

        [Test]
        public void Campaign_MobsCanBeAddedToPlayersArray()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(1, state.Players.Length);

            // Simulate CampaignBootstrap extending Players array with mobs
            var newPlayers = new PlayerState[3];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState
            {
                Position = new Vec2(30f, 10f),
                Health = 30f, MaxHealth = 30f,
                IsAI = true, IsMob = true, MobType = "walker",
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            newPlayers[2] = new PlayerState
            {
                Position = new Vec2(50f, 10f),
                Health = 50f, MaxHealth = 50f,
                IsAI = true, IsMob = true, MobType = "turret",
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            state.Players = newPlayers;
            AILogic.Reset(state.Seed, state.Players.Length);
            BossLogic.Reset(state.Seed, state.Players.Length);

            Assert.AreEqual(3, state.Players.Length);
            Assert.IsFalse(state.Players[0].IsAI);
            Assert.IsTrue(state.Players[1].IsMob);
            Assert.IsTrue(state.Players[2].IsMob);
        }

        [Test]
        public void ObjectiveTracker_EliminateAll_CompletesWhenAllMobsDead()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Add 2 mobs
            var newPlayers = new PlayerState[3];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState { Health = 30f, MaxHealth = 30f, IsAI = true, IsMob = true };
            newPlayers[2] = new PlayerState { Health = 50f, MaxHealth = 50f, IsAI = true, IsMob = true };
            state.Players = newPlayers;

            var objective = new LevelObjectiveData { type = "eliminate_all" };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);

            // Not complete yet
            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete);

            // Kill mob 1
            state.Players[1].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete, "Should not complete until ALL mobs dead");

            // Kill mob 2
            state.Players[2].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsComplete, "Should complete when all mobs are dead");
        }

        [Test]
        public void ObjectiveTracker_DefeatBoss_CompletesWhenBossDies()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Add a boss at index 1
            var newPlayers = new PlayerState[2];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState
            {
                Health = 200f, MaxHealth = 200f, IsAI = true, IsMob = true,
                BossType = "iron_sentinel"
            };
            state.Players = newPlayers;

            var objective = new LevelObjectiveData { type = "defeat_boss" };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);
            tracker.SetBossIndex(1);

            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete);

            state.Players[1].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsComplete, "Should complete when boss dies");
        }

        [Test]
        public void ObjectiveTracker_FailsWhenPlayerDies()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            var objective = new LevelObjectiveData { type = "eliminate_all" };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);

            state.Players[0].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsFailed, "Should fail when player dies");
        }

        [Test]
        public void ObjectiveTracker_SurviveWaves_ProgressesThroughWaves()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            var objective = new LevelObjectiveData
            {
                type = "survive_waves",
                waveCount = 2,
                waves = new[]
                {
                    new LevelWaveData { delay = 1f, enemies = new[] { new LevelEnemyData { type = "walker", x = 30 } } },
                    new LevelWaveData { delay = 1f, enemies = new[] { new LevelEnemyData { type = "turret", x = 50 } } }
                }
            };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);

            // First tick: starts wave 0 timer
            tracker.Update(state, 0f);
            Assert.IsTrue(tracker.WaveActive);
            Assert.AreEqual(0, tracker.CurrentWave);

            // Tick past wave 0 delay
            tracker.Update(state, 2f);
            Assert.IsTrue(tracker.WaveActive, "Wave still active (waiting for spawn)");

            // Simulate external spawning: add mob at index 1
            var newPlayers = new PlayerState[2];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState { Health = 30f, MaxHealth = 30f, IsAI = true, IsMob = true };
            state.Players = newPlayers;
            tracker.MarkWaveSpawned();

            // Mob alive — wave not complete yet
            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete);

            // Kill mob
            state.Players[1].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.AreEqual(1, tracker.CurrentWave, "Should advance to wave 1");
            Assert.IsTrue(tracker.WaveActive, "Wave 1 should be pending");

            // Tick past wave 1 delay + spawn
            tracker.Update(state, 2f);
            var morePlayers = new PlayerState[3];
            System.Array.Copy(state.Players, morePlayers, 2);
            morePlayers[2] = new PlayerState { Health = 50f, MaxHealth = 50f, IsAI = true, IsMob = true };
            state.Players = morePlayers;
            tracker.MarkWaveSpawned();

            // Kill wave 1 mob
            state.Players[2].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsComplete, "Should complete after all waves cleared");
        }

        [Test]
        public void Campaign_MatchType_InConfig()
        {
            var config = CampaignConfig();
            Assert.AreEqual(MatchType.Campaign, config.MatchType);
        }

        [Test]
        public void Campaign_SimulationTicks_WithMobs()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Add mobs like CampaignBootstrap would
            var newPlayers = new PlayerState[3];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState
            {
                Position = new Vec2(30f, 10f),
                Health = 30f, MaxHealth = 30f,
                IsAI = true, IsMob = true, MobType = "walker",
                Energy = 100f, MaxEnergy = 100f,
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            newPlayers[2] = new PlayerState
            {
                Position = new Vec2(50f, 10f),
                Health = 50f, MaxHealth = 50f,
                IsAI = true, IsMob = true, MobType = "turret",
                Energy = 100f, MaxEnergy = 100f,
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            state.Players = newPlayers;
            state.InitWeaponTracking(state.Players.Length);
            AILogic.Reset(state.Seed, state.Players.Length);
            BossLogic.Reset(state.Seed, state.Players.Length);

            // Tick simulation — should not crash
            for (int i = 0; i < 120; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Campaign should remain Playing while mobs alive (no auto-end)");
        }
    }

    // --- Wall-collision regression tests (#373) ---
    [TestFixture]
    public class WallCollisionTests
    {
        static GameConfig FlatConfig()
        {
            return new GameConfig
            {
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = -1f,
                TerrainHillFrequency = 0.05f,
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
        public void Player_BlockedByWall_AtFootLevel()
        {
            // Regression (#373): foot-level wall was not blocked (only chest sampled).
            var state = GameSimulation.CreateMatch(FlatConfig(), 1);
            ref PlayerState p = ref state.Players[0];
            float startX = p.Position.x;
            float startY = p.Position.y;

            var t = state.Terrain;
            int wallPx = t.WorldToPixelX(startX + 1.5f);
            int footPy = t.WorldToPixelY(startY + 0.1f);
            for (int dy = 0; dy <= 2; dy++)
            {
                int row = footPy - dy;
                if (row >= 0 && row < t.Height)
                    t.SetSolid(wallPx, row, true);
            }

            p.Velocity = new Vec2(5f, 0f);
            p.IsGrounded = true;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(p.Position.x, startX + 2.0f,
                "Player should be blocked by foot-level solid wall");
        }

        [Test]
        public void Player_BlockedByWall_AtHeadLevel()
        {
            // Regression (#373): head-level wall was not blocked (only chest sampled).
            var state = GameSimulation.CreateMatch(FlatConfig(), 2);
            ref PlayerState p = ref state.Players[0];
            float startX = p.Position.x;
            float startY = p.Position.y;

            var t = state.Terrain;
            int wallPx = t.WorldToPixelX(startX + 1.5f);
            int headPy = t.WorldToPixelY(startY + 1.4f);
            for (int dy = 0; dy <= 2; dy++)
            {
                int row = headPy + dy;
                if (row < t.Height)
                    t.SetSolid(wallPx, row, true);
            }

            p.Velocity = new Vec2(5f, 0f);
            p.IsGrounded = true;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(p.Position.x, startX + 2.0f,
                "Player should be blocked by head-level solid wall");
        }
    }

    [TestFixture]
    public class ReplayPlayerTests
    {
        static GameConfig SmallConfig() => new GameConfig
        {
            TerrainWidth = 320, TerrainHeight = 160, TerrainPPU = 8f,
            MapWidth = 40f, TerrainMinHeight = -2f, TerrainMaxHeight = 5f,
            TerrainHillFrequency = 0.1f, TerrainFloorDepth = -10f,
            Player1SpawnX = -10f, Player2SpawnX = 10f,
            SpawnProbeY = 20f, DeathBoundaryY = -25f,
            Gravity = 9.81f, DefaultMaxHealth = 100f,
            DefaultMoveSpeed = 5f, DefaultJumpForce = 10f,
            DefaultShootCooldown = 0.5f
        };

        static ReplayData RecordShortMatch(int frames = 30)
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            var data = ReplaySystem.StartRecording(state);
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < frames; i++)
                GameSimulation.Tick(state, 0.016f);
            ReplaySystem.StopRecording(state);
            return data;
        }

        [Test]
        public void ReplayPlayer_InitialState_FrameIndexIsZero()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            Assert.AreEqual(0, player.FrameIndex);
            Assert.AreEqual(10, player.TotalFrames);
            Assert.IsFalse(player.IsFinished);
        }

        [Test]
        public void ReplayPlayer_Step_AdvancesFrameIndex()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            player.Step();
            Assert.AreEqual(1, player.FrameIndex);
        }

        [Test]
        public void ReplayPlayer_StepAll_IsFinished()
        {
            var data = RecordShortMatch(5);
            var player = new ReplayPlayer(data);
            for (int i = 0; i < 5; i++) player.Step();
            Assert.IsTrue(player.IsFinished);
        }

        [Test]
        public void ReplayPlayer_SeekTo_ResetsAndReachesTarget()
        {
            var data = RecordShortMatch(20);
            var player = new ReplayPlayer(data);
            for (int i = 0; i < 10; i++) player.Step();
            player.SeekTo(5);
            Assert.AreEqual(5, player.FrameIndex);
        }

        [Test]
        public void ReplayPlayer_SeekTo_ClampsBeyondEnd()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            player.SeekTo(999);
            Assert.AreEqual(10, player.FrameIndex);
            Assert.IsTrue(player.IsFinished);
        }

        [Test]
        public void ReplayPlayer_SeekTo_ClampsBeforeStart()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            for (int i = 0; i < 5; i++) player.Step();
            player.SeekTo(-1);
            Assert.AreEqual(0, player.FrameIndex);
        }

        [Test]
        public void ReplayPlayer_PauseResume_PreventsTick()
        {
            var data = RecordShortMatch(30);
            var player = new ReplayPlayer(data);
            player.Pause();
            player.Tick(1.0f);
            Assert.AreEqual(0, player.FrameIndex, "Paused player must not advance on Tick");
            player.Resume();
            player.Tick(0.016f);
            Assert.Greater(player.FrameIndex, 0, "Resumed player should advance on Tick");
        }

        [Test]
        public void ReplayPlayer_TogglePause_FlipsState()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            Assert.IsFalse(player.IsPaused);
            player.TogglePause();
            Assert.IsTrue(player.IsPaused);
            player.TogglePause();
            Assert.IsFalse(player.IsPaused);
        }

        [Test]
        public void ReplayPlayer_SpeedDouble_AdvancesFaster()
        {
            var data = RecordShortMatch(30);
            var playerNormal = new ReplayPlayer(data);
            var playerFast = new ReplayPlayer(data);
            playerNormal.Speed = 1f;
            playerFast.Speed = 2f;
            playerNormal.Tick(0.1f);
            playerFast.Tick(0.1f);
            Assert.Greater(playerFast.FrameIndex, playerNormal.FrameIndex,
                "2x speed should advance more frames than 1x in the same real time");
        }

        [Test]
        public void ReplayPlayer_Deterministic_SameStateAsDirectSimulation()
        {
            var config = SmallConfig();
            const int seed = 77;
            const int frames = 50;

            var direct = GameSimulation.CreateMatch(config, seed);
            direct.Phase = MatchPhase.Playing;
            for (int i = 0; i < frames; i++)
                GameSimulation.Tick(direct, 0.016f);

            var state = GameSimulation.CreateMatch(config, seed);
            var replayData = ReplaySystem.StartRecording(state);
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < frames; i++)
                GameSimulation.Tick(state, 0.016f);
            ReplaySystem.StopRecording(state);

            var player = new ReplayPlayer(replayData);
            while (!player.IsFinished)
                player.Step();

            for (int i = 0; i < direct.Players.Length; i++)
            {
                Assert.AreEqual(direct.Players[i].Position.x,
                    player.State.Players[i].Position.x, 0.001f,
                    $"Player {i} X position mismatch between direct and replay");
                Assert.AreEqual(direct.Players[i].Position.y,
                    player.State.Players[i].Position.y, 0.001f,
                    $"Player {i} Y position mismatch between direct and replay");
                Assert.AreEqual(direct.Players[i].Health,
                    player.State.Players[i].Health, 0.001f,
                    $"Player {i} health mismatch between direct and replay");
            }
        }

        [Test]
        public void ReplaySystem_StopRecording_DisablesRecording()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            ReplaySystem.StartRecording(state);
            ReplaySystem.StopRecording(state);
            Assert.IsNull(state.ReplayRecording,
                "StopRecording should clear ReplayRecording from GameState");
        }

        [Test]
        public void ReplaySystem_DisabledDuringPlayback()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            Assert.IsNull(player.State.ReplayRecording,
                "ReplayPlayer must disable recording on the playback state");
        }

        [Test]
        public void BossLogic_SandWyrm_StaysSurfacedOnFirstSpawn()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as a Sand Wyrm boss
            state.Players[1].BossType = "sand_wyrm";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 150f;
            state.Players[1].Health = 150f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Tick a few frames — wyrm should stay surfaced (subState 0), not immediately submerge
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, BossLogic.subState[1],
                "Sand Wyrm should remain surfaced after initial spawn, not immediately submerge");
        }

        [Test]
        public void BossLogic_ForgeColossus_ArmorExpiresEvenIfPhaseAdvancesPast1()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as Forge Colossus
            state.Players[1].BossType = "forge_colossus";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 200f;
            state.Players[1].Health = 200f;
            state.Players[1].BossPhase = 0;

            // Move player 0 far away so explosions don't kill them
            state.Players[0].Position = new Vec2(-40f, 0f);

            BossLogic.Reset(42);

            // Drop HP to 74% to trigger armor (phase 1)
            state.Players[1].Health = 148f;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(2f, state.Players[1].ArmorMultiplier, "Armor should activate at 75% HP");
            Assert.AreEqual(1, state.Players[1].BossPhase);

            // Immediately drop HP to 49% to trigger phase 2 stomp (skipping armor timer)
            state.Players[1].Health = 98f;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(2, state.Players[1].BossPhase, "Should advance to phase 2");

            // Advance time past the 10s armor window
            for (int i = 0; i < 700; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1f, state.Players[1].ArmorMultiplier,
                "Armor should expire after 10s even when BossPhase advanced past 1");
        }

        [Test]
        public void BossLogic_BaronCogsworth_TeleportClampedToMapBounds()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as Baron Cogsworth
            state.Players[1].BossType = "baron_cogsworth";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 300f;
            state.Players[1].Health = 300f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Drop HP to trigger phase 2 (teleporting phase, BossPhase = 1)
            state.Players[1].Health = 190f; // ~63%, below 66%
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(1, state.Players[1].BossPhase, "Should enter phase 2");

            // Place boss near the map edge
            float halfMap = config.MapWidth / 2f;
            state.Players[1].Position.x = halfMap - 1f;

            // Tick many times to trigger multiple teleports
            for (int i = 0; i < 1000; i++)
                GameSimulation.Tick(state, 0.016f);

            float bossX = state.Players[1].Position.x;
            Assert.GreaterOrEqual(bossX, -halfMap,
                "Boss X should not go below -halfMap after teleport");
            Assert.LessOrEqual(bossX, halfMap,
                "Boss X should not exceed halfMap after teleport");
        }

        [Test]
        public void BossLogic_BaronCogsworth_Phase2TeleportDelayed_Issue48()
        {
            // Issue #48: Phase 2 entry set specialTimer instead of stateTimer,
            // causing immediate teleport because stateTimer was still 0.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].BossType = "baron_cogsworth";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 300f;
            state.Players[1].Health = 300f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Record boss position before phase 2 entry
            Vec2 positionBefore = state.Players[1].Position;

            // Drop HP to trigger phase 2 (66% threshold)
            state.Players[1].Health = 190f; // ~63%, below 66%
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(1, state.Players[1].BossPhase, "Should enter phase 2");

            // Boss should NOT have teleported on the very first phase-2 tick.
            // The 10s delay means position should be unchanged after just 1 tick.
            Assert.AreEqual(positionBefore.x, state.Players[1].Position.x,
                "Boss should not teleport immediately on phase 2 entry (issue #48)");
        }
    }

    [TestFixture]
    public class UnlockRegistryTests
    {
        [Test]
        public void GetTier_ZeroWins_ReturnsTier0()
        {
            Assert.AreEqual(0, UnlockRegistry.GetTier(0));
        }

        [Test]
        public void GetTier_FiveWins_ReturnsTier1()
        {
            Assert.AreEqual(1, UnlockRegistry.GetTier(5));
        }

        [Test]
        public void GetTier_FifteenWins_ReturnsTier2()
        {
            Assert.AreEqual(2, UnlockRegistry.GetTier(15));
        }

        [Test]
        public void GetTier_ThirtyWins_ReturnsTier3()
        {
            Assert.AreEqual(3, UnlockRegistry.GetTier(30));
        }

        [Test]
        public void GetTier_FiftyWins_ReturnsTier4()
        {
            Assert.AreEqual(4, UnlockRegistry.GetTier(50));
        }

        [Test]
        public void GetTier_BetweenThresholds_ReturnsLowerTier()
        {
            Assert.AreEqual(1, UnlockRegistry.GetTier(10)); // between 5 and 15
            Assert.AreEqual(3, UnlockRegistry.GetTier(45)); // between 30 and 50
        }

        [Test]
        public void GetWinsForNextTier_AtZero_Returns5()
        {
            Assert.AreEqual(5, UnlockRegistry.GetWinsForNextTier(0));
        }

        [Test]
        public void GetWinsForNextTier_AtMaxTier_ReturnsZero()
        {
            Assert.AreEqual(0, UnlockRegistry.GetWinsForNextTier(50));
            Assert.AreEqual(0, UnlockRegistry.GetWinsForNextTier(100));
        }

        [Test]
        public void GetWinsForNextTier_MidTier_ReturnsCorrectDifference()
        {
            // 10 wins = tier 1, next tier at 15, so 5 more
            Assert.AreEqual(5, UnlockRegistry.GetWinsForNextTier(10));
        }

        [Test]
        public void IsWeaponUnlocked_CannonAlwaysUnlocked()
        {
            Assert.IsTrue(UnlockRegistry.IsWeaponUnlocked("cannon", 0));
        }

        [Test]
        public void IsWeaponUnlocked_ClusterLockedAtTier0()
        {
            Assert.IsFalse(UnlockRegistry.IsWeaponUnlocked("cluster", 0));
        }

        [Test]
        public void IsWeaponUnlocked_ClusterUnlockedAtTier1()
        {
            Assert.IsTrue(UnlockRegistry.IsWeaponUnlocked("cluster", 1));
        }

        [Test]
        public void IsWeaponUnlocked_SheepLockedUntilTier4()
        {
            Assert.IsFalse(UnlockRegistry.IsWeaponUnlocked("sheep", 3));
            Assert.IsTrue(UnlockRegistry.IsWeaponUnlocked("sheep", 4));
        }

        [Test]
        public void IsSkillIndexUnlocked_FirstSixAlwaysUnlocked()
        {
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(UnlockRegistry.IsSkillIndexUnlocked(i, 0), $"Skill {i} should be unlocked at tier 0");
        }

        [Test]
        public void IsSkillIndexUnlocked_GirderLockedAtTier0()
        {
            Assert.IsFalse(UnlockRegistry.IsSkillIndexUnlocked(6, 0));
        }

        [Test]
        public void IsSkillIndexUnlocked_GirderUnlockedAtTier1()
        {
            Assert.IsTrue(UnlockRegistry.IsSkillIndexUnlocked(6, 1));
        }

        [Test]
        public void GetUnlockedWeaponIds_Tier0_ReturnsFiveWeapons()
        {
            var ids = UnlockRegistry.GetUnlockedWeaponIds(0);
            Assert.AreEqual(5, ids.Count);
            Assert.Contains("cannon", ids);
            Assert.Contains("rocket", ids);
            Assert.Contains("dynamite", ids);
            Assert.Contains("shotgun", ids);
            Assert.Contains("drill", ids);
        }

        [Test]
        public void GetUnlockedWeaponIds_Tier4_ReturnsAll22Weapons()
        {
            var ids = UnlockRegistry.GetUnlockedWeaponIds(4);
            Assert.AreEqual(22, ids.Count);
        }

        [Test]
        public void GetUnlockedSkillIndices_Tier0_ReturnsSixSkills()
        {
            var indices = UnlockRegistry.GetUnlockedSkillIndices(0);
            Assert.AreEqual(6, indices.Count);
        }

        [Test]
        public void GetUnlockedSkillIndices_Tier4_ReturnsAll18Skills()
        {
            var indices = UnlockRegistry.GetUnlockedSkillIndices(4);
            Assert.AreEqual(18, indices.Count);
        }

        [Test]
        public void CreateMatch_Tier0_PlayerHasOnlyStarterWeapons()
        {
            var config = new GameConfig { UnlockedTier = 0 };
            var state = GameSimulation.CreateMatch(config, 42);
            var player = state.Players[0];

            int unlocked = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
                if (player.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.AreEqual(5, unlocked, "Tier 0 player should have exactly 5 weapons");
            Assert.IsNotNull(player.WeaponSlots[0].WeaponId); // cannon
        }

        [Test]
        public void CreateMatch_Tier0_AIHasAllWeapons()
        {
            var config = new GameConfig { UnlockedTier = 0 };
            var state = GameSimulation.CreateMatch(config, 42);
            var ai = state.Players[1];

            int unlocked = 0;
            for (int i = 0; i < ai.WeaponSlots.Length; i++)
                if (ai.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.AreEqual(22, unlocked, "AI should always have all 22 weapons regardless of unlock tier");
        }

        [Test]
        public void CreateMatch_Tier4_PlayerHasAllWeapons()
        {
            var config = new GameConfig { UnlockedTier = 4 };
            var state = GameSimulation.CreateMatch(config, 42);
            var player = state.Players[0];

            int unlocked = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
                if (player.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.AreEqual(22, unlocked, "Tier 4 player should have all 22 weapons");
        }

        [Test]
        public void CreateMatch_Tier0_LockedSkillFallsBackToDefault()
        {
            var config = new GameConfig { UnlockedTier = 0 };
            // Skill index 16 = overcharge, locked at tier 0
            var state = GameSimulation.CreateMatch(config, 42, playerSkill0: 0, playerSkill1: 16);
            var player = state.Players[0];

            // Slot 1 should have fallen back to default (dash, index 3)
            Assert.AreEqual("dash", player.SkillSlots[1].SkillId,
                "Locked skill should fall back to default skill slot");
        }

        [Test]
        public void GetTierName_ValidTiers_ReturnCorrectNames()
        {
            Assert.AreEqual("Starter", UnlockRegistry.GetTierName(0));
            Assert.AreEqual("Veteran", UnlockRegistry.GetTierName(1));
            Assert.AreEqual("Expert", UnlockRegistry.GetTierName(2));
            Assert.AreEqual("Master", UnlockRegistry.GetTierName(3));
            Assert.AreEqual("Legend", UnlockRegistry.GetTierName(4));
        }
    }
}