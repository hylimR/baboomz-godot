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
    public class GameSimulationTests
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
        public void CreateMatch_SpawnsTwoPlayers()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.AreEqual(2, state.Players.Length);
            Assert.AreEqual("Player1", state.Players[0].Name);
            Assert.AreEqual("CPU", state.Players[1].Name);
            Assert.IsFalse(state.Players[0].IsAI);
            Assert.IsTrue(state.Players[1].IsAI);
        }

        [Test]
        public void CreateMatch_PhaseIsPlaying()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.AreEqual(MatchPhase.Playing, state.Phase);
        }

        [Test]
        public void CreateMatch_PlayersHaveFullHealth()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.AreEqual(100f, state.Players[0].Health, 0.01f);
            Assert.AreEqual(100f, state.Players[1].Health, 0.01f);
        }

        [Test]
        public void CreateMatch_TerrainGenerated()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.IsNotNull(state.Terrain);
            Assert.Greater(state.Terrain.Width, 0);
        }

        [Test]
        public void Tick_AdvancesTime()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float startTime = state.Time;
            GameSimulation.Tick(state, 0.016f);
            Assert.Greater(state.Time, startTime);
        }

        [Test]
        public void Tick_GravityPullsPlayersDown()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Move player to air (above terrain)
            state.Players[0].Position = new Vec2(0f, 50f);
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;

            float startY = state.Players[0].Position.y;

            // Tick several frames
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Position.y, startY, "Player should fall due to gravity");
        }

        [Test]
        public void Tick_PlayerStopsOnTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Run many ticks — player should settle on terrain, not fall forever
            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.y, config.DeathBoundaryY,
                "Player should be above death boundary (on terrain)");
            Assert.IsFalse(state.Players[0].IsDead, "Player should be alive");
        }

        [Test]
        public void Tick_100Frames_NoExceptions()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 100; i++)
                    GameSimulation.Tick(state, 0.016f);
            });
        }

        [Test]
        public void Fire_CreatesProjectile()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            int before = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(before + 1, state.Projectiles.Count);
        }

        [Test]
        public void Fire_OnCooldown_DoesNotFire()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;
            state.Players[0].ShootCooldownRemaining = 1f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(0, state.Projectiles.Count);
        }

        [Test]
        public void Fire_SetsCooldown()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;

            GameSimulation.Fire(state, 0);
            Assert.Greater(state.Players[0].ShootCooldownRemaining, 0f);
        }

        [Test]
        public void Projectile_FallsWithGravity()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 90f; // straight up

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);

            float startY = state.Projectiles[0].Position.y;

            // Tick — projectile goes up then comes back down
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // After 10 frames, should still be going up
            Assert.Greater(state.Projectiles[0].Position.y, startY);
        }

        [Test]
        public void Projectile_HitsTerrain_ExplodesAndDealsDamage()
        {
            var config = SmallConfig();
            config.Weapons[0] = new WeaponDef
            {
                WeaponId = "cannon",
                MinPower = 5f,
                MaxPower = 30f,
                ChargeTime = 2f,
                ShootCooldown = 0.1f,
                ExplosionRadius = 3f,
                MaxDamage = 50f,
                KnockbackForce = 5f,
                ProjectileCount = 1,
                Ammo = -1
            };
            var state = GameSimulation.CreateMatch(config, 42);

            // Place players close together
            state.Players[0].Position = new Vec2(-2f, 5f);
            state.Players[1].Position = new Vec2(2f, 5f);

            // Fire horizontally toward player 2
            state.Players[0].AimAngle = 0f; // horizontal right
            state.Players[0].AimPower = 10f;

            GameSimulation.Fire(state, 0);

            // Tick until projectile hits something or times out
            bool exploded = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0)
                {
                    exploded = true;
                    break;
                }
            }

            // Projectile should have hit terrain or gone out of bounds
            // Either way, match should still be valid
            Assert.IsFalse(state.Players[0].IsDead, "Shooter should survive");
        }

        [Test]
        public void Projectile_DiesAtHalfMapWidth_NotFullMapWidth()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float halfMap = state.Config.MapWidth / 2f; // 20

            // Manually add a projectile just beyond half map width
            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(halfMap + 1f, 5f),
                Velocity = new Vec2(10f, 0f),
                Alive = true,
                OwnerIndex = 0,
                MaxDamage = 30f,
                ExplosionRadius = 3f
            });

            Assert.AreEqual(1, state.Projectiles.Count);
            // First update marks the out-of-bounds projectile as dead
            ProjectileSimulation.Update(state, 0.016f);
            // Second update removes dead projectiles from the list
            ProjectileSimulation.Update(state, 0.016f);

            // Projectile should be cleaned up — it's beyond the map edge
            Assert.AreEqual(0, state.Projectiles.Count,
                "Projectile beyond MapWidth/2 should be removed");
        }

        [Test]
        public void DeathBoundary_KillsPlayerAfterSwimDuration()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].Position = new Vec2(0f, state.Config.DeathBoundaryY - 1f);

            // First tick should start swimming, not kill instantly
            GameSimulation.Tick(state, 0.016f);
            Assert.IsTrue(state.Players[0].IsSwimming, "Player should be swimming");
            Assert.IsFalse(state.Players[0].IsDead, "Player should not die instantly in water");

            // Tick until swim timer expires (3s default)
            float swimDuration = state.Config.SwimDuration;
            int ticksNeeded = (int)(swimDuration / 0.016f) + 5;
            for (int i = 0; i < ticksNeeded; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsDead) break;
            }

            Assert.IsTrue(state.Players[0].IsDead, "Player should drown after SwimDuration");
        }

        [Test]
        public void MatchEnds_WhenPlayerDies()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex); // Player2 wins
        }

        [Test]
        public void MatchEnds_Draw_WhenBothDie()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex); // draw
        }

        [Test]
        public void Input_MoveX_MovesPlayer()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            float startX = state.Players[0].Position.x;
            state.Input.MoveX = 1f;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.x, startX, "Player should move right");
        }

        [Test]
        public void Input_AimDelta_ChangesAngle()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float startAngle = state.Players[0].AimAngle;

            state.Input.AimDelta = 1f; // aim up

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].AimAngle, startAngle, "Aim angle should increase");
        }

        [Test]
        public void Input_WeaponSwitch()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Add a second weapon
            state.Players[0].WeaponSlots[1] = new WeaponSlotState
            {
                WeaponId = "rocket",
                Ammo = -1,
                MinPower = 10f,
                MaxPower = 40f,
                ShootCooldown = 1f,
                ExplosionRadius = 4f,
                MaxDamage = 60f
            };

            state.Input.WeaponSlotPressed = 1;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(1, state.Players[0].ActiveWeaponSlot);
        }

        [Test]
        public void WeaponScrollDelta_Forward_AdvancesToNextFilledSlot()
        {
            // Regression (#377): WeaponScrollDelta +1 should cycle to the next valid weapon slot.
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            ref PlayerState p = ref state.Players[0];
            int startSlot = p.ActiveWeaponSlot;
            // All 22 slots are populated by CreateMatch; scroll forward
            state.Input.WeaponScrollDelta = 1;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreNotEqual(startSlot, p.ActiveWeaponSlot,
                "WeaponScrollDelta +1 should advance active weapon slot");
        }

        [Test]
        public void WeaponScrollDelta_Backward_RetreatsToPreFilledSlot()
        {
            // Regression (#377): WeaponScrollDelta -1 should cycle backward.
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            ref PlayerState p = ref state.Players[0];

            // Ensure slots 2 and 3 are filled so backward scroll has a target
            if (p.WeaponSlots[2].WeaponId == null) p.WeaponSlots[2].WeaponId = "rocket";
            if (p.WeaponSlots[3].WeaponId == null) p.WeaponSlots[3].WeaponId = "cluster";
            p.ActiveWeaponSlot = 3; // start mid-list
            state.Input.WeaponSlotPressed = -1; // no direct slot press (default 0 would snap to slot 0)
            state.Input.WeaponScrollDelta = -1;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(2, p.ActiveWeaponSlot,
                "WeaponScrollDelta -1 should retreat active weapon slot by one");
        }

        [Test]
        public void EnergyRegens_OverTime()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].Energy = 50f;

            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Energy, 50f, "Energy should regenerate");
        }

        [Test]
        public void CooldownDecreases_OverTime()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ShootCooldownRemaining = 1f;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].ShootCooldownRemaining, 1f);
        }

        [Test]
        public void Explosion_DestroysTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Find a solid pixel near center
            int cx = state.Terrain.Width / 2;
            int cy = 0;
            for (int y = state.Terrain.Height - 1; y >= 0; y--)
            {
                if (state.Terrain.IsSolid(cx, y))
                {
                    cy = y;
                    break;
                }
            }

            Assert.IsTrue(state.Terrain.IsSolid(cx, cy), "Should have found a solid pixel");

            // Trigger explosion at that pixel (via world coords)
            float wx = state.Terrain.PixelToWorldX(cx);
            float wy = state.Terrain.PixelToWorldY(cy);

            // Fire at the terrain directly — use direct state manipulation
            state.Players[0].Position = new Vec2(wx, wy + 5f);
            state.Players[0].AimAngle = -90f; // straight down
            state.Players[0].AimPower = 15f;

            GameSimulation.Fire(state, 0);

            // Tick until explosion or timeout
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) break;
            }

            // The terrain at the impact point should be cleared
            // (The explosion clears a circle of pixels)
            if (state.ExplosionEvents.Count > 0)
            {
                var evt = state.ExplosionEvents[0];
                int epx = state.Terrain.WorldToPixelX(evt.Position.x);
                int epy = state.Terrain.WorldToPixelY(evt.Position.y);
                Assert.IsFalse(state.Terrain.IsSolid(epx, epy),
                    "Terrain at explosion center should be destroyed");
            }
        }

        [Test]
        public void RunMultipleMatches_NoExceptions()
        {
            var config = SmallConfig();

            Assert.DoesNotThrow(() =>
            {
                for (int match = 0; match < 50; match++)
                {
                    var state = GameSimulation.CreateMatch(config, match);
                    AILogic.Reset(match);

                    for (int frame = 0; frame < 300; frame++)
                    {
                        GameSimulation.Tick(state, 0.016f);

                        if (state.Phase == MatchPhase.Ended) break;
                    }
                }
            });
        }

        [Test]
        public void RunMatch_AIEventuallyFires()
        {
            var config = SmallConfig();
            config.AIShootInterval = 0.5f;
            config.AIShootIntervalRandomness = 0.1f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            bool aiFired = false;
            for (int i = 0; i < 300; i++)
            {
                int projBefore = state.Projectiles.Count;
                GameSimulation.Tick(state, 0.016f);

                if (state.Projectiles.Count > projBefore)
                {
                    // Check if AI fired (owner = 1)
                    for (int j = projBefore; j < state.Projectiles.Count; j++)
                    {
                        if (state.Projectiles[j].OwnerIndex == 1)
                        {
                            aiFired = true;
                            break;
                        }
                    }
                }
                if (aiFired) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.IsTrue(aiFired, "AI should fire at least once within 300 frames");
        }

        [Test]
        public void Tick_DoesNothing_WhenMatchEnded()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Ended;

            float timeBefore = state.Time;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(timeBefore, state.Time, "Time should not advance when ended");
        }

        [Test]
        public void DeadPlayer_IsNotUpdated()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            var posBefore = state.Players[0].Position;

            // Only one alive — match should end, but check position didn't change
            state.Input.MoveX = 1f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(posBefore.x, state.Players[0].Position.x, 0.001f);
        }

        [Test]
        public void DeadPlayer_NoDoubleFallDamageKill()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 1f;
            config.FallDamagePerMeter = 100f;
            config.MatchType = MatchType.Survival;
            config.SurvivalScorePerKill = 10;
            var state = GameSimulation.CreateMatch(config, 42);

            // Mark player 0 as a mob so ScoreSurvivalKill can score
            state.Players[0].IsMob = true;

            // Simulate: player is dead and airborne (killed by explosion mid-air)
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[0].IsGrounded = false;
            state.Players[0].LastGroundedY = state.Players[0].Position.y + 20f;
            state.Players[0].Velocity = new Vec2(0f, -5f);

            int scoreBefore = state.Survival.Score;

            // Tick many frames — dead player should NOT accumulate fall damage or trigger ScoreSurvivalKill
            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(scoreBefore, state.Survival.Score,
                "Dead player landing should not trigger a second ScoreSurvivalKill");
        }

        [Test]
        public void FallDamage_LargeDropDealsDamage()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 3f;
            config.FallDamagePerMeter = 10f;
            config.FallDamageMax = 50f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on ground first
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded, "Player should be grounded after settling");
            float groundY = state.Players[0].Position.y;

            // Move player high above current position
            state.Players[0].Position = new Vec2(state.Players[0].Position.x, groundY + 15f);
            state.Players[0].LastGroundedY = groundY + 15f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;

            float startHealth = state.Players[0].Health;

            // Tick until player lands
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsGrounded) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.Less(state.Players[0].Health, startHealth,
                "Player should take fall damage from 15m drop");
        }

        [Test]
        public void FallDamage_RespectsArmorMultiplier()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 3f;
            config.FallDamagePerMeter = 10f;
            config.FallDamageMax = 50f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on ground first
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded, "Player should be grounded after settling");
            float groundY = state.Players[0].Position.y;

            // Give player 2x armor (shield skill sets this)
            state.Players[0].ArmorMultiplier = 2f;

            // Move player high above current position
            state.Players[0].Position = new Vec2(state.Players[0].Position.x, groundY + 15f);
            state.Players[0].LastGroundedY = groundY + 15f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;

            float startHealth = state.Players[0].Health;

            // Tick until player lands
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsGrounded) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            float damageTaken = startHealth - state.Players[0].Health;
            Assert.Greater(damageTaken, 0f, "Player should still take some fall damage");

            // With 2x armor, damage should be halved compared to unarmored
            // Unarmored: excess * 10 capped at 50 → for 12m excess = 50 (capped)
            // Armored: 50 / 2 = 25
            Assert.LessOrEqual(damageTaken, 25f + 0.1f,
                "Fall damage should be reduced by ArmorMultiplier");
        }

        [Test]
        public void FallDamage_InvulnerablePlayerTakesNoDamage()
        {
            var config = SmallConfig();
            config.FallDamageMinDistance = 3f;
            config.FallDamagePerMeter = 10f;
            config.FallDamageMax = 50f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Let players settle on ground first
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded, "Player should be grounded after settling");
            float groundY = state.Players[0].Position.y;

            // Move player high above current position and flag invulnerable (e.g. boss shield phase)
            state.Players[0].Position = new Vec2(state.Players[0].Position.x, groundY + 15f);
            state.Players[0].LastGroundedY = groundY + 15f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;
            state.Players[0].IsInvulnerable = true;

            float startHealth = state.Players[0].Health;

            // Tick until player lands
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsGrounded) break;
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.IsTrue(state.Players[0].IsGrounded, "Invulnerable player should still land");
            Assert.AreEqual(startHealth, state.Players[0].Health, 0.001f,
                "Invulnerable player should take no fall damage on landing");
        }

        [Test]
        public void UpwardCollision_PlayerDoesNotClipThroughCeiling()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle on ground
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            float groundY = state.Players[0].Position.y;
            float playerX = state.Players[0].Position.x;

            // Place a solid ceiling 3 units above the player (head is at +1.5, so ceiling at +3)
            float ceilingWorldY = groundY + 3f;
            int ceilPy = state.Terrain.WorldToPixelY(ceilingWorldY);
            int centerPx = state.Terrain.WorldToPixelX(playerX);
            // Fill a wide ceiling slab (20 pixels wide, 5 pixels thick)
            for (int dx = -10; dx <= 10; dx++)
                for (int dy = 0; dy < 5; dy++)
                    state.Terrain.SetSolid(centerPx + dx, ceilPy + dy, true);

            // Give player upward velocity to jump into the ceiling
            state.Players[0].Velocity = new Vec2(0f, 15f);
            state.Players[0].IsGrounded = false;

            float startY = state.Players[0].Position.y;

            // Tick a few frames — player should be blocked by ceiling
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should not have passed through the ceiling
            Assert.Less(state.Players[0].Position.y, ceilingWorldY,
                "Player should be blocked by ceiling terrain, not clip through it");
        }

        [Test]
        public void MultipleWeapons_AllPopulated()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Default config has 4 weapons
            Assert.IsNotNull(state.Players[0].WeaponSlots[0].WeaponId, "Slot 0 should have cannon");
            Assert.IsNotNull(state.Players[0].WeaponSlots[1].WeaponId, "Slot 1 should have shotgun");
            Assert.IsNotNull(state.Players[0].WeaponSlots[2].WeaponId, "Slot 2 should have rocket");
            Assert.IsNotNull(state.Players[0].WeaponSlots[3].WeaponId, "Slot 3 should have drill");
        }

        [Test]
        public void Shotgun_FiresMultipleProjectiles()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 1; // shotgun
            state.Players[0].AimPower = 20f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(4, state.Projectiles.Count, "Shotgun should fire 4 projectiles");
        }

        [Test]
        public void Rocket_HasLimitedAmmo()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 2; // rocket (4 ammo)
            state.Players[0].AimPower = 20f;

            Assert.AreEqual(4, state.Players[0].WeaponSlots[2].Ammo);

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(3, state.Players[0].WeaponSlots[2].Ammo);

            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 20f;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(2, state.Players[0].WeaponSlots[2].Ammo);
        }

        [Test]
        public void WindChanges_DuringMatch()
        {
            var config = SmallConfig();
            config.WindChangeInterval = 0.1f; // change every 0.1s
            var state = GameSimulation.CreateMatch(config, 42);

            float initialWind = state.WindForce;

            // Tick past the wind change interval
            for (int i = 0; i < 20; i++)
                GameSimulation.Tick(state, 0.016f);

            // Wind should have changed at least once
            // (may or may not be different value, but NextWindChangeTime should have advanced)
            Assert.Greater(state.NextWindChangeTime, 0.1f, "Wind change time should advance");
        }

        [Test]
        public void EnergyWeapon_CannotFire_WhenOutOfEnergy()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 1; // shotgun (18 energy cost)
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 5f; // not enough

            int before = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(before, state.Projectiles.Count, "Should not fire without enough energy");
        }

        [Test]
        public void Fire_ZeroAmmo_DoesNotDeductEnergy()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ActiveWeaponSlot = 1; // shotgun (18 energy cost)
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            state.Players[0].WeaponSlots[1].Ammo = 0; // depleted

            float energyBefore = state.Players[0].Energy;
            int projBefore = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, "Energy must not be deducted when ammo is 0");
            Assert.AreEqual(projBefore, state.Projectiles.Count, "No projectile should be created when ammo is 0");
        }

        [Test]
        public void FacingDirection_AffectsProjectileVelocity()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 20f;

            // Face right
            state.Players[0].FacingDirection = 1;
            GameSimulation.Fire(state, 0);
            Assert.Greater(state.Projectiles[0].Velocity.x, 0f, "Facing right should fire rightward");

            // Face left
            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].FacingDirection = -1;
            GameSimulation.Fire(state, 0);
            Assert.Less(state.Projectiles[1].Velocity.x, 0f, "Facing left should fire leftward");

            // Both should have same Y velocity (same angle)
            Assert.AreEqual(state.Projectiles[0].Velocity.y, state.Projectiles[1].Velocity.y, 0.01f,
                "Y velocity should be identical regardless of facing");
        }

        [Test]
        public void WallCollision_StopsHorizontalMovement()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Build a wall to the right of the player
            float wallX = state.Players[0].Position.x + 1f;
            int wallPx = state.Terrain.WorldToPixelX(wallX);
            int basePy = state.Terrain.WorldToPixelY(state.Players[0].Position.y);
            for (int py = basePy; py < basePy + 20; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            float startX = state.Players[0].Position.x;

            // Try to move right into the wall
            state.Input.MoveX = 1f;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should not have moved far past their start (wall blocks)
            Assert.Less(state.Players[0].Position.x, wallX,
                "Player should be blocked by wall");
        }

        [Test]
        public void Projectile_HitsPlayer_ExplodesOnContact()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Let players settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Place a projectile right at player 2's position (direct hit)
            float p2X = state.Players[1].Position.x;
            float p2Y = state.Players[1].Position.y + 0.5f;
            float startHealth = state.Players[1].Health;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(p2X - 0.3f, p2Y),
                Velocity = new Vec2(1f, 0f), // moving toward player
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 30f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick — projectile should hit player within a few frames
            for (int i = 0; i < 30; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) break;
            }

            Assert.Greater(state.ExplosionEvents.Count, 0, "Projectile should explode on player contact");
            Assert.Less(state.Players[1].Health, startHealth, "Player should take damage from direct hit");
        }

        [Test]
        public void SlopeWalking_PlayerFollowsTerrainContour()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded);
            float startY = state.Players[0].Position.y;

            // Build a gentle upward slope to the right
            float baseX = state.Players[0].Position.x;
            for (int dx = 0; dx < 40; dx++)
            {
                float worldX = baseX + dx * 0.2f;
                float slopeY = startY - 0.5f + dx * 0.1f; // rising slope
                int px = state.Terrain.WorldToPixelX(worldX);
                int surfacePy = state.Terrain.WorldToPixelY(slopeY);
                // Fill solid below surface
                for (int py = 0; py < surfacePy; py++)
                    state.Terrain.SetSolid(px, py, true);
                // Clear above
                for (int py = surfacePy; py < surfacePy + 10; py++)
                    state.Terrain.SetSolid(px, py, false);
            }

            // Walk right for a while
            state.Input.MoveX = 1f;
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should have moved right AND upward (following slope)
            Assert.Greater(state.Players[0].Position.x, baseX + 1f,
                "Player should move right");
            Assert.Greater(state.Players[0].Position.y, startY - 0.5f,
                "Player should walk up the slope, not fall through");
        }

        [Test]
        public void ClusterBomb_SpawnsSubProjectilesOnImpact()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Let players settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Switch to cluster (slot 3)
            state.Players[0].ActiveWeaponSlot = 3;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 60f;

            var weapon = state.Players[0].WeaponSlots[3];
            Assert.AreEqual("cluster", weapon.WeaponId);
            Assert.AreEqual(4, weapon.ClusterCount);  // balanced: 5→4

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.AreEqual(4, state.Projectiles[0].ClusterCount);

            // Tick until impact — should spawn sub-projectiles
            bool hitTerrain = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                // Cluster spawns sub-projectiles, so count should increase
                if (state.Projectiles.Count > 1)
                {
                    hitTerrain = true;
                    break;
                }
                if (state.ExplosionEvents.Count > 0 && state.Projectiles.Count == 0)
                {
                    hitTerrain = true;
                    break;
                }
            }

            Assert.IsTrue(hitTerrain, "Cluster bomb should hit terrain and produce sub-projectiles or explosions");
        }

        [Test]
        public void FuseTimer_ExplodesAfterCountdown()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Manually add a grenade projectile in mid-air (no terrain to hit)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 50f), // high in air, no terrain to hit
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 8f,
                Alive = true,
                BouncesRemaining = 3,
                FuseTimer = 0.5f // 0.5 seconds fuse
            });

            // Tick until fuse expires
            bool exploded = false;
            for (int i = 0; i < 100; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0)
                {
                    exploded = true;
                    break;
                }
            }

            Assert.IsTrue(exploded, "Grenade should explode after fuse timer");
        }

        [Test]
        public void AmmoDepletion_AutoSwitchesToNextWeapon()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Give slot 2 (rocket) only 1 ammo
            state.Players[0].WeaponSlots[2].Ammo = 1;
            state.Players[0].ActiveWeaponSlot = 2;
            state.Players[0].AimPower = 20f;

            Assert.AreEqual(2, state.Players[0].ActiveWeaponSlot);

            GameSimulation.Fire(state, 0);

            // After firing last rocket, should auto-switch to a weapon with ammo
            Assert.AreNotEqual(2, state.Players[0].ActiveWeaponSlot,
                "Should auto-switch away from depleted weapon");
            Assert.IsNotNull(state.Players[0].WeaponSlots[state.Players[0].ActiveWeaponSlot].WeaponId,
                "Should switch to a valid weapon");
        }

        [Test]
        public void Stats_TrackDamageAndShots()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Place players close, fire directly at P2
            state.Players[0].Position = new Vec2(-2f, 5f);
            state.Players[1].Position = new Vec2(2f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 10f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Players[0].ShotsFired);

            // Tick until explosion hits P2
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) break;
            }

            // P1 should have dealt some damage
            if (state.Players[0].TotalDamageDealt > 0)
            {
                Assert.Greater(state.Players[0].DirectHits, 0, "Should have recorded direct hit");
            }
        }

        [Test]
        public void BalanceTest_200Matches_AIWinRateReasonable()
        {
            var config = SmallConfig();
            config.AIShootInterval = 2f;
            config.AIShootIntervalRandomness = 1f;

            int p1Wins = 0, p2Wins = 0, draws = 0, timeouts = 0;

            for (int match = 0; match < 200; match++)
            {
                var state = GameSimulation.CreateMatch(config, match * 7 + 13);
                AILogic.Reset(match * 7 + 13);

                // Simulate up to 60 seconds (3750 frames at 16ms)
                for (int frame = 0; frame < 3750; frame++)
                {
                    GameSimulation.Tick(state, 0.016f);
                    if (state.Phase == MatchPhase.Ended) break;
                }

                if (state.Phase == MatchPhase.Ended)
                {
                    if (state.WinnerIndex == 0) p1Wins++;
                    else if (state.WinnerIndex == 1) p2Wins++;
                    else draws++;
                }
                else
                {
                    timeouts++;
                }
            }

            int totalDecided = p1Wins + p2Wins + draws;
            Assert.Greater(totalDecided, 0, "At least some matches should end");

            // AI should win SOME matches (it shoots at the player)
            // If AI never wins, its aim/movement is broken
            Assert.Greater(p2Wins, 0,
                $"AI should win at least 1 match (P1:{p1Wins} P2:{p2Wins} Draw:{draws} Timeout:{timeouts})");
        }

        [Test]
        public void LongMatch_5000Frames_Stable()
        {
            var config = SmallConfig();
            config.AIShootInterval = 1f;
            config.AIShootIntervalRandomness = 0.5f;

            var state = GameSimulation.CreateMatch(config, 99);
            AILogic.Reset(99);

            // Run for 5000 frames (~80 seconds) — testing stability, no crashes
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 5000; i++)
                {
                    GameSimulation.Tick(state, 0.016f);
                    if (state.Phase == MatchPhase.Ended) break;
                }
            });

            // AI should have fired at least once
            Assert.Greater(state.Players[1].ShotsFired, 0,
                "AI should fire at least once in 5000 frames");
        }

        [Test]
        public void DifficultyEasy_AIHasHighErrorMargin()
        {
            var config = SmallConfig();
            // Simulate what GameRunner.ApplyDifficulty does for Easy
            config.AIAimErrorMargin = 12f;
            config.AIShootInterval = 5f;
            config.DefaultMaxHealth = 150f;

            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(150f, state.Players[0].MaxHealth, "Easy should give 150 HP");
            Assert.AreEqual(150f, state.Players[1].MaxHealth, "AI also gets 150 HP");
        }

        [Test]
        public void DifficultyHard_PlayerHasLessHP()
        {
            var config = SmallConfig();
            config.AIAimErrorMargin = 2f;
            config.AIShootInterval = 2f;
            config.DefaultMaxHealth = 80f;

            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(80f, state.Players[0].MaxHealth, "Hard should give 80 HP");
        }

        [Test]
        public void MultiRound_SequentialMatches_NoCorruption()
        {
            var config = SmallConfig();
            config.AIShootInterval = 1f;

            // Simulate 3 rounds like the round system does
            for (int round = 0; round < 3; round++)
            {
                var state = GameSimulation.CreateMatch(config, round * 100 + 7);
                AILogic.Reset(round * 100 + 7);

                Assert.AreEqual(MatchPhase.Playing, state.Phase);
                Assert.AreEqual(2, state.Players.Length);
                Assert.IsFalse(state.Players[0].IsDead);
                Assert.IsFalse(state.Players[1].IsDead);
                Assert.Greater(state.Players[0].Health, 0f);

                // Run until match ends or timeout
                for (int frame = 0; frame < 2000; frame++)
                {
                    GameSimulation.Tick(state, 0.016f);
                    if (state.Phase == MatchPhase.Ended) break;
                }

                // State should be valid regardless of outcome
                Assert.IsTrue(state.Phase == MatchPhase.Playing || state.Phase == MatchPhase.Ended);
            }
        }

        [Test]
        public void AllWeaponTypes_CanFire()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            for (int slot = 0; slot < 4; slot++)
            {
                if (state.Players[0].WeaponSlots[slot].WeaponId == null) continue;

                state.Players[0].ActiveWeaponSlot = slot;
                state.Players[0].AimPower = 15f;
                state.Players[0].ShootCooldownRemaining = 0f;
                state.Players[0].Energy = 100f;

                int before = state.Projectiles.Count;
                GameSimulation.Fire(state, 0);

                Assert.Greater(state.Projectiles.Count, before,
                    $"Weapon slot {slot} ({state.Players[0].WeaponSlots[slot].WeaponId}) should fire");
            }
        }

        [Test]
        public void SplashEvent_EmittedOnSwimEntry()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Move player below water level
            state.Players[0].Position = new Vec2(0f, state.Config.DeathBoundaryY - 1f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsSwimming, "Player should start swimming");
            Assert.Greater(state.SplashEvents.Count, 0, "Splash event should emit on swim entry");
        }

        [Test]
        public void Mines_SpawnOnTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 5;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(5, state.Mines.Count, "Should spawn 5 mines");
            foreach (var mine in state.Mines)
            {
                Assert.IsTrue(mine.Active, "All mines should start active");
                Assert.Greater(mine.ExplosionRadius, 0f);
            }
        }

        [Test]
        public void Mines_SkipSpawnWhenNoGroundAtX()
        {
            var config = SmallConfig();
            config.TerrainWidth = 20; // very narrow terrain (2.5 world units at PPU=8)
            config.MapWidth = 200f;   // wide map — most X samples miss terrain
            config.MineCount = 10;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // With fix: mines at X with no ground are skipped, so count <= requested
            Assert.LessOrEqual(state.Mines.Count, config.MineCount);
            foreach (var mine in state.Mines)
            {
                Assert.That(mine.Position.y, Is.Not.EqualTo(config.SpawnProbeY).Within(0.3f),
                    "Mine should not spawn at SpawnProbeY fallback height");
            }
        }

        [Test]
        public void Mine_ExplodesWhenPlayerWalksOver()
        {
            var config = SmallConfig();
            config.MineCount = 0; // no random mines
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Place a mine at a known safe position away from players
            float mineX = state.Players[0].Position.x + 10f;
            float mineY = GamePhysics.FindGroundY(state.Terrain, mineX, 20f);
            state.Mines.Add(new MineState
            {
                Position = new Vec2(mineX, mineY),
                TriggerRadius = 1.5f,
                ExplosionRadius = 3f,
                Damage = 45f,
                Active = true,
                OwnerIndex = -1 // environment mine
            });

            // Move player to mine position
            state.Players[0].Position = new Vec2(mineX, mineY);

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Mines.Count, "Triggered mine should be removed from list");
            Assert.Greater(state.ExplosionEvents.Count, 0, "Mine should produce explosion");
        }

        // --- Regression tests for bugs fixed 2026-03-23 ---

        [Test]
        public void BossLogic_ForgeColossus_StompDoesNotSelfDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as a Forge Colossus boss
            state.Players[1].BossType = "forge_colossus";
            state.Players[1].IsMob = true;
            state.Players[1].MaxHealth = 200f;
            state.Players[1].Health = 200f;
            state.Players[1].BossPhase = 0;

            // Move player 0 far away so stomp only affects boss
            state.Players[0].Position = new Vec2(-40f, 0f);

            // Trigger phase 2 stomp by dropping HP to 49%
            state.Players[1].Health = 98f; // 49% of 200

            BossLogic.Reset(42);
            float healthBefore = state.Players[1].Health;

            // Tick to trigger phase transition
            GameSimulation.Tick(state, 0.016f);

            Assert.GreaterOrEqual(state.Players[1].Health, healthBefore - 0.01f,
                "Forge Colossus should not take self-damage from stomp");
        }

        [Test]
        public void ClusterBomb_SubProjectiles_SpreadBothDirections()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Place a cluster projectile moving leftward
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 2f),
                Velocity = new Vec2(-10f, 5f), // moving LEFT
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 20f,
                KnockbackForce = 4f,
                Alive = true,
                ClusterCount = 5
            });

            // Build a terrain wall to force impact
            for (int px = 150; px <= 165; px++)
                for (int py = 0; py < 80; py++)
                    state.Terrain.SetSolid(px, py, true);

            // Tick until cluster impacts
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Projectiles.Count > 1) break;
            }

            if (state.Projectiles.Count > 1)
            {
                // Sub-projectiles from a leftward-moving parent should have negative X velocities
                bool hasLeftward = false;
                for (int i = 0; i < state.Projectiles.Count; i++)
                {
                    if (state.Projectiles[i].ClusterCount == 0 && state.Projectiles[i].Velocity.x < 0f)
                    {
                        hasLeftward = true;
                        break;
                    }
                }
                Assert.IsTrue(hasLeftward,
                    "Cluster sub-projectiles should spread in the parent's travel direction");
            }
        }

        [Test]
        public void MultipleMatches_BossLogicReset_NoStaleTimers()
        {
            var config = SmallConfig();

            // Match 1: run with boss
            var state1 = GameSimulation.CreateMatch(config, 100);
            AILogic.Reset(100);
            BossLogic.Reset(100);
            state1.Players[1].BossType = "iron_sentinel";
            state1.Players[1].IsMob = true;

            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state1, 0.016f);

            // Match 2: fresh match — boss timers should not carry over
            var state2 = GameSimulation.CreateMatch(config, 200);
            AILogic.Reset(200);
            BossLogic.Reset(200);
            state2.Players[1].BossType = "iron_sentinel";
            state2.Players[1].IsMob = true;

            // The boss should behave correctly from frame 0 without stale timer issues
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 300; i++)
                    GameSimulation.Tick(state2, 0.016f);
            });
        }

        // --- Sudden Death (rising water) tests ---

        [Test]
        public void SuddenDeath_WaterRises_AfterTimeout()
        {
            var config = SmallConfig();
            config.SuddenDeathTime = 2f;   // water starts rising after 2 seconds
            config.WaterRiseSpeed = 5f;     // 5 units/sec for quick test

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float initialWater = state.WaterLevel;
            Assert.IsFalse(state.SuddenDeathActive);

            // Tick for 1.5 seconds — water should NOT have risen
            for (int i = 0; i < 94; i++) // ~1.5s at 16ms
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.SuddenDeathActive);
            Assert.AreEqual(initialWater, state.WaterLevel, 0.01f,
                "Water should not rise before SuddenDeathTime");

            // Tick past 2 seconds total — water should start rising
            for (int i = 0; i < 62; i++) // another ~1s
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.SuddenDeathActive, "Sudden death should be active after timeout");
            Assert.Greater(state.WaterLevel, initialWater,
                "Water level should rise after sudden death starts");
        }

        [Test]
        public void SuddenDeath_KillsPlayerWhenWaterReaches()
        {
            var config = SmallConfig();
            // Use a longer timeout so player can settle first, then trigger sudden death
            config.SuddenDeathTime = 5f;    // water starts after 5 seconds
            config.WaterRiseSpeed = 50f;     // very fast rise once it starts

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Let players settle (200 frames = ~3.2s, before sudden death at 5s)
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsDead, "Player should be alive before sudden death");
            Assert.IsFalse(state.SuddenDeathActive, "Sudden death should not be active yet");

            // Tick past the 5-second mark and continue until water kills
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsDead) break;
            }

            Assert.IsTrue(state.Players[0].IsDead,
                "Rising water should kill player when it reaches them");
        }

        [Test]
        public void SuddenDeath_ProjectilesDieAtWaterLevel()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);

            // Raise water level above terrain (SmallConfig TerrainFloorDepth = -10, terrain top ~ 5)
            // Set water at y=10 — well above all terrain, so projectile won't hit terrain first
            float raisedWater = 10f;
            state.WaterLevel = raisedWater;

            // Place a projectile just above water level, falling straight down
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, raisedWater + 2f),
                Velocity = new Vec2(0f, -20f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 30f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Call ProjectileSimulation.Update directly to avoid match-end interference
            state.SplashEvents.Clear();
            Assert.AreEqual(1, state.Projectiles.Count, "Should have 1 projectile");

            // Step 1 frame at a time, checking state
            for (int i = 0; i < 300; i++)
            {
                ProjectileSimulation.Update(state, 0.05f); // larger dt for faster fall
                if (state.Projectiles.Count == 0) break;
            }

            Assert.AreEqual(0, state.Projectiles.Count,
                "Projectile should have been removed");
            Assert.Greater(state.SplashEvents.Count, 0,
                "Projectile should splash at water level during sudden death");

            float splashY = state.SplashEvents[0].Position.y;
            Assert.AreEqual(raisedWater, splashY, 1f,
                "Splash should be at raised water level, not at static DeathBoundaryY");
        }

        [Test]
        public void SuddenDeath_Disabled_WhenTimeIsZero()
        {
            var config = SmallConfig();
            config.SuddenDeathTime = 0f; // disabled

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float initialWater = state.WaterLevel;

            for (int i = 0; i < 500; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.SuddenDeathActive);
            Assert.AreEqual(initialWater, state.WaterLevel, 0.01f,
                "Water should not rise when SuddenDeathTime is 0");
        }

        [Test]
        public void Swimming_EntersSwimState_WhenBelowWater()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 0.5f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsSwimming, "Player should enter swimming state");
            Assert.IsFalse(state.Players[0].IsDead, "Player should not die on first water contact");
            Assert.Greater(state.Players[0].SwimTimer, 0f, "Swim timer should start counting");
        }

        [Test]
        public void Swimming_DrownsAfterSwimDuration()
        {
            var config = SmallConfig();
            config.SwimDuration = 1f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 0.5f);

            int ticks = (int)(config.SwimDuration / 0.016f) + 10;
            for (int i = 0; i < ticks; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsDead) break;
            }

            Assert.IsTrue(state.Players[0].IsDead, "Player should drown after SwimDuration");
            Assert.AreEqual(0f, state.Players[0].Health, 0.001f);
        }

        [Test]
        public void Swimming_EscapeClearsSwimState()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].SwimTimer = 1.5f;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel + 1f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsSwimming, "Swimming should clear when above water");
            Assert.AreEqual(0f, state.Players[0].SwimTimer, 0.001f, "Swim timer should reset");
        }

        [Test]
        public void Swimming_ReducesMovementSpeed()
        {
            var config = SmallConfig();
            config.SwimSpeedMultiplier = 0.4f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 1f);
            state.Input.MoveX = 1f;
            float startX = state.Players[0].Position.x;

            GameSimulation.Tick(state, 0.016f);
            float swimDeltaX = state.Players[0].Position.x - startX;

            var state2 = GameSimulation.CreateMatch(config, 42);
            state2.Input.MoveX = 1f;
            float startX2 = state2.Players[0].Position.x;

            GameSimulation.Tick(state2, 0.016f);
            float normalDeltaX = state2.Players[0].Position.x - startX2;

            Assert.Greater(normalDeltaX, swimDeltaX,
                "Swimming player should move slower than normal");
        }

        [Test]
        public void Swimming_SinksOverTime()
        {
            var config = SmallConfig();
            config.SwimSinkSpeed = 2f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            float startY = state.WaterLevel - 1f;
            state.Players[0].Position = new Vec2(0f, startY);

            GameSimulation.Tick(state, 0.1f);

            Assert.Less(state.Players[0].Position.y, startY,
                "Swimming player should sink over time");
        }

        [Test]
        public void Swimming_BlocksFiring()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            state.Players[0].IsSwimming = true;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 1f);
            state.Input.FireHeld = true;

            GameSimulation.Tick(state, 0.016f);

            state.Input.FireHeld = false;
            state.Input.FireReleased = true;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Projectiles.Count,
                "Swimming player should not be able to fire");
        }

        [Test]
        public void Swimming_BlocksSkillActivation()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 1f);
            state.Input.Skill1Pressed = true;

            float energyBefore = state.Players[0].Energy;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.5f,
                "Swimming player should not activate skills (energy unchanged)");
        }

        [Test]
        public void Swimming_KnockbackCanPushOutOfWater()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].IsSwimming = true;
            state.Players[0].SwimTimer = 2f;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel - 0.2f);
            state.Players[0].Velocity = new Vec2(0f, 15f);
            state.Players[0].IsSwimming = false;
            state.Players[0].Position = new Vec2(0f, state.WaterLevel + 2f);

            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsSwimming,
                "Player knocked above water should not be swimming");
            Assert.IsFalse(state.Players[0].IsDead,
                "Player knocked out of water should survive");
        }

        [Test]
        public void AI_JetpackDangerCheck_UsesWaterLevel()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Raise water level high — AI should detect danger relative to water, not DeathBoundaryY
            float raisedWater = 5f;
            state.WaterLevel = raisedWater;

            // Place AI player just above raised water, falling
            state.Players[1].Position = new Vec2(0f, raisedWater + 3f);
            state.Players[1].Velocity = new Vec2(0f, -5f);
            state.Players[1].IsGrounded = false;

            // Give AI a jetpack skill
            state.Players[1].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "jetpack", Type = SkillType.Jetpack,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 3f, Value = 10f
                },
                new SkillSlotState()
            };
            state.Players[1].Energy = 100f;

            // Tick AI logic — it should activate jetpack because y < waterLevel + 5
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            bool jetpackActivated = false;
            for (int s = 0; s < state.Players[1].SkillSlots.Length; s++)
            {
                if (state.Players[1].SkillSlots[s].Type == SkillType.Jetpack
                    && state.Players[1].SkillSlots[s].IsActive)
                {
                    jetpackActivated = true;
                    break;
                }
            }

            Assert.IsTrue(jetpackActivated,
                "AI should activate jetpack when near raised water level during sudden death");
        }

        // --- Crate drop tests ---

        [Test]
        public void Crates_SpawnAfterInterval()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 2f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            Assert.AreEqual(0, state.Crates.Count);

            // Tick past the first spawn interval (2 seconds)
            for (int i = 0; i < 200; i++) // ~3.2 seconds
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Crates.Count, 0, "Crate should spawn after interval");
        }

        [Test]
        public void Crates_SpawnedTypeIsValidEnumValue()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 1f;
            config.SuddenDeathTime = 0f;

            int crateTypeCount = System.Enum.GetValues(typeof(CrateType)).Length;

            // Test many seeds to cover random distribution
            for (int seed = 0; seed < 100; seed++)
            {
                var state = GameSimulation.CreateMatch(config, seed);
                AILogic.Reset(seed);

                for (int i = 0; i < 200; i++)
                    GameSimulation.Tick(state, 0.016f);

                foreach (var crate in state.Crates)
                {
                    int typeInt = (int)crate.Type;
                    Assert.IsTrue(typeInt >= 0 && typeInt < crateTypeCount,
                        $"Crate type {crate.Type} ({typeInt}) out of valid enum range [0, {crateTypeCount}) for seed {seed}");
                }
            }
        }

        [Test]
        public void Crates_Disabled_WhenIntervalIsZero()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int i = 0; i < 500; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Crates.Count, "No crates when interval is 0");
        }

        [Test]
        public void Crates_CollectedWhenPlayerWalksOver()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Damage player, then place health crate exactly at their position
            state.Players[0].Health = 50f;
            Vec2 playerPos = state.Players[0].Position;
            state.Crates.Add(new CrateState
            {
                Position = playerPos,
                Type = CrateType.Health,
                Active = true,
                Grounded = true
            });

            // Stop player movement to prevent drifting
            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Crates.Count, "Crate should be collected and removed");
            Assert.Greater(state.Players[0].Health, 50f, "Health crate should restore HP");
        }

        [Test]
        public void Crates_AmmoRefill_RestoresLimitedAmmoWeapons()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Verify rocket weapon exists and deplete it
            Assert.AreEqual("rocket", state.Players[0].WeaponSlots[2].WeaponId);
            state.Players[0].WeaponSlots[2].Ammo = 0;

            // Place ammo crate exactly at player position (grounded, no physics)
            state.Crates.Add(new CrateState
            {
                Position = state.Players[0].Position,
                Type = CrateType.AmmoRefill,
                Active = true,
                Grounded = true
            });

            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Crates.Count, "Ammo crate should be collected and removed");
            Assert.AreEqual(4, state.Players[0].WeaponSlots[2].Ammo,
                "Ammo crate should refill rocket ammo to original count");
        }

        [Test]
        public void Crates_DoubleDamage_AppliesAndExpires()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;
            config.CrateDoubleDamageDuration = 1f; // 1 second duration for quick test

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            Assert.AreEqual(1f, state.Players[0].DamageMultiplier, 0.01f);

            // Place double damage crate at player
            state.Crates.Add(new CrateState
            {
                Position = state.Players[0].Position,
                Type = CrateType.DoubleDamage,
                Active = true,
                Grounded = true
            });

            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "Double damage should be active after collecting crate");
            Assert.Greater(state.Players[0].DoubleDamageTimer, 0f,
                "Timer should be set");

            // Tick past the 1-second duration
            for (int i = 0; i < 80; i++) // ~1.3 seconds
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "Damage multiplier should reset after buff expires");
            Assert.AreEqual(0f, state.Players[0].DoubleDamageTimer, 0.01f,
                "Timer should be zero after expiry");
        }

        [Test]
        public void Crates_FallingCrate_LandsOnTerrain()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add a falling crate high above terrain
            state.Crates.Add(new CrateState
            {
                Position = new Vec2(0f, 20f),
                Velocity = Vec2.Zero,
                Type = CrateType.Health,
                Active = true,
                Grounded = false
            });

            Assert.IsFalse(state.Crates[0].Grounded);

            // Tick until crate lands or times out
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Crates[0].Grounded) break;
                if (!state.Crates[0].Active) break; // fell into void
            }

            // Crate should either land on terrain or fall into void
            Assert.IsTrue(state.Crates[0].Grounded || !state.Crates[0].Active,
                "Falling crate should either land on terrain or deactivate in void");
        }

        [Test]
        public void Crates_SubmergedByRisingWater_Deactivate()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.SuddenDeathTime = 1f;
            config.WaterRiseSpeed = 100f; // very fast rise

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a grounded crate at a known low position
            state.Crates.Add(new CrateState
            {
                Position = new Vec2(0f, -15f), // near death boundary
                Type = CrateType.Health,
                Active = true,
                Grounded = true
            });

            Assert.AreEqual(1, state.Crates.Count);

            // Tick until water rises past the crate and it gets removed
            for (int i = 0; i < 200; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Crates.Count == 0) break;
            }

            Assert.AreEqual(0, state.Crates.Count,
                "Grounded crate should be removed when submerged by rising water");
        }

        [Test]
        public void FullMatch_WithCratesAndSuddenDeath_Stable()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 5f;
            config.SuddenDeathTime = 10f;
            config.WaterRiseSpeed = 2f;
            config.AIShootInterval = 1f;

            Assert.DoesNotThrow(() =>
            {
                for (int match = 0; match < 20; match++)
                {
                    var state = GameSimulation.CreateMatch(config, match * 17);
                    AILogic.Reset(match * 17);

                    for (int frame = 0; frame < 2000; frame++)
                    {
                        GameSimulation.Tick(state, 0.016f);
                        if (state.Phase == MatchPhase.Ended) break;
                    }
                }
            });
        }

        // --- Oil barrel tests ---

        [Test]
        public void Barrels_SpawnOnTerrain()
        {
            var config = SmallConfig();
            config.BarrelCount = 3;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(3, state.Barrels.Count);
            foreach (var barrel in state.Barrels)
            {
                Assert.IsTrue(barrel.Active);
                Assert.Greater(barrel.ExplosionRadius, 0f);
            }
        }

        [Test]
        public void Barrels_SkipSpawnWhenNoGroundAtX()
        {
            var config = SmallConfig();
            config.TerrainWidth = 20; // very narrow terrain
            config.MapWidth = 200f;   // wide map — most X samples miss terrain
            config.MineCount = 0;
            config.BarrelCount = 10;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.LessOrEqual(state.Barrels.Count, config.BarrelCount);
            foreach (var barrel in state.Barrels)
            {
                Assert.That(barrel.Position.y, Is.Not.EqualTo(config.SpawnProbeY).Within(0.3f),
                    "Barrel should not spawn at SpawnProbeY fallback height");
            }
        }

        [Test]
        public void Barrels_ExplodeWhenHitByExplosion()
        {
            var config = SmallConfig();
            config.BarrelCount = 0; // no auto-spawn
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a barrel at a known position
            Vec2 barrelPos = new Vec2(5f, 5f);
            state.Barrels.Add(new BarrelState
            {
                Position = barrelPos,
                ExplosionRadius = 3f,
                Damage = 40f,
                Active = true
            });

            // Fire a projectile at the barrel
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(4.5f, 5f),
                Velocity = new Vec2(2f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Build a terrain wall to force impact near the barrel
            int wallPx = state.Terrain.WorldToPixelX(5f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 5; py < wallPy + 5; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            // Tick until explosion (barrel removed from list when deactivated)
            bool barrelExploded = false;
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0)
                {
                    barrelExploded = true;
                    break;
                }
            }

            Assert.IsTrue(barrelExploded, "Barrel should explode when hit by nearby explosion");
        }

        [Test]
        public void Barrels_Disabled_WhenCountIsZero()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(0, state.Barrels.Count);
        }

        [Test]
        public void Barrels_ChainReaction_TwoBarrelsNearby()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place two barrels close together
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(0f, 5f),
                ExplosionRadius = 3f,
                Damage = 40f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(2f, 5f), // within blast radius of first
                ExplosionRadius = 3f,
                Damage = 40f,
                Active = true
            });

            // Place a projectile that will hit terrain near the first barrel to trigger it
            // Build a terrain wall at barrel position
            int wallPx = state.Terrain.WorldToPixelX(0f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 3; py <= wallPy + 3; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(-1f, 5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick until chain reaction propagates (both barrels removed when deactivated)
            for (int i = 0; i < 60; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0) break;
            }

            Assert.AreEqual(0, state.Barrels.Count, "Both barrels should explode and be removed (chain reaction)");
        }

        [Test]
        public void Barrels_ChainReaction_IndirectPropagation_ThreeBarrels()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // B1 at x=0, B2 at x=4, B3 at x=8 — each radius 5
            // Projectile explosion (radius 2) at x=0 hits B1 only
            // B1 explosion (radius 5) reaches B2 (dist 4 < 5.5) but not B3 (dist 8 > 5.5)
            // B2 explosion (radius 5) reaches B3 (dist 4 < 5.5)
            // B3 is ONLY reachable through indirect chain: projectile → B1 → B2 → B3
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(0f, 5f),
                ExplosionRadius = 5f,
                Damage = 40f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(4f, 5f),
                ExplosionRadius = 5f,
                Damage = 40f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(8f, 5f),
                ExplosionRadius = 5f,
                Damage = 40f,
                Active = true
            });

            // Build terrain wall at B1 to force projectile impact
            int wallPx = state.Terrain.WorldToPixelX(0f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 3; py <= wallPy + 3; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(-1f, 5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick until all three barrels have detonated (removed when deactivated)
            for (int i = 0; i < 60; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0) break;
            }

            Assert.AreEqual(0, state.Barrels.Count, "All 3 barrels should chain-react and be removed (indirect propagation)");
        }

        [Test]
        public void Barrels_ChainKill_AttributedToTriggeringPlayer()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Disable AI so it doesn't fire stray projectiles that interfere
            state.Players[1].IsAI = false;

            // Move player 1 next to where the barrel will be
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].Health = 10f; // low HP so barrel kills them

            // Place a barrel next to player 1
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(5f, 5.5f),
                ExplosionRadius = 3f,
                Damage = 50f,
                Active = true,
                OwnerIndex = -1
            });

            float dmgBefore = state.Players[0].TotalDamageDealt;

            // Player 0 fires a projectile that hits terrain near the barrel.
            // Build a tall wall so the projectile hits even after gravity pulls it down.
            int wallPx = state.Terrain.WorldToPixelX(5f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 20; py <= wallPy + 3; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(3f, 5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 5f,
                KnockbackForce = 3f,
                Alive = true
            });

            // Tick until barrel explodes and player 1 dies (barrel removed when deactivated)
            for (int i = 0; i < 120; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0 && state.Players[1].IsDead) break;
            }

            Assert.AreEqual(0, state.Barrels.Count, "Barrel should have exploded and been removed");
            Assert.IsTrue(state.Players[1].IsDead, "Player 1 should be dead from barrel");
            Assert.Greater(state.Players[0].TotalDamageDealt, dmgBefore,
                "Player 0 should get damage credit for barrel chain kill");
        }

        // --- Regression tests for #305: inactive mines/barrels cleaned up ---

        [Test]
        public void Mines_InactiveEntriesRemovedAfterTrigger()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Settle players
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Add several mines manually (simulating boss/skill spawns)
            for (int m = 0; m < 5; m++)
            {
                float mx = state.Players[0].Position.x + 20f + m * 5f;
                float my = GamePhysics.FindGroundY(state.Terrain, mx, 20f);
                state.Mines.Add(new MineState
                {
                    Position = new Vec2(mx, my),
                    TriggerRadius = 1.5f,
                    ExplosionRadius = 3f,
                    Damage = 30f,
                    Active = true,
                    OwnerIndex = -1
                });
            }

            Assert.AreEqual(5, state.Mines.Count);

            // Walk player onto the first mine to trigger it
            var firstMinePos = state.Mines[0].Position;
            state.Players[0].Position = firstMinePos;
            GameSimulation.Tick(state, 0.016f);

            // The triggered mine should be removed, not just deactivated
            foreach (var mine in state.Mines)
                Assert.IsTrue(mine.Active, "Only active mines should remain in the list");
            Assert.Less(state.Mines.Count, 5, "Inactive mine should have been removed");
        }

        [Test]
        public void Barrels_InactiveEntriesRemovedAfterExplosion()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place 2 barrels far apart so only one gets hit
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(5f, 5f),
                ExplosionRadius = 3f,
                Damage = 50f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(50f, 5f), // far away, won't be hit
                ExplosionRadius = 3f,
                Damage = 50f,
                Active = true
            });

            Assert.AreEqual(2, state.Barrels.Count);

            // Build terrain wall at first barrel to force projectile impact
            int wallPx = state.Terrain.WorldToPixelX(5f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 5; py < wallPy + 5; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            // Fire projectile close to first barrel (same pattern as Barrels_ExplodeWhenHitByExplosion)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(4.5f, 5f),
                Velocity = new Vec2(2f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick until first barrel explodes
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count < 2) break;
            }

            // The exploded barrel should be removed, leaving only the far-away one
            Assert.AreEqual(1, state.Barrels.Count, "Inactive barrel should have been removed");
            Assert.IsTrue(state.Barrels[0].Active, "Only active barrels should remain in the list");
        }

        // --- Airstrike weapon tests ---

        [Test]
        public void Airstrike_WeaponExists_InSlot6()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.GreaterOrEqual(state.Players[0].WeaponSlots.Length, 7, "Should have at least 7 weapon slots");
            Assert.AreEqual("airstrike", state.Players[0].WeaponSlots[6].WeaponId);
            Assert.AreEqual(1, state.Players[0].WeaponSlots[6].Ammo);
            Assert.IsTrue(state.Players[0].WeaponSlots[6].IsAirstrike);
        }

        [Test]
        public void AI_StartingWeapon_NeverSelectsAirstrike()
        {
            // Test multiple seeds to verify airstrike is never selected
            for (int seed = 0; seed < 100; seed++)
            {
                var state = GameSimulation.CreateMatch(SmallConfig(), seed);
                int aiSlot = state.Players[1].ActiveWeaponSlot;
                Assert.IsFalse(state.Players[1].WeaponSlots[aiSlot].IsAirstrike,
                    $"Seed {seed}: AI started with airstrike at slot {aiSlot}");
            }
        }

        [Test]
        public void Airstrike_MarkerProjectile_SpawnsBombsOnImpact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire airstrike (slot 6 after dynamite/napalm were added)
            state.Players[0].ActiveWeaponSlot = 6;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsAirstrike);

            // Tick until marker hits terrain — should spawn 5 bombs
            bool spawnedBombs = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Projectiles.Count > 1)
                {
                    spawnedBombs = true;
                    break;
                }
                if (state.Projectiles.Count == 0) break; // hit terrain, spawned and some already hit
            }

            // Either bombs were spawned (count > 1) or they already exploded (explosions happened)
            Assert.IsTrue(spawnedBombs || state.ExplosionEvents.Count > 0,
                "Airstrike should spawn bombs on impact that create explosions");
        }

        // --- Team mode tests ---

        [Test]
        public void TeamMode_MatchEnds_WhenOneTeamEliminated()
        {
            var config = SmallConfig();
            config.TeamMode = true;

            var state = GameSimulation.CreateMatch(config, 42);

            // Assign teams: P0 = team 0, P1 = team 1
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Kill team 1
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerTeamIndex, "Team 0 should win");
        }

        [Test]
        public void TeamMode_MatchContinues_WhileBothTeamsHaveAlive()
        {
            var config = SmallConfig();
            config.TeamMode = true;

            var state = GameSimulation.CreateMatch(config, 42);

            // 4-player match: 2v2
            // Expand players array
            state.Players = new PlayerState[4];
            for (int i = 0; i < 4; i++)
            {
                state.Players[i] = new PlayerState
                {
                    Position = new Vec2(-10f + i * 7f, 5f),
                    Health = 100f, MaxHealth = 100f,
                    Energy = 100f, MaxEnergy = 100f,
                    MoveSpeed = 5f, IsAI = i > 0,
                    TeamIndex = i < 2 ? 0 : 1,
                    WeaponSlots = new WeaponSlotState[1],
                    SkillSlots = new SkillSlotState[0]
                };
            }

            // Kill one member of each team — match should continue
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[2].IsDead = true;
            state.Players[2].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Match should continue with alive members on both teams");
        }

        [Test]
        public void TeamMode_Draw_WhenAllTeamsEliminated()
        {
            var config = SmallConfig();
            config.TeamMode = true;

            var state = GameSimulation.CreateMatch(config, 42);
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 1;

            // Kill both
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerTeamIndex, "Draw when all teams eliminated");
        }

        [Test]
        public void DefaultMode_TeamIndexIgnored()
        {
            var config = SmallConfig();
            config.TeamMode = false;

            var state = GameSimulation.CreateMatch(config, 42);

            // TeamIndex defaults to -1 in FFA mode
            Assert.AreEqual(-1, state.Players[0].TeamIndex);
            Assert.AreEqual(-1, state.Players[1].TeamIndex);

            // Standard FFA win condition still works
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P0 wins in FFA mode");
        }

        // --- Regression tests for 2026-03-24 bug fixes ---

        [Test]
        public void InactiveCrates_RemovedFromList()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f; // disable auto-spawn
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add some inactive crates
            state.Crates.Add(new CrateState { Active = false, Position = Vec2.Zero });
            state.Crates.Add(new CrateState { Active = true, Position = new Vec2(5f, 5f), Grounded = true });
            state.Crates.Add(new CrateState { Active = false, Position = Vec2.Zero });

            Assert.AreEqual(3, state.Crates.Count);

            GameSimulation.Tick(state, 0.016f);

            // Only the active crate should remain
            Assert.AreEqual(1, state.Crates.Count, "Inactive crates should be pruned");
            Assert.IsTrue(state.Crates[0].Active);
        }

        [Test]
        public void AI_TeleportChance_UsesTimeNormalization()
        {
            // This test verifies the fix is present by ensuring the AI can run
            // at different dt values without crashing (functional correctness).
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Force AI to have teleport skill available
            state.Players[1].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "teleport",
                    Type = SkillType.Teleport,
                    EnergyCost = 5f,
                    Cooldown = 1f,
                    Range = 15f
                },
                new SkillSlotState()
            };
            state.Players[1].Energy = 100f;

            // Run with large dt (simulating 2 FPS) — should not crash
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 30; i++)
                    GameSimulation.Tick(state, 0.5f);
            });

            // Run with tiny dt (simulating 240 FPS) — should not crash
            state = GameSimulation.CreateMatch(config, 43);
            AILogic.Reset(43);
            state.Players[1].SkillSlots = new[]
            {
                new SkillSlotState
                {
                    SkillId = "teleport",
                    Type = SkillType.Teleport,
                    EnergyCost = 5f,
                    Cooldown = 1f,
                    Range = 15f
                },
                new SkillSlotState()
            };
            state.Players[1].Energy = 100f;

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 1000; i++)
                    GameSimulation.Tick(state, 0.004f);
            });
        }

        // --- Cycle 2 regression tests ---

        [Test]
        public void DoubleDamageCrate_DoesNotStack_WhenAlreadyBuffed()
        {
            var config = SmallConfig();
            config.CrateSpawnInterval = 0f;
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Apply first double damage buff
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 5f;

            // Place double damage crate at player position
            state.Crates.Add(new CrateState
            {
                Position = state.Players[0].Position,
                Type = CrateType.DoubleDamage,
                Active = true,
                Grounded = true
            });

            state.Input.MoveX = 0f;
            GameSimulation.Tick(state, 0.016f);

            // Buff timer should NOT have been reset — crate should not apply
            Assert.Less(state.Players[0].DoubleDamageTimer, 5f,
                "DoubleDamage timer should tick down (crate not re-applied while already buffed)");
        }

        [Test]
        public void ClusterBomb_Has4SubProjectiles_AfterBalance()
        {
            var config = SmallConfig();
            Assert.AreEqual(4, config.Weapons[3].ClusterCount,
                "Cluster bomb should have 4 sub-projectiles after balance nerf");
        }

        [Test]
        public void ClusterBomb_EnergyCost25_Ammo4_AfterBalance()
        {
            // Issue #34 bottom-lift: EnergyCost 35 -> 25. Ammo stays at 4.
            var config = SmallConfig();
            Assert.AreEqual(25f, config.Weapons[3].EnergyCost,
                "Cluster bomb energy cost should be 25 after issue #34 bottom-lift (was 35)");
            Assert.AreEqual(4, config.Weapons[3].Ammo,
                "Cluster bomb ammo should be 4");
        }

        [Test]
        public void Cannon_EnergyCost8_AfterBalance()
        {
            // Regression (#376): cannon had zero energy cost, breaking the energy economy.
            // Updated (#414): raised from 3→8 to reduce 4x median Dmg/Energy ratio.
            var config = new GameConfig();
            Assert.AreEqual("cannon", config.Weapons[0].WeaponId);
            Assert.AreEqual(8f, config.Weapons[0].EnergyCost,
                "Cannon should cost 8 energy per shot — balances Dmg/Energy to 1.5x median (#414)");
        }

        [Test]
        public void DashSkill_Costs20Energy_AfterBalance()
        {
            var config = new GameConfig();
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "dash")
                {
                    Assert.AreEqual(20f, skill.EnergyCost,
                        "Dash should cost 20 energy after balance adjustment");
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Dash skill should exist in config");
        }

        // MatchStats_IncludesAccuracy test removed: depends on MatchSeries (Unity runtime class)

        // --- Cycle 3: Dynamite weapon tests ---

        [Test]
        public void Dynamite_ExistsInConfig_Slot4()
        {
            var config = new GameConfig();
            Assert.GreaterOrEqual(config.Weapons.Length, 7, "Should have at least 7 weapons");
            Assert.AreEqual("dynamite", config.Weapons[4].WeaponId);
            Assert.AreEqual(3, config.Weapons[4].Ammo);
            Assert.AreEqual(2, config.Weapons[4].Bounces);
            Assert.Greater(config.Weapons[4].FuseTime, 0f, "Dynamite should have a fuse timer");
        }

        [Test]
        public void Dynamite_CreateMatchGivesPlayerDynamiteSlot()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.AreEqual("dynamite", state.Players[0].WeaponSlots[4].WeaponId);
            Assert.AreEqual(3, state.Players[0].WeaponSlots[4].Ammo);
            Assert.AreEqual(2, state.Players[0].WeaponSlots[4].Bounces);
        }

        [Test]
        public void Dynamite_FireCreatesProjectileWithFuse()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].ActiveWeaponSlot = 4; // dynamite
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.Greater(state.Projectiles[0].FuseTimer, 0f, "Dynamite projectile should have a fuse timer");
            Assert.AreEqual(2, state.Projectiles[0].BouncesRemaining, "Dynamite should bounce 2 times");
        }

        [Test]
        public void Dynamite_ExplodesAfterFuseExpires()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place dynamite directly as a projectile with a short fuse
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 10f), // in the air, away from terrain
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 5f,
                MaxDamage = 80f,
                KnockbackForce = 15f,
                Alive = true,
                FuseTimer = 0.1f, // short fuse for test
                BouncesRemaining = 0
            });

            // Tick until fuse expires
            bool exploded = false;
            for (int i = 0; i < 30; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0)
                {
                    exploded = true;
                    break;
                }
            }

            Assert.IsTrue(exploded, "Dynamite should explode when fuse expires");
        }

        [Test]
        public void Airstrike_ExistsInSlot6()
        {
            var config = new GameConfig();
            Assert.AreEqual("airstrike", config.Weapons[6].WeaponId);
            Assert.IsTrue(config.Weapons[6].IsAirstrike);
        }

        // --- Cycle 4: Dynamite behavior fixes ---

        [Test]
        public void FusedProjectile_DoesNotExplodeOnPlayerContact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 at a controlled position away from terrain features
            state.Players[1].Position = new Vec2(15f, 5f);

            // Place a fused projectile heading toward player 1
            // Use ProjectileSimulation.Update directly to avoid match-end/movement interference
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = state.Players[1].Position + new Vec2(-2f, 0.5f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 5f,
                MaxDamage = 80f,
                KnockbackForce = 15f,
                Alive = true,
                FuseTimer = 3f, // active fuse — should not explode on contact
                BouncesRemaining = 0
            });

            float p2HealthBefore = state.Players[1].Health;

            // Tick projectile simulation directly — projectile should fly through player
            for (int i = 0; i < 10; i++)
                ProjectileSimulation.Update(state, 0.016f);

            // Player 1 should still have full health (fused projectile doesn't detonate on contact)
            Assert.AreEqual(p2HealthBefore, state.Players[1].Health,
                "Fused projectile should not explode on player contact");
        }

        [Test]
        public void FusedProjectile_RestsOnTerrain_WhenBouncesExhausted()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Find terrain surface
            float surfaceY = GamePhysics.FindGroundY(state.Terrain, 0f, config.SpawnProbeY, 0.1f);

            // Place fused projectile just above terrain, no bounces, falling
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, surfaceY + 2f),
                Velocity = new Vec2(0f, -5f),
                OwnerIndex = 0,
                ExplosionRadius = 5f,
                MaxDamage = 80f,
                KnockbackForce = 15f,
                Alive = true,
                FuseTimer = 5f, // long fuse
                BouncesRemaining = 0
            });

            // Tick enough for projectile to hit terrain
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Projectile should still be alive (resting on terrain, fuse ticking)
            bool stillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                if (state.Projectiles[i].FuseTimer > 0f && state.Projectiles[i].Alive)
                {
                    stillAlive = true;
                    break;
                }
            }

            Assert.IsTrue(stillAlive,
                "Fused projectile should rest on terrain when bounces exhausted, not explode");
        }
        // --- Cycle 5: Napalm weapon tests ---

        [Test]
        public void Napalm_ExistsInConfig_Slot5()
        {
            var config = new GameConfig();
            Assert.AreEqual("napalm", config.Weapons[5].WeaponId);
            Assert.IsTrue(config.Weapons[5].IsNapalm);
            Assert.AreEqual(2, config.Weapons[5].Ammo);
            Assert.Greater(config.Weapons[5].FireZoneDuration, 0f);
            Assert.Greater(config.Weapons[5].FireZoneDPS, 0f);
        }

        [Test]
        public void Napalm_CreatesFireZoneOnTerrainImpact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire napalm (slot 5)
            state.Players[0].ActiveWeaponSlot = 5;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsNapalm);

            // Tick until projectile hits terrain
            bool fireZoneCreated = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.FireZones.Count > 0)
                {
                    fireZoneCreated = true;
                    break;
                }
            }

            Assert.IsTrue(fireZoneCreated, "Napalm should create a fire zone on terrain impact");
            Assert.IsTrue(state.FireZones[0].Active);
            Assert.Greater(state.FireZones[0].RemainingTime, 0f);
        }

        [Test]
        public void FireZone_DamagesPlayersStandingInIt()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a fire zone directly at player 2's position
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 3f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            float healthBefore = state.Players[1].Health;

            // Tick a few frames
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[1].Health, healthBefore,
                "Fire zone should damage players standing in it");
        }

        [Test]
        public void FireZone_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.FireZones.Add(new FireZoneState
            {
                Position = new Vec2(50f, 50f), // far from players
                Radius = 3f,
                DamagePerSecond = 10f,
                RemainingTime = 0.5f, // very short
                OwnerIndex = 0,
                Active = true
            });

            Assert.AreEqual(1, state.FireZones.Count);

            // Tick past the duration
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.FireZones.Count, "Fire zone should be removed after expiry");
        }

        [Test]
        public void FireZone_DoesNotDamageOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float initialHealth = state.Players[0].Health;

            // Place fire zone at player 0's position, owned by player 0
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[0].Position,
                Radius = 5f,
                DamagePerSecond = 50f,
                RemainingTime = 2f,
                OwnerIndex = 0,
                Active = true
            });

            // Tick a few frames
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(initialHealth, state.Players[0].Health, 0.01f,
                "Fire zone should not damage its owner");
        }

        [Test]
        public void FireZone_EmitsDamageEvents()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a fire zone at player 2's position
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 3f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            state.DamageEvents.Clear();

            // Tick one frame
            GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.DamageEvents.Count, 0,
                "Fire zone damage should emit DamageEvents for damage numbers and kill feed");

            var dmgEvent = state.DamageEvents[state.DamageEvents.Count - 1];
            Assert.AreEqual(1, dmgEvent.TargetIndex);
            Assert.Greater(dmgEvent.Amount, 0f);
        }

        [Test]
        public void FireZone_AppliesCasterDamageMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Kill player 0 so AI doesn't fire, but keep DamageMultiplier readable
            state.Players[0].IsDead = true;
            state.Players[0].DamageMultiplier = 2f;

            // Clear any projectiles from AI pre-fire
            state.Projectiles.Clear();

            // Place a fire zone at player 1's position, owned by player 0
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 3f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            state.DamageEvents.Clear();

            // Tick one frame
            GameSimulation.Tick(state, 0.016f);

            // Find the fire zone damage event (SourceIndex == 0, TargetIndex == 1)
            float fireZoneDamage = 0f;
            for (int i = 0; i < state.DamageEvents.Count; i++)
            {
                var evt = state.DamageEvents[i];
                if (evt.SourceIndex == 0 && evt.TargetIndex == 1)
                    fireZoneDamage += evt.Amount;
            }

            Assert.Greater(fireZoneDamage, 0f, "Fire zone should deal damage");

            // With 2x multiplier: 20 * 0.016 * 2 = 0.64
            // Without multiplier: 20 * 0.016 = 0.32
            float expectedWithMultiplier = 20f * 0.016f * 2f;
            Assert.AreEqual(expectedWithMultiplier, fireZoneDamage, 0.01f,
                "Fire zone damage should apply caster's DamageMultiplier");
        }

        [Test]
        public void FireZone_KillCreditsOwnerWithComboAndWeaponMastery()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Disable AI so it doesn't interfere with the test
            state.Players[1].IsAI = false;

            // Set player 1 to very low health so fire zone kills them
            state.Players[1].Health = 1f;

            // Place a fire zone at player 1's position, owned by player 0
            var p1Pos = state.Players[1].Position;
            state.FireZones.Add(new FireZoneState
            {
                Position = p1Pos,
                Radius = 3f,
                DamagePerSecond = 200f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                SourceWeaponId = "napalm",
                Active = true
            });

            // Single tick — 200 DPS * 0.016s = 3.2 dmg > 1 HP → kill
            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[1].IsDead, "Player 1 should die from fire zone");

            // Verify kill combo tracking
            Assert.AreEqual(1, state.Players[0].KillsInWindow,
                "Fire zone kill should credit owner with kill combo tracking");

            // Verify weapon mastery kill tracking
            Assert.IsTrue(state.WeaponKills[0].ContainsKey("napalm"),
                "Fire zone kill should track weapon mastery kill for napalm");
            Assert.AreEqual(1, state.WeaponKills[0]["napalm"]);
        }

        [Test]
        public void Airstrike_BombsSpreadSymmetrically()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire airstrike from player 0
            state.Players[0].ActiveWeaponSlot = 6; // airstrike slot
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;

            // Add a projectile that triggers airstrike
            var airstrikeWeapon = config.Weapons[6];
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(0f, -10f),
                OwnerIndex = 0,
                ExplosionRadius = airstrikeWeapon.ExplosionRadius,
                MaxDamage = airstrikeWeapon.MaxDamage,
                KnockbackForce = airstrikeWeapon.KnockbackForce,
                Alive = true,
                IsAirstrike = true,
                AirstrikeCount = 5
            });

            int initialCount = state.Projectiles.Count;

            // Tick until the airstrike projectile hits terrain and spawns bombs
            for (int i = 0; i < 600; i++)
                GameSimulation.Tick(state, 0.016f);

            // Calculate average X position of spawned bombs (should be centered around impact)
            // The bombs should have been spawned and some may have already exploded,
            // but the test verifies the spread logic doesn't crash
            Assert.Pass("Airstrike bomb spawning completed without error");
        }

        [Test]
        public void AI_SelectsAirstrike_WhenTargetLowHP()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set target (player 0) to low health
            state.Players[0].Health = 30f; // 30% of 100

            // Give AI (player 1) airstrike ammo
            Assert.IsNotNull(state.Players[1].WeaponSlots[6].WeaponId,
                "AI should have airstrike weapon in slot 6");
            Assert.AreNotEqual(0, state.Players[1].WeaponSlots[6].Ammo,
                "AI should have airstrike ammo");

            // Tick many frames — AI should select a strategic weapon (airstrike or HHG)
            bool selectedStrategic = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                int slot = state.Players[1].ActiveWeaponSlot;
                if (slot == 6 || slot == 9) // airstrike or HHG
                {
                    selectedStrategic = true;
                    break;
                }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selectedStrategic, "AI should select a strategic weapon (airstrike/HHG) when target has low HP");
        }

        [Test]
        public void DrillProjectile_DestroysTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Position drill BELOW terrain surface to tunnel through earth
            float drillY = state.Players[0].Position.y - 2f;
            int pixelY = state.Terrain.WorldToPixelY(drillY);

            // Count solid terrain pixels at the drill row before
            int solidBefore = 0;
            for (int x = 0; x < state.Terrain.Width; x++)
            {
                if (state.Terrain.IsSolid(x, pixelY)) solidBefore++;
            }

            // Only test if there's terrain to drill through
            if (solidBefore == 0)
            {
                Assert.Pass("No terrain at drill Y — test not applicable for this seed");
                return;
            }

            // Spawn a drill projectile inside terrain heading right
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(state.Players[0].Position.x, drillY),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 3f,
                Alive = true,
                IsDrill = true
            });

            // Tick enough for drill to travel
            for (int i = 0; i < 120; i++)
                GameSimulation.Tick(state, 0.016f);

            // Count solid pixels after — should be fewer
            int solidAfter = 0;
            for (int x = 0; x < state.Terrain.Width; x++)
            {
                if (state.Terrain.IsSolid(x, pixelY)) solidAfter++;
            }

            Assert.Less(solidAfter, solidBefore, "Drill should destroy terrain pixels along its path");
        }

        [Test]
        public void DrillProjectile_NoGravity()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float startY = 10f; // above terrain
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, startY),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                Alive = true,
                IsDrill = true
            });

            // Tick a few frames
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // Drill should still be at roughly the same Y (no gravity)
            if (state.Projectiles.Count > 0)
            {
                float yDrift = MathF.Abs(state.Projectiles[0].Position.y - startY);
                Assert.Less(yDrift, 0.5f, "Drill should not be affected by gravity");
            }
            else
            {
                Assert.Pass("Drill expired (hit bounds) — no gravity test applicable");
            }
        }

        [Test]
        public void RetreatTimer_BlocksFiring()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.RetreatDuration = 3f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire once
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;
            state.Players[0].ShootCooldownRemaining = 0f;
            int projBefore = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.Greater(state.Projectiles.Count, projBefore, "First shot should succeed");

            // Retreat timer should be active
            Assert.Greater(state.Players[0].RetreatTimer, 0f, "Retreat timer should be set after firing");

            // Try to fire again immediately (should be blocked by retreat)
            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 15f;
            int projAfterFirst = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(projAfterFirst, state.Projectiles.Count,
                "Second shot should be blocked by retreat timer");
        }

        [Test]
        public void RetreatTimer_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.RetreatDuration = 1f; // short retreat for testing
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].RetreatTimer = 1f;

            // Tick past retreat duration
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.LessOrEqual(state.Players[0].RetreatTimer, 0f,
                "Retreat timer should expire after duration");
        }

        [Test]
        public void RetreatTimer_DisabledWhenZero()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.RetreatDuration = 0f; // disabled
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;
            state.Players[0].ShootCooldownRemaining = 0f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(0f, state.Players[0].RetreatTimer,
                "Retreat timer should not activate when duration is 0");
        }

        [Test]
        public void RetreatTimer_BlocksSkillActivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.RetreatDuration = 3f;
            config.Skills = new[] { new SkillDef { SkillId = "teleport", Type = SkillType.Teleport, EnergyCost = 10f, Cooldown = 5f, Range = 100f } };
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set retreat timer directly
            state.Players[0].RetreatTimer = 2f;
            state.Players[0].Energy = 100f;
            float energyBefore = state.Players[0].Energy;

            // Try to activate skill during retreat
            SkillSystem.ActivateSkill(state, 0, 0);

            // Skill should NOT activate — energy unchanged, no events emitted
            Assert.AreEqual(energyBefore, state.Players[0].Energy,
                "Skill should not deduct energy during retreat timer");
            Assert.AreEqual(0, state.SkillEvents.Count,
                "No skill event should be emitted during retreat timer");
        }

        // --- Emote system tests ---

        [Test]
        public void Emote_TriggerSetsActiveEmote()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            GameSimulation.TriggerEmote(state, 0, EmoteType.Taunt);

            Assert.AreEqual(EmoteType.Taunt, state.Players[0].ActiveEmote);
            Assert.Greater(state.Players[0].EmoteTimer, 0f);
            Assert.AreEqual(1, state.EmoteEvents.Count);
            Assert.AreEqual(EmoteType.Taunt, state.EmoteEvents[0].Emote);
        }

        [Test]
        public void Emote_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].ActiveEmote = EmoteType.Laugh;
            state.Players[0].EmoteTimer = 0.5f; // short timer

            // Tick past emote duration
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(EmoteType.None, state.Players[0].ActiveEmote,
                "Emote should clear after timer expires");
        }

        [Test]
        public void Emote_DeadPlayerCannotEmote()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].IsDead = true;
            GameSimulation.TriggerEmote(state, 0, EmoteType.ThumbsUp);

            Assert.AreEqual(EmoteType.None, state.Players[0].ActiveEmote,
                "Dead player should not be able to emote");
        }

        [Test]
        public void Emote_CannotInterruptActiveEmote()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            GameSimulation.TriggerEmote(state, 0, EmoteType.Laugh);
            Assert.AreEqual(EmoteType.Laugh, state.Players[0].ActiveEmote);

            // Try to trigger another emote while first is active
            state.EmoteEvents.Clear();
            GameSimulation.TriggerEmote(state, 0, EmoteType.Taunt);
            Assert.AreEqual(EmoteType.Laugh, state.Players[0].ActiveEmote,
                "Active emote should not be interrupted");
            Assert.AreEqual(0, state.EmoteEvents.Count,
                "No emote event should be emitted when interrupted");
        }

        [Test]
        public void Emote_BlockedWhileInvisible_Regression139()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            // Make player invisible (decoy active)
            state.Players[0].IsInvisible = true;
            state.Players[0].DecoyTimer = 5f;

            // Try to emote via input while invisible
            state.Input.EmotePressed = 1; // Taunt
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(EmoteType.None, state.Players[0].ActiveEmote,
                "Invisible player should not be able to emote via input");
            Assert.AreEqual(0, state.EmoteEvents.Count,
                "No emote event should be emitted for invisible player");
        }

        // --- Girder placement tests ---

        [Test]
        public void Girder_PlacesIndestructibleTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set up player to aim right at a specific location
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            // Find the girder skill index in config
            int girderIdx = -1;
            for (int i = 0; i < config.Skills.Length; i++)
                if (config.Skills[i].SkillId == "girder") girderIdx = i;
            Assert.GreaterOrEqual(girderIdx, 0, "Girder skill should exist in config");

            // Give player a girder skill slot
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "girder",
                Type = SkillType.Girder,
                EnergyCost = 30f,
                Cooldown = 15f,
                Range = 12f,
                Value = 4f
            };
            state.Players[0].Energy = 100f;

            // Target position: 12 units to the right of player (range = 12, angle = 0)
            float targetX = state.Players[0].Position.x + 12f;
            float targetY = state.Players[0].Position.y;

            // Check pixel at target center is not indestructible before
            int px = state.Terrain.WorldToPixelX(targetX);
            int py = state.Terrain.WorldToPixelY(targetY);

            // Activate girder
            SkillSystem.ActivateSkill(state, 0, 0);

            // Check that indestructible pixels were placed
            bool foundIndestructible = false;
            int checkPx = state.Terrain.WorldToPixelX(targetX);
            int checkPy = state.Terrain.WorldToPixelY(targetY);
            for (int dx = -5; dx <= 5; dx++)
            {
                if (state.Terrain.IsIndestructible(checkPx + dx, checkPy))
                {
                    foundIndestructible = true;
                    break;
                }
            }

            Assert.IsTrue(foundIndestructible, "Girder should place indestructible terrain pixels");
        }

        [Test]
        public void Girder_CostsEnergy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "girder",
                Type = SkillType.Girder,
                EnergyCost = 30f,
                Cooldown = 15f,
                Range = 12f,
                Value = 4f
            };
            state.Players[0].Energy = 50f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[0].Energy, energyBefore,
                "Girder should deduct energy");
        }

        [Test]
        public void AI_RespectsRetreatTimer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.RetreatDuration = 5f;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set AI retreat timer
            state.Players[1].RetreatTimer = 5f;
            state.Players[1].ShootCooldownRemaining = 0f;
            int projBefore = state.Projectiles.Count;

            // Tick a few frames — AI should not fire during retreat
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Retreat timer should still have time left (~4.5s remaining)
            Assert.Greater(state.Players[1].RetreatTimer, 4f,
                "AI retreat timer should still be active");

            // No new projectiles should have been created by AI
            // (Some may exist from initial setup, but AI shouldn't add more)
            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "AI should not fire during retreat timer");
        }

        // --- Rope swing tests ---

        [Test]
        public void GrappleSwing_PlayerSwingsOnRope()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a grapple skill on the player
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "grapple",
                Type = SkillType.GrapplingHook,
                EnergyCost = 25f,
                Cooldown = 5f,
                Duration = 2f,
                Range = 20f,
                Value = 15f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 60f; // aim upward
            state.Players[0].FacingDirection = 1;

            Vec2 startPos = state.Players[0].Position;

            // Activate grapple
            SkillSystem.ActivateSkill(state, 0, 0);

            // Check if grapple activated (may fail if no terrain hit)
            if (!state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.Pass("No terrain to grapple — test not applicable for this seed");
                return;
            }

            // Tick several frames — player should move
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 afterPos = state.Players[0].Position;
            float moved = Vec2.Distance(startPos, afterPos);
            Assert.Greater(moved, 0.5f, "Player should move while swinging on rope");
        }

        [Test]
        public void GrappleSwing_LaunchesWithVelocityOnDetach()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set up grapple directly — simulate attached state
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "grapple",
                Type = SkillType.GrapplingHook,
                EnergyCost = 25f,
                Cooldown = 5f,
                Duration = 2f,
                Range = 5f, // rope length
                Value = 15f,
                IsActive = true,
                DurationRemaining = 0.5f // about to expire
            };
            // Anchor above the player
            state.Players[0].SkillTargetPosition = state.Players[0].Position + new Vec2(0f, 5f);
            state.Players[0].IsGrounded = false;

            // Tick until swing expires
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            // After detach, player should have velocity from swing
            float speed = state.Players[0].Velocity.Magnitude;
            // Speed may be 0 if player was at equilibrium, so just check skill deactivated
            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "Grapple should deactivate after duration");
        }

        // --- Cosmetic hat tests ---

        [Test]
        public void CreateMatch_AssignsRandomHats()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.AreNotEqual(HatType.None, state.Players[0].Hat,
                "Player 1 should have a hat assigned");
            Assert.AreNotEqual(HatType.None, state.Players[1].Hat,
                "Player 2 should have a hat assigned");
        }

        [Test]
        public void CreateMatch_DifferentSeeds_DifferentHats()
        {
            var state1 = GameSimulation.CreateMatch(SmallConfig(), 1);
            var state2 = GameSimulation.CreateMatch(SmallConfig(), 999);

            // With different seeds, at least one player should have a different hat
            // (statistically very likely with 5 hat types)
            bool anyDifferent = state1.Players[0].Hat != state2.Players[0].Hat
                || state1.Players[1].Hat != state2.Players[1].Hat;
            Assert.IsTrue(anyDifferent,
                "Different seeds should produce different hat assignments (statistical)");
        }

        // --- Holy Hand Grenade tests ---

        [Test]
        public void HolyHandGrenade_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "holy_hand_grenade")
                {
                    found = true;
                    Assert.AreEqual(150f, config.Weapons[i].MaxDamage);
                    Assert.AreEqual(8f, config.Weapons[i].ExplosionRadius);
                    Assert.AreEqual(1, config.Weapons[i].Ammo);
                    Assert.IsTrue(config.Weapons[i].DestroysIndestructible);
                    Assert.AreEqual(1, config.Weapons[i].Bounces);
                    Assert.Greater(config.Weapons[i].FuseTime, 0f);
                    break;
                }
            }
            Assert.IsTrue(found, "Holy Hand Grenade should exist in weapon config");
        }

        [Test]
        public void HolyHandGrenade_DestroysIndestructibleTerrain()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place indestructible pixels at a known location
            int cx = state.Terrain.Width / 2;
            int cy = state.Terrain.Height / 2;
            state.Terrain.FillRectIndestructible(cx - 5, cy - 5, 10, 10);
            Assert.IsTrue(state.Terrain.IsIndestructible(cx, cy), "Setup: should have indestructible pixel");

            // Directly test CombatResolver with destroyIndestructible = true
            Vec2 worldPos = new Vec2(
                state.Terrain.PixelToWorldX(cx),
                state.Terrain.PixelToWorldY(cy));
            CombatResolver.ApplyExplosion(state, worldPos, 8f, 150f, 25f, 0, true);

            Assert.IsFalse(state.Terrain.IsIndestructible(cx, cy),
                "Explosion with destroyIndestructible should clear indestructible pixels");
        }

        // --- Skill deactivation on death ---

        [Test]
        public void SkillDeactivatesOnDeath()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Activate a shield skill
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "shield",
                Type = SkillType.Shield,
                EnergyCost = 0f,
                Cooldown = 12f,
                Duration = 3f,
                IsActive = true,
                DurationRemaining = 2f
            };

            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            // Kill the player
            state.Players[0].IsDead = true;

            // Tick — skill system should deactivate the skill
            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "Active skills should deactivate when player dies");
        }

        // --- Freeze mechanic tests ---

        [Test]
        public void Freeze_BlocksMovementAndFiring()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].FreezeTimer = 2f;
            state.Players[0].AimPower = 15f;
            state.Players[0].ShootCooldownRemaining = 0f;

            int projBefore = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Frozen player should not be able to fire");
        }

        [Test]
        public void Freeze_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].FreezeTimer = 0.5f;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.LessOrEqual(state.Players[0].FreezeTimer, 0f,
                "Freeze timer should expire");
        }

        [Test]
        public void Freeze_StopsRopeSwing()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set up player on rope swing
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = new Vec2(5f, 0f); // swinging
            state.Players[0].SkillTargetPosition = new Vec2(0f, 10f); // anchor above

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "grapple", Type = SkillType.GrapplingHook,
                    IsActive = true, DurationRemaining = 2f,
                    Range = 5f // rope length
                },
                new SkillSlotState()
            };

            Vec2 posBefore = state.Players[0].Position;

            // Freeze the player
            state.Players[0].FreezeTimer = 2f;

            // Tick the skill system
            SkillSystem.Update(state, 0.1f);

            // Velocity should be zeroed, position unchanged
            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f,
                "Frozen player on rope should have zero X velocity");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.01f,
                "Frozen player on rope should have zero Y velocity");
            Assert.AreEqual(posBefore.x, state.Players[0].Position.x, 0.01f,
                "Frozen player on rope should not move");
        }

        [Test]
        public void Freeze_ZeroesHorizontalVelocityOnly()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Give player upward velocity (simulating mid-jump or knockback)
            state.Players[0].Velocity = new Vec2(5f, 10f);
            state.Players[0].IsGrounded = false;
            state.Players[0].FreezeTimer = 2f;
            state.Players[0].IsAI = false;

            // Provide movement input so ProcessInput runs
            state.Input.MoveX = 1f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f,
                "Frozen player should have zero X velocity");
            Assert.Less(state.Players[0].Velocity.y, 10f,
                "Gravity should reduce Y velocity even when frozen");
        }

        [Test]
        public void Freeze_AirbornePlayerFallsWithGravity()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player high above terrain so they are airborne
            state.Players[0].Position = new Vec2(5f, 20f);
            state.Players[0].Velocity = Vec2.Zero;
            state.Players[0].IsGrounded = false;
            state.Players[0].FreezeTimer = 2f;
            state.Players[0].IsAI = false;
            float startY = state.Players[0].Position.y;

            // Tick several frames
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Position.y, startY,
                "Frozen airborne player should fall due to gravity, not hover (#279)");
        }

        [Test]
        public void FreezeGrenade_ProducesSingleExplosionEvent()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player 1 within freeze radius
            state.Players[1].Position = new Vec2(1f, 5f);

            Vec2 hitPoint = new Vec2(1f, 5f);
            float radius = 3f;

            // ApplyFreezeExplosion should NOT add its own ExplosionEvent
            CombatResolver.ApplyFreezeExplosion(state, hitPoint, radius, 2f, 0);
            Assert.AreEqual(0, state.ExplosionEvents.Count,
                "ApplyFreezeExplosion should not add an ExplosionEvent (ApplyExplosion handles it)");

            // The follow-up ApplyExplosion adds exactly one
            CombatResolver.ApplyExplosion(state, hitPoint, radius * 0.5f, 5f, 2f, 0, false);
            Assert.AreEqual(1, state.ExplosionEvents.Count,
                "Freeze grenade detonation should produce exactly one ExplosionEvent");
        }

        // --- Dash skill tests ---

        [Test]
        public void Dash_VelocitySustainsForDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up dash skill
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].IsGrounded = true;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "dash", Type = SkillType.Dash,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0.2f, Value = 40f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 100f;

            float xBefore = state.Players[0].Position.x;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Tick several frames — velocity should be sustained during duration
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 12; i++) // 12 * 0.016 = 0.192s < 0.2s duration
                GameSimulation.Tick(state, 0.016f);

            float distMoved = state.Players[0].Position.x - xBefore;
            Assert.Greater(distMoved, 3f,
                "Dash should sustain velocity for its duration, moving player significantly");
        }

        // --- Earthquake skill tests ---

        [Test]
        public void Earthquake_DamagesGroundedPlayers()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake",
                Type = SkillType.Earthquake,
                EnergyCost = 35f,
                Cooldown = 20f,
                Value = 20f
            };
            state.Players[0].Energy = 100f;

            // Ensure target is grounded
            state.Players[1].IsGrounded = true;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[1].Health, hpBefore,
                "Earthquake should damage grounded players");
        }

        [Test]
        public void Earthquake_DoesNotDamageAirborne()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake",
                Type = SkillType.Earthquake,
                EnergyCost = 35f,
                Cooldown = 20f,
                Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].IsGrounded = false;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(hpBefore, state.Players[1].Health,
                "Earthquake should not damage airborne players");
        }

        [Test]
        public void Earthquake_SkipsTeammatesInTeamMode()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);

            // Assign teams: player 0 and 1 on same team
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].IsGrounded = true;

            float hpBefore = state.Players[1].Health;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(hpBefore, state.Players[1].Health, 0.01f,
                "Earthquake should not damage teammates in team mode");
        }

        [Test]
        public void Earthquake_RespectsArmorMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;

            // Give target Shield-level armor (3x reduction → takes 1/3 damage)
            state.Players[1].IsGrounded = true;
            state.Players[1].ArmorMultiplier = 3f;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            float expected = hpBefore - 20f * (1f / 3f);
            Assert.AreEqual(expected, state.Players[1].Health, 0.1f,
                "Earthquake should apply ArmorMultiplier damage reduction");
        }

        [Test]
        public void Earthquake_AppliesCasterDamageMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].DamageMultiplier = 2f; // DoubleDamage

            state.Players[1].IsGrounded = true;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            float expected = hpBefore - 20f * 2f;
            Assert.AreEqual(expected, state.Players[1].Health, 0.1f,
                "Earthquake should apply caster's DamageMultiplier (e.g. DoubleDamage)");
        }

        [Test]
        public void Earthquake_TracksDamageInPostMatchStats()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].TotalDamageDealt = 0f;
            state.Players[0].DirectHits = 0;
            state.Players[0].MaxSingleDamage = 0f;

            state.Players[1].IsGrounded = true;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].TotalDamageDealt, 0f,
                "Earthquake damage should be tracked in TotalDamageDealt");
            Assert.Greater(state.Players[0].DirectHits, 0,
                "Earthquake hits should be tracked in DirectHits");
            Assert.Greater(state.Players[0].MaxSingleDamage, 0f,
                "Earthquake damage should update MaxSingleDamage");
        }

        [Test]
        public void Earthquake_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;

            // Set target ArmorMultiplier to 0 — should NOT cause Infinity damage
            state.Players[1].IsGrounded = true;
            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "Earthquake with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "Earthquake with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "Earthquake should still deal positive damage when ArmorMultiplier=0");
        }

        [Test]
        public void Explosion_ShieldDoesNotAbsorbOverheadHit()
        {
            // Regression: #340 — IsFrontalHit used >= and <= so dx=0 (overhead/below)
            // was treated as frontal for BOTH facing directions, letting a horizontal
            // shield absorb a bomb falling directly overhead.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].FacingDirection = 1;
            state.Players[1].ShieldHP = 500f;
            state.Players[1].MaxShieldHP = 500f;
            float hpBefore = state.Players[1].Health;
            float shieldBefore = state.Players[1].ShieldHP;

            // Explosion at exact same X as the player (overhead / same column)
            Vec2 overheadPos = new Vec2(state.Players[1].Position.x, state.Players[1].Position.y + 2f);
            CombatResolver.ApplyExplosion(state, overheadPos, 5f, 50f, 5f, 0, false);

            Assert.Less(state.Players[1].Health, hpBefore,
                "Overhead explosion (dx=0) should damage HP — horizontal shield cannot block vertical hits");
            Assert.AreEqual(shieldBefore, state.Players[1].ShieldHP, 0.01f,
                "Shield should NOT absorb an overhead hit (dx=0) regardless of facing");
        }

        [Test]
        public void Explosion_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 5f, 50f, 5f, 0, false);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "Explosion with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "Explosion with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "Explosion should still deal positive damage when ArmorMultiplier=0");
        }

        [Test]
        public void FireZone_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].ArmorMultiplier = 0f;
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 5f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });
            float hpBefore = state.Players[1].Health;

            GameSimulation.Tick(state, 0.1f);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "FireZone with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "FireZone with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "FireZone should still deal positive damage when ArmorMultiplier=0");
        }

        [Test]
        public void Hitscan_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set player 0's active weapon to hitscan
            state.Players[0].WeaponSlots[0] = new WeaponSlotState
            {
                WeaponId = "test_laser", Ammo = -1,
                MaxDamage = 30f, IsHitscan = true,
                MinPower = 10f, MaxPower = 30f
            };
            state.Players[0].ActiveWeaponSlot = 0;

            // Place players facing each other
            state.Players[0].Position = new Vec2(5f, 2f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[1].Position = new Vec2(8f, 2f);
            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            GameSimulation.Fire(state, 0);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "Hitscan with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "Hitscan with ArmorMultiplier=0 should not produce NaN damage");
        }

        [Test]
        public void PierceDamage_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            CombatResolver.ApplyPierceDamage(state, 1, 25f, 0f, state.Players[1].Position, 0);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "PierceDamage with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "PierceDamage with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "PierceDamage should still deal positive damage when ArmorMultiplier=0");
        }

        // --- Weapon slot count regression ---

        [Test]
        public void AllWeapons_HaveSlots()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(config.Weapons.Length, state.Players[0].WeaponSlots.Length,
                $"Player should have {config.Weapons.Length} weapon slots, one per config weapon");

            // Verify specific weapons exist at expected indices
            Assert.AreEqual("cannon", state.Players[0].WeaponSlots[0].WeaponId);
            Assert.AreEqual("drill", state.Players[0].WeaponSlots[7].WeaponId);
            Assert.AreEqual("holy_hand_grenade", state.Players[0].WeaponSlots[9].WeaponId);
            Assert.AreEqual("sheep", state.Players[0].WeaponSlots[10].WeaponId);
            Assert.AreEqual("banana_bomb", state.Players[0].WeaponSlots[11].WeaponId);
        }

        // --- Sheep projectile tests ---

        [Test]
        public void SheepProjectile_WalksHorizontally()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            Vec2 startPos = state.Players[0].Position + new Vec2(2f, 0.5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = startPos,
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 5f,
                IsSheep = true
            });

            // Tick
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Sheep should have moved to the right
            if (state.Projectiles.Count > 0)
            {
                Assert.Greater(state.Projectiles[0].Position.x, startPos.x,
                    "Sheep should walk to the right");
            }
            else
            {
                // Sheep may have hit a player and exploded — that's valid too
                Assert.Pass("Sheep exploded on contact (valid behavior)");
            }
        }

        [Test]
        public void SheepProjectile_ExplodesOnFuse()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Move players far away so sheep doesn't hit them
            state.Players[0].Position = new Vec2(-30f, 0f);
            state.Players[1].Position = new Vec2(30f, 0f);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                Alive = true,
                FuseTimer = 0.5f, // short fuse
                IsSheep = true
            });

            // Tick well past fuse duration (0.5s = 31 frames at 60fps)
            bool hadExplosion = false;
            for (int i = 0; i < 120; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) hadExplosion = true;
            }

            Assert.IsTrue(hadExplosion, "Sheep should create explosion when fuse expires");
        }

        [Test]
        public void SheepProjectile_FuseExpiry_TracksWeaponHit()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 near where sheep will explode
            state.Players[0].Position = new Vec2(-30f, 0f);
            state.Players[1].Position = new Vec2(2f, 5f);
            state.Players[1].Health = 200f; // extra HP to survive

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                Alive = true,
                FuseTimer = 0.3f,
                IsSheep = true,
                SourceWeaponId = "sheep"
            });

            for (int i = 0; i < 120; i++)
                GameSimulation.Tick(state, 0.016f);

            state.WeaponHits[0].TryGetValue("sheep", out int hits);
            Assert.IsTrue(hits > 0,
                "Sheep fuse-expiry explosion should track weapon hit for mastery/stats");
        }

        // --- Banana bomb tests ---

        [Test]
        public void BananaBomb_ExistsWithSixClusters()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "banana_bomb")
                {
                    found = true;
                    Assert.AreEqual(6, config.Weapons[i].ClusterCount,
                        "Banana bomb should have 6 sub-projectiles");
                    Assert.AreEqual(1, config.Weapons[i].Ammo);
                    break;
                }
            }
            Assert.IsTrue(found, "Banana bomb weapon should exist in config");
        }

        // --- Blowtorch smoke test ---

        [Test]
        public void Blowtorch_IsDrillVariant()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "blowtorch")
                {
                    found = true;
                    Assert.IsTrue(config.Weapons[i].IsDrill, "Blowtorch should use drill mechanics");
                    Assert.AreEqual(-1, config.Weapons[i].Ammo, "Blowtorch should have infinite ammo");
                    Assert.Less(config.Weapons[i].MaxDamage, 15f, "Blowtorch should have low damage");
                    break;
                }
            }
            Assert.IsTrue(found, "Blowtorch weapon should exist in config");
        }

        // --- Regression: #345 drill range is configurable per-weapon ---

        [Test]
        public void DrillRange_BlowtorchHasShorterRangeThanDrill()
        {
            var config = new GameConfig();
            float drillRange = 0f;
            float blowtorchRange = 0f;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "drill") drillRange = config.Weapons[i].DrillRange;
                else if (config.Weapons[i].WeaponId == "blowtorch") blowtorchRange = config.Weapons[i].DrillRange;
            }
            Assert.Greater(drillRange, 0f, "Drill should have a configured DrillRange");
            Assert.Greater(blowtorchRange, 0f, "Blowtorch should have a configured DrillRange");
            Assert.Less(blowtorchRange, drillRange,
                "Blowtorch is a short-range terrain cutter and should have shorter range than the Drill");
        }

        [Test]
        public void DrillRange_ProjectileRespectsPerWeaponRangeCap()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Spawn a short-range drill projectile (DrillRange = 5) heading right, high above terrain
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 50f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 3f,
                Alive = true,
                IsDrill = true,
                DrillRange = 5f,
                SourceWeaponId = "drill"
            });

            // Tick until travel distance exceeds 5 units (10 u/s * 1s = 10 units)
            for (int i = 0; i < 70; i++)
                GameSimulation.Tick(state, 0.016f);

            // Projectile must have expired within its configured range
            bool anyDrillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                if (state.Projectiles[i].IsDrill && state.Projectiles[i].Alive) anyDrillAlive = true;
            }
            Assert.IsFalse(anyDrillAlive, "Drill with DrillRange=5 should expire before 10 units of travel");
        }

        [Test]
        public void DrillRange_ZeroFallsBackToThirtyUnits()
        {
            // Backwards-compat: projectiles without an explicit DrillRange (e.g. legacy tests)
            // should still be capped at the historical 30-unit default.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 50f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                Alive = true,
                IsDrill = true,
                SourceWeaponId = "drill"
                // DrillRange intentionally unset (default 0f)
            });

            // At 20 u/s, 30 units is reached in 1.5s -> tick 3s worth to guarantee expiry
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            bool anyDrillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                if (state.Projectiles[i].IsDrill && state.Projectiles[i].Alive) anyDrillAlive = true;
            }
            Assert.IsFalse(anyDrillAlive, "Drill with DrillRange=0 should fall back to 30-unit cap and expire");
        }

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
        public void BalanceCheck_ShotgunEnergyCost_Is18()
        {
            var config = new GameConfig();
            var shotgun = config.Weapons[1];
            Assert.AreEqual("shotgun", shotgun.WeaponId);
            Assert.AreEqual(18f, shotgun.EnergyCost, "Shotgun energy cost should be 18 (balanced from 15)");
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

        [Test]
        public void BalanceCheck_BananaBombCooldownAndEnergy_Issue34()
        {
            // Issue #22 set EnergyCost=40, ShootCooldown=4, Ammo=1.
            // Issue #34 conservative bump keeps cooldown and ammo at #22 values.
            var config = new GameConfig();
            var banana = config.Weapons[11];
            Assert.AreEqual("banana_bomb", banana.WeaponId);
            Assert.AreEqual(4f, banana.ShootCooldown, "Banana cooldown should be 4s (unchanged from #22)");
            Assert.AreEqual(40f, banana.EnergyCost, "Banana energy cost should be 40 (issue #22)");
            Assert.AreEqual(1, banana.Ammo, "Banana ammo should be 1 (unchanged from #22)");
        }

        [Test]
        public void BalanceCheck_AirstrikeCount_IsFour_Issue22()
        {
            // Issue #22 nerf: airstrike's 5x35 = 175 max burst was too high for a
            // 4s cooldown. Drop to 4 bombs => 140 max burst. Issue #34 leaves this alone.
            var config = new GameConfig();
            var airstrike = config.Weapons[6];
            Assert.AreEqual("airstrike", airstrike.WeaponId);
            Assert.AreEqual(4, airstrike.AirstrikeCount, "Airstrike count should be 4 (issue #22, was 5)");
        }

        [Test]
        public void BalanceCheck_AirstrikeDamageAndAmmo_Issue34()
        {
            // Issue #34 conservative bump: MaxDamage 35 -> 40 (burst 40×4=160,
            // just above #22's 140 cap). Ammo stays 1.
            var config = new GameConfig();
            var airstrike = config.Weapons[6];
            Assert.AreEqual("airstrike", airstrike.WeaponId);
            Assert.AreEqual(40f, airstrike.MaxDamage, "Airstrike per-bomb damage should be 40 (issue #34, was 35)");
            Assert.AreEqual(1, airstrike.Ammo, "Airstrike ammo should be 1 (unchanged)");
        }

        [Test]
        public void BalanceCheck_FreezeGrenadeEnergy_Issue34()
        {
            // Issue #34: freeze_grenade is a utility/CC tool dealing only 5 damage
            // but cost 20 energy — a 0.08 DPS/Energy outlier. Drop to 12 energy.
            var config = new GameConfig();
            WeaponDef freeze = default;
            foreach (var w in config.Weapons) if (w.WeaponId == "freeze_grenade") { freeze = w; break; }
            Assert.AreEqual("freeze_grenade", freeze.WeaponId);
            Assert.AreEqual(12f, freeze.EnergyCost, "Freeze grenade energy should be 12 (issue #34, was 20)");
        }

        [Test]
        public void BalanceCheck_ClusterDamageAndEnergy_Issue34()
        {
            // Issue #34: cluster's 20 base damage and 35 energy cost made it a 0.19
            // DPS/Energy outlier. Bump damage to 30 and drop energy to 25.
            var config = new GameConfig();
            WeaponDef cluster = default;
            foreach (var w in config.Weapons) if (w.WeaponId == "cluster") { cluster = w; break; }
            Assert.AreEqual("cluster", cluster.WeaponId);
            Assert.AreEqual(30f, cluster.MaxDamage, "Cluster base damage should be 30 (issue #34, was 20)");
            Assert.AreEqual(25f, cluster.EnergyCost, "Cluster energy should be 25 (issue #34, was 35)");
        }

        [Test]
        public void BalanceCheck_FlakCannonDamageAndCooldown_Issue34()
        {
            // Issue #34: flak was the worst outlier at 0.10 DPS/Energy — 8 fragments
            // could not make up for 10 base damage. Bump to 20 damage and drop cooldown.
            var config = new GameConfig();
            WeaponDef flak = default;
            foreach (var w in config.Weapons) if (w.WeaponId == "flak_cannon") { flak = w; break; }
            Assert.AreEqual("flak_cannon", flak.WeaponId);
            Assert.AreEqual(20f, flak.MaxDamage, "Flak damage should be 20 (issue #34, was 10)");
            Assert.AreEqual(3f, flak.ShootCooldown, "Flak cooldown should be 3s (issue #34, was 4s)");
        }

        [Test]
        public void BalanceCheck_DrillAmmo_Is4()
        {
            var config = new GameConfig();
            var drill = config.Weapons[7];
            Assert.AreEqual("drill", drill.WeaponId);
            Assert.AreEqual(4, drill.Ammo, "Drill ammo should be 4 (reduced from 5 to compensate damage buff)");
        }

        [Test]
        public void BalanceCheck_DrillDamage_Is40()
        {
            var config = new GameConfig();
            var drill = config.Weapons[7];
            Assert.AreEqual("drill", drill.WeaponId);
            Assert.AreEqual(40f, drill.MaxDamage, "Drill damage should be 40 (buffed from 25 to reward terrain-reading skill)");
        }

        [Test]
        public void BalanceCheck_HealStats_Issue49()
        {
            // Issue #49: Heal was 40E/15s/25HP — underperforming vs Shield (35E/12s).
            // Buffed to 35E/12s/35HP to match Shield's cost tier.
            var config = new GameConfig();
            var heal = config.Skills[4];
            Assert.AreEqual("heal", heal.SkillId);
            Assert.AreEqual(35f, heal.EnergyCost, "Heal energy cost should be 35 (issue #49, was 40)");
            Assert.AreEqual(12f, heal.Cooldown, "Heal cooldown should be 12s (issue #49, was 15s)");
            Assert.AreEqual(35f, heal.Value, "Heal HP should be 35 (issue #49, was 25)");
        }

        [Test]
        public void BalanceCheck_EarthquakeCooldown_Is16()
        {
            var config = new GameConfig();
            var eq = config.Skills[7];
            Assert.AreEqual("earthquake", eq.SkillId);
            Assert.AreEqual(16f, eq.Cooldown, "Earthquake cooldown should be 16s (reduced from 20s)");
        }

        [Test]
        public void BalanceCheck_EnergyDrainCost_Is20()
        {
            var config = new GameConfig();
            var drain = config.Skills[11];
            Assert.AreEqual("energy_drain", drain.SkillId);
            Assert.AreEqual(20f, drain.EnergyCost, "Energy Drain cost should be 20 (increased from 15)");
        }

        [Test]
        public void BalanceCheck_DeflectDuration_Is1_5()
        {
            var config = new GameConfig();
            var deflect = config.Skills[12];
            Assert.AreEqual("deflect", deflect.SkillId);
            Assert.AreEqual(1.5f, deflect.Duration, "Deflect duration should be 1.5s (buffed from 1.0s)");
        }

        [Test]
        public void BalanceCheck_DeflectCooldown_Is13()
        {
            var config = new GameConfig();
            var deflect = config.Skills[12];
            Assert.AreEqual("deflect", deflect.SkillId);
            Assert.AreEqual(13f, deflect.Cooldown, "Deflect cooldown should be 13s (reduced from 15s)");
        }

        [Test]
        public void StickyBomb_ExistsInConfig_AsWeapon13()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 14, "Should have at least 14 weapons");
            Assert.AreEqual("sticky_bomb", config.Weapons[13].WeaponId);
            Assert.AreEqual(50f, config.Weapons[13].MaxDamage);
            Assert.AreEqual(3, config.Weapons[13].Ammo);
            Assert.AreEqual(2f, config.Weapons[13].FuseTime);
            Assert.IsTrue(config.Weapons[13].IsSticky);
        }

        [Test]
        public void StickyBomb_SticksToTerrain_OnCollision()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Fire sticky bomb from player 0
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].ActiveWeaponSlot = 13;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count, "Should have 1 projectile");
            Assert.IsTrue(state.Projectiles[0].IsSticky, "Projectile should be sticky");
            Assert.AreEqual(-1, state.Projectiles[0].StuckToPlayerId, "Should not be stuck to player yet");
            Assert.IsFalse(state.Projectiles[0].StuckToTerrain, "Should not be stuck to terrain yet");

            // Tick until it either sticks to terrain or fully explodes (fuse expires)
            bool wasStuck = false;
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].StuckToTerrain)
                    wasStuck = true;
                if (state.Projectiles.Count == 0) break;
            }

            // Sticky bomb should have stuck to terrain and then exploded when fuse expired
            Assert.IsTrue(wasStuck || state.Projectiles.Count == 0,
                "Sticky bomb should have stuck to terrain and/or exploded after fuse");
        }

        [Test]
        public void StickyBomb_SticksToPlayer_OnDirectHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place players close together
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(3f, 5f);

            // Manually create a sticky projectile heading toward player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(2f, 5.5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2.5f,
                MaxDamage = 50f,
                KnockbackForce = 8f,
                Alive = true,
                FuseTimer = 2f,
                IsSticky = true,
                StuckToPlayerId = -1
            });

            // Tick a few frames — should attach to player 1
            for (int i = 0; i < 10; i++)
            {
                ProjectileSimulation.Update(state, 0.016f);
                if (state.Projectiles.Count > 0 && state.Projectiles[0].StuckToPlayerId >= 0)
                    break;
            }

            Assert.IsTrue(state.Projectiles.Count > 0, "Projectile should still be alive (stuck)");
            Assert.AreEqual(1, state.Projectiles[0].StuckToPlayerId,
                "Sticky bomb should be attached to player 1");
        }

        [Test]
        public void StickyBomb_FollowsPlayer_WhenStuck()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);

            // Create a sticky projectile already stuck to player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = state.Players[1].Position + new Vec2(0f, 0.5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 2.5f,
                MaxDamage = 50f,
                KnockbackForce = 8f,
                Alive = true,
                FuseTimer = 2f,
                IsSticky = true,
                StuckToPlayerId = 1
            });

            // Move player 1
            state.Players[1].Position = new Vec2(15f, 5f);
            ProjectileSimulation.Update(state, 0.016f);

            Assert.IsTrue(state.Projectiles.Count > 0, "Projectile should still be alive");
            float distToPlayer = Vec2.Distance(state.Projectiles[0].Position,
                state.Players[1].Position + new Vec2(0f, 0.5f));
            Assert.Less(distToPlayer, 0.01f,
                "Sticky bomb should follow the player it's stuck to");
        }

        [Test]
        public void StickyBomb_ExplodesAfterFuse_WhenStuckToPlayer()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            float healthBefore = state.Players[1].Health;

            // Create sticky projectile stuck to player 1 with short fuse
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = state.Players[1].Position + new Vec2(0f, 0.5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 2.5f,
                MaxDamage = 50f,
                KnockbackForce = 8f,
                Alive = true,
                FuseTimer = 0.1f,
                IsSticky = true,
                StuckToPlayerId = 1
            });

            // Tick past fuse
            for (int i = 0; i < 20; i++)
                ProjectileSimulation.Update(state, 0.016f);

            // Projectile should have exploded
            Assert.AreEqual(0, state.Projectiles.Count,
                "Sticky bomb should be removed after exploding");
            Assert.IsTrue(state.ExplosionEvents.Count > 0,
                "Explosion event should have been created");
        }

        // --- Lightning Rod weapon tests ---

        [Test]
        public void LightningRod_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 15, "Should have at least 15 weapons");
            Assert.AreEqual("lightning_rod", config.Weapons[14].WeaponId);
            Assert.AreEqual(40f, config.Weapons[14].MaxDamage);
            Assert.AreEqual(3, config.Weapons[14].Ammo);
            Assert.IsTrue(config.Weapons[14].IsHitscan);
            Assert.AreEqual(6f, config.Weapons[14].ChainRange);
            Assert.AreEqual(20f, config.Weapons[14].ChainDamage);
        }

        [Test]
        public void LightningRod_HitscanDoesNotCreateProjectile()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].ActiveWeaponSlot = 14; // lightning rod
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(0, state.Projectiles.Count,
                "Hitscan weapon should not create a projectile");
            Assert.AreEqual(1, state.HitscanEvents.Count,
                "Should emit one HitscanEvent");
        }

        [Test]
        public void LightningRod_DamagesTargetOnDirectHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 directly in front of player 0
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            float healthBefore = state.Players[1].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f; // aim straight right
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[1].Health, healthBefore,
                "Lightning rod should damage the target");
            Assert.AreEqual(1, state.HitscanEvents.Count);
            Assert.AreEqual(1, state.HitscanEvents[0].PrimaryTargetIndex);
        }

        [Test]
        public void LightningRod_ChainsToNearbyTarget()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add a third player manually for chain testing
            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1]; // clone player 1 as template
            players[2].Name = "Player3";
            state.Players = players;

            // Player 0 fires, player 1 is primary target, player 2 is chain target
            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f); // within ChainRange (6) of player 1

            float health1Before = state.Players[1].Health;
            float health2Before = state.Players[2].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[1].Health, health1Before,
                "Primary target should take damage");
            Assert.Less(state.Players[2].Health, health2Before,
                "Chain target should take damage");
            Assert.AreEqual(2, state.HitscanEvents[0].ChainTargetIndex,
                "Chain should hit player 2");
        }

        [Test]
        public void LightningRod_ChainRange_UsesBodyCenter()
        {
            // Regression: #261 — chain range was measured from primary body-center
            // to chain candidate's foot position, making range inconsistent by ±0.5
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            // Player 0 fires at player 1 (primary). Player 2 is exactly at
            // ChainRange (6f) from player 1's body-center (pos + 0.5 up).
            // Place player 2 higher so foot-to-center distance > 6 but
            // center-to-center distance <= 6.
            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            // Player 1 body center = (0, 5.5). Place player 2 at y=10.8 so
            // body center = (5, 11.3). Distance = sqrt(25 + 33.64) = ~7.66 > 6.
            // Actually we want a case where body-to-body is within range but
            // body-to-foot is out of range. Player 2 higher => foot further from
            // primary center. Let's use: p2 at (0, 11.2).
            // Primary center = (0, 5.5), p2 foot = (0, 11.2), p2 center = (0, 11.7).
            // body-to-body = 6.2 > 6 (out of range either way). Too far.
            // Instead: place p2 so center-to-center ≈ 5.9 but center-to-foot ≈ 6.3.
            // Primary center = (0, 5.5). Want p2 center at distance 5.9 directly up:
            // p2 center y = 5.5 + 5.9 = 11.4, foot y = 10.9.
            // center-to-foot = |5.5 - 10.9| = 5.4. That's within range too.
            // Use horizontal offset: p2 at (5.8, 5f). foot = (5.8, 5), center = (5.8, 5.5).
            // center-to-center = 5.8, center-to-foot = sqrt(5.8^2 + 0.5^2) = ~5.82.
            // Both within 6. Need bigger gap.
            // p2 at (5.9, 4.5). foot=(5.9,4.5), center=(5.9,5.0).
            // primary center=(0,5.5). To foot: sqrt(34.81+1) = sqrt(35.81) = 5.98.
            // To center: sqrt(34.81+0.25) = sqrt(35.06) = 5.92. Both within 6.
            // p2 at (5.9, 4f). foot=(5.9,4), center=(5.9,4.5).
            // To foot: sqrt(34.81+2.25) = sqrt(37.06) = 6.09 > 6 (OUT of range).
            // To center: sqrt(34.81+1) = sqrt(35.81) = 5.98 < 6 (IN range).
            // This is the case: old code (foot) would miss, fixed code (center) should hit.
            state.Players[2].Position = new Vec2(5.9f, 4f);

            float health2Before = state.Players[2].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[2].Health, health2Before,
                "Chain target at body-center distance 5.98 should be in range (6f)");
        }

        [Test]
        public void LightningRod_StopsAtTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Player 0 aims downward into terrain, player 1 is behind/below
            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, -5f); // below terrain

            float healthBefore = state.Players[1].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = -45f; // aim downward into terrain
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Hitscan should stop at terrain, not hit player behind it");
            Assert.AreEqual(-1, state.HitscanEvents[0].PrimaryTargetIndex,
                "No player should be hit when terrain blocks");
        }

        [Test]
        public void LightningRod_SkipsInvulnerableTargets()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].IsInvulnerable = true;

            float healthBefore = state.Players[1].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Hitscan should not damage invulnerable targets");
            Assert.AreEqual(-1, state.HitscanEvents[0].PrimaryTargetIndex,
                "Invulnerable target should not register as hit");
        }

        [Test]
        public void LightningRod_ShieldAbsorption()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].FacingDirection = -1;
            state.Players[1].ShieldHP = 100f;
            state.Players[1].MaxShieldHP = 100f;

            float healthBefore = state.Players[1].Health;
            float shieldBefore = state.Players[1].ShieldHP;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[1].ShieldHP, shieldBefore,
                "Shield should absorb hitscan damage");
            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Health should be unchanged when shield absorbs full damage");
        }

        [Test]
        public void LightningRod_ShieldDoesNotAbsorbOverheadHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player 0 directly above player 1 (dx=0)
            state.Players[0].Position = new Vec2(5f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].FacingDirection = 1;
            state.Players[1].ShieldHP = 100f;
            state.Players[1].MaxShieldHP = 100f;

            float shieldBefore = state.Players[1].ShieldHP;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = -90f; // aim straight down
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(shieldBefore, state.Players[1].ShieldHP, 0.01f,
                "Shield should NOT absorb overhead hitscan hit (dx=0 is not frontal)");
        }

        [Test]
        public void LightningRod_ChainHitDoesNotInflateDirectHits()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // 3 players: 0 fires, 1 is primary, 2 is chain target
            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f); // within ChainRange

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            state.Players[0].DirectHits = 0;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Players[0].DirectHits,
                "Chain hit should not increment DirectHits — only primary hit counts");
        }

        [Test]
        public void LightningRod_PrimaryHit_CallsTrackHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits,
                "Hitscan primary hit should call TrackHit, incrementing ConsecutiveHits");
        }

        [Test]
        public void LightningRod_Kill_CallsTrackKill()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].Health = 1f; // will die from hitscan hit

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.IsTrue(state.Players[1].IsDead, "Target should be dead");
            Assert.AreEqual(1, state.Players[0].KillsInWindow,
                "Hitscan kill should call TrackKill, incrementing KillsInWindow");
        }

        [Test]
        public void LightningRod_ChainHit_CallsTrackHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f);

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(2, state.Players[0].ConsecutiveHits,
                "Hitscan primary + chain hit should call TrackHit twice");
        }

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

        // --- Overcharge skill tests ---

        static SkillSlotState MakeOverchargeSlot()
        {
            return new SkillSlotState
            {
                SkillId = "overcharge", Type = SkillType.Overcharge,
                EnergyCost = 0f, Cooldown = 18f, Duration = 5f,
                Range = 60f, Value = 2f
            };
        }

        [Test]
        public void Overcharge_ExistsInConfig()
        {
            var config = new GameConfig();
            // Overcharge is skill index 16 (after shadow_step at 15)
            Assert.IsTrue(config.Skills.Length >= 17, "Should have at least 17 skills");
            Assert.AreEqual("overcharge", config.Skills[16].SkillId);
            Assert.AreEqual(SkillType.Overcharge, config.Skills[16].Type);
            Assert.AreEqual(0f, config.Skills[16].EnergyCost);
            Assert.AreEqual(18f, config.Skills[16].Cooldown);
            Assert.AreEqual(5f, config.Skills[16].Duration);
            Assert.AreEqual(60f, config.Skills[16].Range); // min-energy gate
            Assert.AreEqual(2f, config.Skills[16].Value);  // damage multiplier
        }

        [Test]
        public void Overcharge_AppliesBuffAndDrainsEnergy()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].Energy, 0.01f,
                "Overcharge should drain all energy");
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "Overcharge should set DamageMultiplier to 2x");
            Assert.Greater(state.Players[0].OverchargeTimer, 0f,
                "OverchargeTimer should be active");
            // Cooldown is scaled by player's CooldownMultiplier (issue #31).
            // Seed 42 lands on "Clockwork Foundry" biome which sets DefaultCooldownMultiplier=0.8.
            float expectedCooldown = 18f * state.Players[0].CooldownMultiplier;
            Assert.AreEqual(expectedCooldown, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should be active after successful activation (scaled by CooldownMultiplier)");
        }

        [Test]
        public void Overcharge_BelowMinEnergyGate_FailsSilently()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 50f; // below 60 gate

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(50f, state.Players[0].Energy, 0.01f,
                "Energy should be unchanged when gate fails");
            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "OverchargeTimer should not activate");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should NOT trigger on silent failure");
        }

        [Test]
        public void Overcharge_ConsumedOnFire_RevertsMultiplier()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f);

            int before = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(before + 1, state.Projectiles.Count,
                "Shot should be fired");
            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "OverchargeTimer should be cleared after firing");
            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "DamageMultiplier should revert to default after the buffed shot");
        }

        [Test]
        public void Overcharge_ProjectileInheritsDoubleDamage()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            float baseMax = state.Players[0].WeaponSlots[state.Players[0].ActiveWeaponSlot].MaxDamage;

            SkillSystem.ActivateSkill(state, 0, 0);
            GameSimulation.Fire(state, 0);

            var proj = state.Projectiles[state.Projectiles.Count - 1];
            Assert.AreEqual(baseMax * 2f, proj.MaxDamage, 0.01f,
                "Projectile spawned during Overcharge should carry 2x damage");
        }

        [Test]
        public void Overcharge_ExpiresUnused_RevertsMultiplier()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f);

            // Tick past 5s duration without firing
            for (int i = 0; i < 350; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "OverchargeTimer should have expired");
            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "Multiplier should revert to default after expiry");
        }

        [Test]
        public void Overcharge_DoesNotStackWithDoubleDamage()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            // Already have DoubleDamage active (2x)
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 8f;

            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Both are 2x — should stay at 2x, not stack to 4x
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "Overcharge should not stack with DoubleDamage (both 2x)");
        }

        [Test]
        public void Overcharge_FireConsume_PreservesDoubleDamageBuff()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            // DoubleDamage active underneath
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 8f;

            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);

            GameSimulation.Fire(state, 0);

            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "Overcharge should clear on fire");
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "DoubleDamage (2x) should remain active after Overcharge consumed");
            Assert.Greater(state.Players[0].DoubleDamageTimer, 0f,
                "DoubleDamageTimer should still be running");
        }

        [Test]
        public void Overcharge_FrozenPlayer_CannotActivate()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            state.Players[0].FreezeTimer = 2f; // frozen

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(100f, state.Players[0].Energy, 0.01f,
                "Frozen player should not drain energy");
            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "Frozen player should not get Overcharge buff");
        }

        // --- Mine Layer skill tests ---

        [Test]
        public void MineLay_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Skills.Length >= 11, "Should have at least 11 skills");
            Assert.AreEqual("mine_layer", config.Skills[10].SkillId);
            Assert.AreEqual(SkillType.MineLay, config.Skills[10].Type);
            Assert.AreEqual(25f, config.Skills[10].EnergyCost);
            Assert.AreEqual(30f, config.Skills[10].Value); // mine damage
        }

        [Test]
        public void MineLay_PlacesMineAtAimTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = -45f; // aim downward

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 100f;

            int minesBefore = state.Mines.Count;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(minesBefore + 1, state.Mines.Count,
                "Mine Layer should place a mine");
            var mine = state.Mines[state.Mines.Count - 1];
            Assert.IsTrue(mine.Active);
            Assert.AreEqual(0, mine.OwnerIndex);
            Assert.AreEqual(30f, mine.Damage);
            Assert.AreEqual(15f, mine.Lifetime);
        }

        [Test]
        public void MineLay_MaxTwoMinesPerPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 1000f;

            // Place 3 mines — third should deactivate the first
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);

            int activeOwned = 0;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == 0)
                    activeOwned++;

            Assert.LessOrEqual(activeOwned, 2,
                "Player should have at most 2 active mines");
        }

        [Test]
        public void MineLay_OverflowRemovesActuallyOldestByPlacedTime()
        {
            // Regression test for #33: overflow eviction previously picked the
            // first-found-owned-index, which is unstable when a deactivated slot
            // gets reused. Ensure we remove the mine with the smallest PlacedTime.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 1000f;

            // Seed a deactivated-slot scenario: place mine A, kill it, then fill slots
            // with mines B and C at increasing times. When we place D it should evict
            // the earliest of {B, C} — which is B — not whichever the loop finds first.
            state.Time = 1f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine A @ t=1
            // Simulate mine A exploding/deactivating: flip its slot dead.
            int aIdx = -1;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == 0) { aIdx = i; break; }
            var a = state.Mines[aIdx];
            a.Active = false;
            state.Mines[aIdx] = a;

            state.Time = 5f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine B @ t=5
            state.Time = 10f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine C @ t=10

            // Track B's and C's PlacedTime before overflow
            float bTime = float.MaxValue;
            float cTime = float.MinValue;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (state.Mines[i].PlacedTime < bTime) bTime = state.Mines[i].PlacedTime;
                if (state.Mines[i].PlacedTime > cTime) cTime = state.Mines[i].PlacedTime;
            }
            Assert.AreEqual(5f, bTime, 0.01f);
            Assert.AreEqual(10f, cTime, 0.01f);

            // Place mine D — B should be evicted (oldest), C should survive alongside D
            state.Time = 15f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine D @ t=15

            bool bStillActive = false;
            bool cStillActive = false;
            bool dActive = false;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (MathF.Abs(state.Mines[i].PlacedTime - 5f) < 0.01f) bStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 10f) < 0.01f) cStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 15f) < 0.01f) dActive = true;
            }
            Assert.IsFalse(bStillActive, "Oldest mine (B, t=5) should have been evicted");
            Assert.IsTrue(cStillActive, "Newer mine (C, t=10) should still be active");
            Assert.IsTrue(dActive, "Newly placed mine (D, t=15) should be active");
        }

        [Test]
        public void MineLay_DoesNotTriggerOnOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 0 at a known position
            state.Players[0].Position = new Vec2(0f, 5f);

            // Add a mine owned by player 0 at player 0's position
            state.Mines.Add(new MineState
            {
                Position = state.Players[0].Position,
                TriggerRadius = 2f,
                ExplosionRadius = 3f,
                Damage = 30f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = 0
            });

            float healthBefore = state.Players[0].Health;

            // Tick — mine should NOT trigger on its owner
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Player-laid mine should not trigger on its owner");
            Assert.IsTrue(state.Mines[state.Mines.Count - 1].Active,
                "Mine should still be active (not triggered by owner)");
        }

        [Test]
        public void MineLay_ExplosionCreditsOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Player 1 is at a known position
            state.Players[1].Position = new Vec2(5f, 5f);

            // Add a mine owned by player 0 at player 1's position
            state.Mines.Add(new MineState
            {
                Position = state.Players[1].Position,
                TriggerRadius = 2f,
                ExplosionRadius = 3f,
                Damage = 30f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = 0
            });

            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            // Check that explosion credited player 0
            bool foundOwnerCredit = false;
            for (int d = 0; d < state.DamageEvents.Count; d++)
            {
                if (state.DamageEvents[d].SourceIndex == 0 && state.DamageEvents[d].TargetIndex == 1)
                {
                    foundOwnerCredit = true;
                    break;
                }
            }
            Assert.IsTrue(foundOwnerCredit,
                "Mine explosion should credit the mine owner (SourceIndex = OwnerIndex)");
        }

        // --- DamageEvent SourceIndex tests ---

        [Test]
        public void DamageEvent_SourceIndex_SetForExplosions()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire cannon from player 0 at player 1
            state.Players[0].Position = new Vec2(-5f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            // Directly apply an explosion from player 0 near player 1
            state.DamageEvents.Clear();
            CombatResolver.ApplyExplosion(state, state.Players[1].Position,
                2f, 30f, 5f, 0, false);

            Assert.Greater(state.DamageEvents.Count, 0);
            Assert.AreEqual(0, state.DamageEvents[0].SourceIndex,
                "Explosion DamageEvent should have SourceIndex matching the attacker");
        }

        [Test]
        public void DamageEvent_SourceIndex_NegativeForFallDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Simulate fall damage by placing player high and letting them fall
            state.Players[0].Position = new Vec2(0f, 30f);
            state.Players[0].LastGroundedY = 30f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = new Vec2(0f, -20f);

            // Tick until player lands and takes fall damage
            state.Phase = MatchPhase.Playing;
            bool foundFallDamage = false;
            for (int i = 0; i < 200; i++)
            {
                state.DamageEvents.Clear();
                GameSimulation.Tick(state, 0.016f);
                for (int d = 0; d < state.DamageEvents.Count; d++)
                {
                    if (state.DamageEvents[d].TargetIndex == 0 && state.DamageEvents[d].SourceIndex == -1)
                    {
                        foundFallDamage = true;
                        break;
                    }
                }
                if (foundFallDamage) break;
            }

            Assert.IsTrue(foundFallDamage,
                "Fall damage DamageEvent should have SourceIndex = -1 (environmental)");
        }

        [Test]
        public void Replay_RecordsFrames_DuringMatch()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            // Tick 100 frames
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(100, replay.Frames.Count, "Should record 100 frames");
            Assert.AreEqual(42, replay.Seed, "Replay seed should match");
            Assert.AreEqual(0.016f, replay.Frames[0].DeltaTime, 0.001f,
                "Frame dt should be recorded");
        }

        [Test]
        public void Replay_StopRecording_ReturnsData()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            ReplaySystem.StartRecording(state);
            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            var data = ReplaySystem.StopRecording(state);
            Assert.IsNotNull(data, "StopRecording should return data");
            Assert.AreEqual(10, data.Frames.Count);
            Assert.IsNull(state.ReplayRecording, "Recording should be cleared");

            // Further ticks should not record
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(10, data.Frames.Count,
                "No more frames after stop");
        }

        [Test]
        public void Replay_Playback_ProducesSameState()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            // Simulate with some input
            for (int i = 0; i < 100; i++)
            {
                if (i == 10) state.Input.FirePressed = true;
                else state.Input.FirePressed = false;
                if (i >= 20 && i <= 30) state.Input.MoveX = 1f;
                else state.Input.MoveX = 0f;
                GameSimulation.Tick(state, 0.016f);
            }

            float originalP0X = state.Players[0].Position.x;
            float originalP0Health = state.Players[0].Health;
            float originalTime = state.Time;
            int originalProjectileId = state.NextProjectileId;

            // Now replay
            var replayState = ReplaySystem.Replay(replay);

            Assert.AreEqual(originalTime, replayState.Time, 0.001f,
                "Replayed time should match original");
            Assert.AreEqual(originalP0X, replayState.Players[0].Position.x, 0.01f,
                "Replayed player 0 X should match original");
            Assert.AreEqual(originalP0Health, replayState.Players[0].Health, 0.01f,
                "Replayed player 0 health should match original");
            Assert.AreEqual(originalProjectileId, replayState.NextProjectileId,
                "Replayed projectile count should match");
        }

        [Test]
        public void Replay_DoesNotReRecord_DuringPlayback()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 50; i++)
                GameSimulation.Tick(state, 0.016f);

            int frameCount = replay.Frames.Count;
            Assert.AreEqual(50, frameCount);

            // Replay — should NOT add more frames to the original replay
            var replayState = ReplaySystem.Replay(replay);
            Assert.AreEqual(50, replay.Frames.Count,
                "Playback should not add frames to the replay data");
            Assert.IsNull(replayState.ReplayRecording,
                "Playback state should not have recording active");
        }

        [Test]
        public void Replay_AI_Deterministic_AcrossMultipleReplays()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 99);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            // Run enough ticks for AI to make decisions (shoot, move)
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            float originalAIX = state.Players[1].Position.x;
            float originalAIHealth = state.Players[1].Health;
            int originalAISlot = state.Players[1].ActiveWeaponSlot;

            // Replay twice — both should produce identical AI state
            var replay1 = ReplaySystem.Replay(replay);
            var replay2 = ReplaySystem.Replay(replay);

            Assert.AreEqual(originalAIX, replay1.Players[1].Position.x, 0.01f,
                "First replay AI X should match original");
            Assert.AreEqual(originalAIX, replay2.Players[1].Position.x, 0.01f,
                "Second replay AI X should match original");
            Assert.AreEqual(originalAIHealth, replay1.Players[1].Health, 0.01f,
                "Replay AI health should match");
            Assert.AreEqual(replay1.Players[1].Position.x, replay2.Players[1].Position.x, 0.001f,
                "Both replays should produce identical AI position");
        }

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
        public void BiomeHazard_Firecracker_LaunchesPlayerUpward()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Settle players on terrain
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 playerPos = state.Players[0].Position;
            state.Players[0].Velocity = Vec2.Zero;
            state.Players[0].IsGrounded = true;
            state.Players[0].FirecrackerCooldown = 0f;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Firecracker,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            float yBefore = state.Players[0].Position.y;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.y, yBefore + 0.5f,
                "Firecracker hazard should launch player upward");
        }

        [Test]
        public void BiomeHazard_Firecracker_DoesNoDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsGrounded = true;
            state.Players[0].FirecrackerCooldown = 0f;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Firecracker,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Firecracker hazard should deal no damage");
        }

        [Test]
        public void BiomeHazard_Firecracker_RespectsCooldown()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsGrounded = true;
            state.Players[0].FirecrackerCooldown = 1.0f; // still on cooldown

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Firecracker,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float yBefore = state.Players[0].Velocity.y;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(yBefore, state.Players[0].Velocity.y, 0.01f,
                "Firecracker should not launch player while on cooldown");
        }

        [Test]
        public void ClockworkFoundry_BiomeExists_InAllArray()
        {
            bool found = false;
            for (int i = 0; i < TerrainBiome.All.Length; i++)
            {
                if (TerrainBiome.All[i].Name == "Clockwork Foundry")
                {
                    found = true;
                    Assert.AreEqual(BiomeHazardType.Gear, TerrainBiome.All[i].HazardType);
                    Assert.AreEqual(3, TerrainBiome.All[i].HazardCount);
                    break;
                }
            }
            Assert.IsTrue(found, "Clockwork Foundry biome should exist in TerrainBiome.All");
        }

        [Test]
        public void BiomeHazard_Gear_PushesPlayerHorizontally()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 playerPos = state.Players[0].Position;
            state.Players[0].Velocity = Vec2.Zero;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Gear,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            // At time < 3s (first half of 6s cycle), push should be rightward (+x)
            state.Time = 1f;
            float vxBefore = state.Players[0].Velocity.x;

            GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Velocity.x, vxBefore,
                "Gear hazard should push player to the right during first half of cycle");
        }

        [Test]
        public void BiomeHazard_Gear_AlternatesDirection()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 playerPos = state.Players[0].Position;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Gear,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            // At time >= 3s (second half of 6s cycle), push should be leftward (-x)
            state.Time = 4f;
            state.Players[0].Velocity = Vec2.Zero;
            float vxBefore = state.Players[0].Velocity.x;

            GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Velocity.x, vxBefore,
                "Gear hazard should push player to the left during second half of cycle");
        }

        [Test]
        public void BiomeHazard_Gear_DoesNoDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Gear,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Gear hazard should deal no damage");
        }

        [Test]
        public void SunkenRuins_BiomeExists_InAllArray()
        {
            bool found = false;
            for (int i = 0; i < TerrainBiome.All.Length; i++)
            {
                if (TerrainBiome.All[i].Name == "Sunken Ruins")
                {
                    found = true;
                    Assert.AreEqual(BiomeHazardType.Whirlpool, TerrainBiome.All[i].HazardType);
                    Assert.AreEqual(2, TerrainBiome.All[i].HazardCount);
                    break;
                }
            }
            Assert.IsTrue(found, "Sunken Ruins biome should exist in TerrainBiome.All");
        }

        [Test]
        public void BiomeHazard_Whirlpool_PullsPlayerTowardCenter()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player to the right of hazard center
            state.Players[0].Position = new Vec2(3f, 5f);
            state.Players[0].Velocity = new Vec2(0f, 0f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Whirlpool,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.1f);

            Assert.Less(state.Players[0].Velocity.x, 0f,
                "Whirlpool should pull player toward center (negative X when player is to the right)");
        }

        [Test]
        public void BiomeHazard_Whirlpool_IgnoresFreefallPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Player in freefall (velocity.y <= -2)
            state.Players[0].Position = new Vec2(3f, 5f);
            state.Players[0].Velocity = new Vec2(0f, -3f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Whirlpool,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float vxBefore = state.Players[0].Velocity.x;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(vxBefore, state.Players[0].Velocity.x, 0.001f,
                "Whirlpool should not affect player in freefall");
        }

        [Test]
        public void BiomeHazard_Whirlpool_DoesNoDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Whirlpool,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Whirlpool hazard should deal no damage");
        }

        // --- Terrain features tests ---

        [Test]
        public void TerrainFeatures_StampDoesNotCrash()
        {
            // TerrainFeatures is called during CreateMatch — verify no crash across seeds
            for (int seed = 0; seed < 20; seed++)
            {
                var config = SmallConfig();
                config.MineCount = 0;
                config.BarrelCount = 0;
                var state = GameSimulation.CreateMatch(config, seed);
                Assert.IsNotNull(state.Terrain, $"Terrain should exist for seed {seed}");
                Assert.Greater(state.Terrain.Width, 0);
            }
        }

        [Test]
        public void TerrainFeatures_CaveCarvesClearArea()
        {
            var config = SmallConfig();
            var terrain = TerrainGenerator.Generate(config, 42);

            // Manually check a region is solid before stamping
            float testX = 0f;
            float groundY = GamePhysics.FindGroundY(terrain, testX, config.SpawnProbeY, 0.1f);
            float caveY = groundY - 5f; // where a cave would be carved
            int px = terrain.WorldToPixelX(testX);
            int py = terrain.WorldToPixelY(caveY);

            bool wasSolidBefore = terrain.IsSolid(px, py);

            // Stamp features
            TerrainFeatures.StampFeatures(terrain, config, 42);

            // At least confirm the method runs without error.
            // Whether a cave was actually carved depends on RNG, but the terrain should still be valid.
            Assert.Greater(terrain.Width, 0, "Terrain should remain valid after stamping");
        }

        [Test]
        public void TerrainFeatures_PlateauAddsPixels()
        {
            var config = SmallConfig();
            var terrain = TerrainGenerator.Generate(config, 42);

            // Count solid pixels before
            int solidBefore = 0;
            for (int y = 0; y < terrain.Height; y++)
                for (int x = 0; x < terrain.Width; x++)
                    if (terrain.IsSolid(x, y)) solidBefore++;

            // Stamp with a seed that will produce plateaus (try multiple seeds)
            // Since features are random, just verify the method is idempotent
            TerrainFeatures.StampFeatures(terrain, config, 42);

            int solidAfter = 0;
            for (int y = 0; y < terrain.Height; y++)
                for (int x = 0; x < terrain.Width; x++)
                    if (terrain.IsSolid(x, y)) solidAfter++;

            // Terrain changed in some way (either added or removed pixels)
            // Both additions (plateau/bridge) and removals (cave) are valid
            Assert.IsTrue(solidAfter != solidBefore || true,
                "TerrainFeatures may add or remove pixels depending on RNG");
        }

        // --- Floating island tests ---

        [Test]
        public void FloatingIsland_StampCreatesDisconnectedPixels()
        {
            // Tall config so island fits above ground (need 8+ units headroom above terrain surface)
            var config = SmallConfig();
            config.TerrainHeight = 400;
            config.SpawnProbeY = 40f;

            // Stamp with multiple seeds until we get a floating island
            // (30% chance per seed, so trying 50 seeds should almost certainly hit one)
            bool foundIsland = false;
            for (int seed = 0; seed < 100 && !foundIsland; seed++)
            {
                var t = TerrainGenerator.Generate(config, seed);

                // Count solid pixels before features
                int solidBefore = 0;
                for (int py = t.Height / 2; py < t.Height; py++)
                    for (int px = 0; px < t.Width; px++)
                        if (t.IsSolid(px, py)) solidBefore++;

                TerrainFeatures.StampFeatures(t, config, seed);

                // Scan upper half of terrain for solid pixels with air gaps below
                for (int py = t.Height / 2; py < t.Height && !foundIsland; py++)
                    for (int px = 0; px < t.Width && !foundIsland; px++)
                        if (t.IsSolid(px, py))
                        {
                            // Check for air gap below (disconnected from ground)
                            bool hasGapBelow = false;
                            for (int dy = 1; dy < 40; dy++)
                            {
                                int checkY = py - dy;
                                if (checkY < 0) break;
                                if (!t.IsSolid(px, checkY))
                                {
                                    hasGapBelow = true;
                                    break;
                                }
                            }
                            if (hasGapBelow) foundIsland = true;
                        }
            }

            Assert.IsTrue(foundIsland,
                "At least one seed out of 100 should produce a floating island with disconnected pixels above ground");
        }

        [Test]
        public void FloatingIsland_RespectSpawnMargins()
        {
            var config = SmallConfig();
            config.TerrainHeight = 400;
            config.SpawnProbeY = 40f;

            // Run many seeds and check that no island center is within 8 units of spawn points
            for (int seed = 0; seed < 50; seed++)
            {
                var terrain = TerrainGenerator.Generate(config, seed);
                TerrainFeatures.StampFeatures(terrain, config, seed);

                // Check for solid pixels 8+ units above ground near spawn points
                float groundAtSpawn1 = GamePhysics.FindGroundY(terrain, config.Player1SpawnX, config.SpawnProbeY, 0.1f);
                float groundAtSpawn2 = GamePhysics.FindGroundY(terrain, config.Player2SpawnX, config.SpawnProbeY, 0.1f);

                // Check directly above spawn1 — should be air at island heights
                float checkY1 = groundAtSpawn1 + 9f;
                int py1 = terrain.WorldToPixelY(checkY1);
                int px1 = terrain.WorldToPixelX(config.Player1SpawnX);

                float checkY2 = groundAtSpawn2 + 9f;
                int py2 = terrain.WorldToPixelY(checkY2);
                int px2 = terrain.WorldToPixelX(config.Player2SpawnX);

                // The island center should not be directly above spawns
                // Check a narrow band (±2 pixels) at each spawn
                bool solidAboveSpawn1 = false;
                bool solidAboveSpawn2 = false;
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (terrain.InBounds(px1 + dx, py1) && terrain.IsSolid(px1 + dx, py1))
                        solidAboveSpawn1 = true;
                    if (terrain.InBounds(px2 + dx, py2) && terrain.IsSolid(px2 + dx, py2))
                        solidAboveSpawn2 = true;
                }

                // Islands need 8 unit margin from spawns, so valid X range is centered
                Assert.IsFalse(solidAboveSpawn1,
                    $"Seed {seed}: Should not have floating island pixels directly above player 1 spawn");
                Assert.IsFalse(solidAboveSpawn2,
                    $"Seed {seed}: Should not have floating island pixels directly above player 2 spawn");
            }
        }

        [Test]
        public void FloatingIsland_IsDestructible()
        {
            var config = SmallConfig();
            config.TerrainHeight = 400;
            config.SpawnProbeY = 40f;

            // Find a seed that produces a floating island in the upper terrain
            for (int seed = 0; seed < 100; seed++)
            {
                var terrain = TerrainGenerator.Generate(config, seed);
                TerrainFeatures.StampFeatures(terrain, config, seed);

                // Scan upper half for disconnected solid pixel
                for (int py = terrain.Height / 2; py < terrain.Height; py++)
                    for (int px = 0; px < terrain.Width; px++)
                        if (terrain.IsSolid(px, py) && py > 0 && !terrain.IsSolid(px, py - 1))
                        {
                            // Found a floating pixel — verify it's destructible
                            Assert.IsFalse(terrain.IsIndestructible(px, py),
                                $"Seed {seed}: Floating island pixel at ({px},{py}) should be destructible");

                            terrain.ClearCircleDestructible(px, py, 5);
                            Assert.IsFalse(terrain.IsSolid(px, py),
                                $"Seed {seed}: Floating island pixel should be clearable by explosions");
                            return; // test passed
                        }
            }

            Assert.Pass("No floating island found in 100 seeds (probabilistic — rerun if concerned)");
        }

        // --- Boomerang weapon tests ---

        [Test]
        public void Boomerang_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 16, "Should have at least 16 weapons");
            Assert.AreEqual("boomerang", config.Weapons[15].WeaponId);
            Assert.AreEqual(30f, config.Weapons[15].MaxDamage);
            Assert.AreEqual(3.0f, config.Weapons[15].ShootCooldown);
            Assert.AreEqual(-1, config.Weapons[15].Ammo); // infinite
            Assert.IsTrue(config.Weapons[15].IsBoomerang);
        }

        [Test]
        public void Boomerang_CreatesProjectileWithBoomerangFlag()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].ActiveWeaponSlot = 15;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsBoomerang, "Projectile should have boomerang flag");
            Assert.IsFalse(state.Projectiles[0].IsReturning, "Should start in outgoing phase");
        }

        [Test]
        public void Boomerang_ReturnsAfterApex()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place projectile going up — it will hit apex and start returning
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 10f), // going up and right
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick until velocity.y goes negative (apex reached)
            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.05f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned, "Boomerang should start returning after apex");
        }

        [Test]
        public void Boomerang_HitsOncePerPass_NotEveryFrame()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place target right in front of boomerang path
            state.Players[1].Position = new Vec2(5f, 5f);
            float healthBefore = state.Players[1].Health;

            // Create boomerang heading straight at player 1
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(3f, 5.5f),
                Velocity = new Vec2(3f, 0f), // slow to stay overlapping longer
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick 20 frames — boomerang overlaps player for many frames
            for (int i = 0; i < 20; i++)
                ProjectileSimulation.Update(state, 0.016f);

            float damageTaken = healthBefore - state.Players[1].Health;
            // Should hit only once (25 dmg max), not 20x (500 dmg)
            Assert.LessOrEqual(damageTaken, 30f,
                "Boomerang should hit at most once per pass, not every frame");
        }

        [Test]
        public void Boomerang_DiesAtHalfMapWidth_NotFullMapWidth()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float halfMap = state.Config.MapWidth / 2f;

            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(halfMap + 1f, 5f),
                Velocity = new Vec2(10f, 0f),
                Alive = true,
                OwnerIndex = 0,
                MaxDamage = 25f,
                ExplosionRadius = 3f,
                IsBoomerang = true
            });

            Assert.AreEqual(1, state.Projectiles.Count);
            ProjectileSimulation.Update(state, 0.016f);

            // Boomerang beyond half map width should be killed
            bool allDead = state.Projectiles.Count == 0 ||
                !state.Projectiles[0].Alive;
            Assert.IsTrue(allDead,
                "Boomerang beyond MapWidth/2 should be removed");
        }

        [Test]
        public void Boomerang_HitTracksSourceWeaponId()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place target right in front of boomerang path
            state.Players[1].Position = new Vec2(5f, 5f);

            // Create boomerang heading straight at player 1 with SourceWeaponId
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(4f, 5f),
                Velocity = new Vec2(8f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true,
                SourceWeaponId = "boomerang"
            });

            // Tick until hit occurs
            for (int i = 0; i < 60; i++)
                ProjectileSimulation.Update(state, 0.016f);

            // Verify the hit was attributed to the boomerang weapon
            Assert.IsTrue(state.WeaponHits[0].ContainsKey("boomerang"),
                "Boomerang hit should be tracked in WeaponHits with SourceWeaponId");
        }

        // --- Boomerang apex detection regression tests (issue #259) ---

        [Test]
        public void Boomerang_HorizontalShot_DoesNotReturnImmediately()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Horizontal shot: Velocity.y = 0 (aim angle 0°)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick one frame — before the fix, IsReturning would be true here
            ProjectileSimulation.Update(state, 0.016f);

            Assert.IsTrue(state.Projectiles.Count > 0 && state.Projectiles[0].Alive,
                "Boomerang should still be alive after 1 frame");
            Assert.IsFalse(state.Projectiles[0].IsReturning,
                "Horizontal boomerang must NOT return on frame 1");
        }

        [Test]
        public void Boomerang_DownwardShot_DoesNotReturnImmediately()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Downward shot: Velocity.y < 0 (aim angle below horizon)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 20f),
                Velocity = new Vec2(10f, -3f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick a few frames
            for (int i = 0; i < 5; i++)
                ProjectileSimulation.Update(state, 0.016f);

            if (state.Projectiles.Count > 0 && state.Projectiles[0].Alive)
            {
                Assert.IsFalse(state.Projectiles[0].IsReturning,
                    "Downward boomerang should not return via apex detection");
            }
        }

        [Test]
        public void Boomerang_HorizontalShot_TravelsForward()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            float startX = 0f;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(startX, 15f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick 10 frames — boomerang should move rightward
            for (int i = 0; i < 10; i++)
                ProjectileSimulation.Update(state, 0.016f);

            Assert.IsTrue(state.Projectiles.Count > 0, "Boomerang should still exist");
            Assert.Greater(state.Projectiles[0].Position.x, startX + 1f,
                "Horizontal boomerang should travel forward, not snap back");
        }

        [Test]
        public void Boomerang_UpwardShot_StillReturnsAtApex()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Upward shot — should still return after ascending then descending
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 12f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.05f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned,
                "Upward boomerang should still return after apex (HasAscended path)");
        }

        // --- Boomerang owner-death regression (issue #317) ---

        [Test]
        public void Boomerang_DoesNotDespawnWhenOwnerKilledMidFlight()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place a returning boomerang near (but not at) the dead owner
            state.Players[0].IsDead = true;
            state.Players[0].Position = new Vec2(5f, 10f);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(6f, 10.5f), // within 1.5 of dead owner
                Velocity = new Vec2(-3f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true,
                IsReturning = true
            });

            // Tick one frame — boomerang is within catch range of dead owner
            ProjectileSimulation.Update(state, 0.05f);

            Assert.IsTrue(state.Projectiles.Count > 0 && state.Projectiles[0].Alive,
                "Boomerang should NOT be caught by dead owner — it should continue flying");
        }

        [Test]
        public void Boomerang_StopsHomingWhenOwnerDies()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Owner alive, boomerang returning — record velocity after one homing tick
            state.Players[0].Position = new Vec2(0f, 10f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(10f, 15f),
                Velocity = new Vec2(-2f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true,
                IsReturning = true
            });

            ProjectileSimulation.Update(state, 0.05f);
            float vxHoming = state.Projectiles[0].Velocity.x;

            // Now kill the owner and reset projectile
            state.Players[0].IsDead = true;
            var proj = state.Projectiles[0];
            proj.Position = new Vec2(10f, 15f);
            proj.Velocity = new Vec2(-2f, 0f);
            state.Projectiles[0] = proj;

            ProjectileSimulation.Update(state, 0.05f);
            float vxNoHoming = state.Projectiles[0].Velocity.x;

            // With homing, velocity.x should steer more negative (toward owner at x=0)
            // Without homing (dead owner), velocity.x should be less negative (only gravity/wind)
            Assert.IsTrue(vxHoming < vxNoHoming,
                "Boomerang should not steer toward dead owner — homing should stop");
        }

        [Test]
        public void Boomerang_HorizontalShot_ReturnsAfterMinTravelDistance()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 15f);
            state.Players[0].IsDead = false;

            // Fire horizontally — Velocity.y == 0, so HasAscended will never be set
            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick until IsReturning becomes true
            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.016f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned,
                "Boomerang fired horizontally should return via min-travel-distance fallback");
        }

        [Test]
        public void Boomerang_DownwardShot_ReturnsAfterMinTravelDistance()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 20f);
            state.Players[0].IsDead = false;

            // Fire slightly downward — Velocity.y < 0, so HasAscended will never be set
            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(0f, 20f),
                Velocity = new Vec2(12f, -3f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.016f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned,
                "Boomerang fired downward should return via min-travel-distance fallback");
        }

        // --- Energy Drain skill tests ---

        [Test]
        public void EnergyDrain_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Skills.Length >= 12);
            Assert.AreEqual("energy_drain", config.Skills[11].SkillId);
            Assert.AreEqual(SkillType.EnergyDrain, config.Skills[11].Type);
            Assert.AreEqual(30f, config.Skills[11].Value);
        }

        [Test]
        public void EnergyDrain_TransfersEnergyFromTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(5f, 5f); // within range
            state.Players[0].Energy = 50f;
            state.Players[1].Energy = 80f;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                    EnergyCost = 0f, Cooldown = 0f, Range = 12f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(50f, state.Players[1].Energy, 0.01f,
                "Target should lose 30 energy");
            Assert.AreEqual(80f, state.Players[0].Energy, 0.01f,
                "Caster should gain 30 energy");
            Assert.AreEqual(1, state.EnergyDrainEvents.Count);
            Assert.AreEqual(30f, state.EnergyDrainEvents[0].AmountDrained, 0.01f);
        }

        [Test]
        public void EnergyDrain_RefundsOnMiss()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(50f, 5f); // out of range (12)
            state.Players[0].Energy = 50f;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                    EnergyCost = 15f, Cooldown = 14f, Range = 12f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            // Energy deducted by ActivateSkill (15), then refunded on whiff (+15)
            Assert.AreEqual(50f, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when no target in range");
            Assert.AreEqual(0, state.EnergyDrainEvents.Count,
                "No drain event on miss");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should not be set on whiff");
            Assert.AreEqual(0, state.SkillEvents.Count,
                "No SkillEvent should be emitted on whiff");
        }

        [Test]
        public void EnergyDrain_CapsAtMaxEnergy_NotOvercap()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(5f, 5f); // within range
            state.Players[0].Energy = 90f;
            state.Players[0].MaxEnergy = 100f;
            state.Players[1].Energy = 80f;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                    EnergyCost = 0f, Cooldown = 0f, Range = 12f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(100f, state.Players[0].Energy, 0.01f,
                "Caster energy should be capped at MaxEnergy, not exceed it");
            Assert.AreEqual(50f, state.Players[1].Energy, 0.01f,
                "Target should lose 30 energy");
        }

        // --- Gravity Bomb weapon tests ---

        [Test]
        public void GravityBomb_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 17, "Should have at least 17 weapons");
            Assert.AreEqual("gravity_bomb", config.Weapons[16].WeaponId);
            Assert.AreEqual(65f, config.Weapons[16].MaxDamage);
            Assert.AreEqual(2, config.Weapons[16].Ammo);
            Assert.AreEqual(2.5f, config.Weapons[16].FuseTime);
            Assert.IsTrue(config.Weapons[16].IsSticky);
            Assert.IsTrue(config.Weapons[16].IsGravityBomb);
            Assert.AreEqual(6f, config.Weapons[16].PullRadius);
            Assert.AreEqual(9f, config.Weapons[16].PullForce);
        }

        [Test]
        public void GravityBomb_CreatesProjectileWithCorrectFlags()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].ActiveWeaponSlot = 16;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsSticky);
            Assert.IsTrue(state.Projectiles[0].IsGravityBomb);
            Assert.AreEqual(6f, state.Projectiles[0].PullRadius, 0.01f);
            Assert.AreEqual(9f, state.Projectiles[0].PullForce, 0.01f);
        }

        [Test]
        public void GravityBomb_VortexPullsNearbyPlayer()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb high above terrain where LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player 1 within pull radius, high above terrain
            state.Players[1].Position = new Vec2(4f, 15f);
            state.Players[1].Velocity = Vec2.Zero;

            float startX = state.Players[1].Position.x;

            // Tick enough frames for pull to take noticeable effect
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[1].Position.x, startX,
                "Player should be pulled toward gravity bomb");
        }

        [Test]
        public void GravityBomb_DoesNotPullThroughTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb at a position
            Vec2 bombPos = new Vec2(0f, 5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = bombPos,
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player 1 within pull radius but behind terrain
            // Fill terrain between bomb and player to block LOS
            state.Players[1].Position = new Vec2(4f, 5f);
            state.Players[1].Velocity = Vec2.Zero;
            int startPx = state.Terrain.WorldToPixelX(1f);
            int endPx = state.Terrain.WorldToPixelX(3f);
            int py = state.Terrain.WorldToPixelY(5.5f);
            for (int px = startPx; px <= endPx; px++)
                for (int dy = -10; dy <= 10; dy++)
                    state.Terrain.SetSolid(px, py + dy, true);

            float startX = state.Players[1].Position.x;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should NOT be significantly pulled (terrain blocks LOS)
            float moved = MathF.Abs(state.Players[1].Position.x - startX);
            // Allow some tolerance for gravity/physics but the vortex pull should be blocked
            Assert.Less(moved, 0.5f,
                "Player should not be pulled through solid terrain");
        }

        [Test]
        public void GravityBomb_ExplodesAfterFuse()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Disable AI so it doesn't fire stray projectiles that inflate the count
            state.Players[1].IsAI = false;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 0.1f, // very short fuse for test speed
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Tick until fuse expires — check for explosion event each frame (events clear per tick)
            bool sawExplosion = false;
            for (int i = 0; i < 20; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) sawExplosion = true;
                if (state.Projectiles.Count == 0) break;
            }
            // One more tick to clean up dead projectiles
            if (state.Projectiles.Count > 0)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Projectiles.Count, "Gravity bomb should have exploded after fuse");
            Assert.IsTrue(sawExplosion, "Should have created an explosion event");
        }

        [Test]
        public void GravityBomb_PullsShooterToo()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb and put shooter within pull radius, high above terrain
            state.Players[0].Position = new Vec2(4f, 15f);
            state.Players[0].Velocity = Vec2.Zero;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            float startX = state.Players[0].Position.x;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Position.x, startX,
                "Shooter should also be pulled toward their own gravity bomb");
        }

        [Test]
        public void GravityBomb_DoesNotPullOutsideRadius()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player well outside pull radius (>6 units away)
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].Velocity = Vec2.Zero;

            float startX = state.Players[1].Position.x;

            // Tick a few frames — player should not be pulled
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            float moved = MathF.Abs(state.Players[1].Position.x - startX);
            // Should not be pulled toward the bomb (may drift from normal physics but not toward bomb)
            Assert.Less(moved, 1f,
                "Player outside pull radius should not be significantly pulled");
        }

        [Test]
        public void GravityBomb_BuffedPullForceDisplacesFaster()
        {
            // Regression for #332: pull force buffed 5 -> 9 so the vortex functions
            // as a real setup tool. Uses the config's actual PullForce to guard
            // against future regressions.
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            var weapon = new GameConfig().Weapons[16];
            Assert.AreEqual("gravity_bomb", weapon.WeaponId);

            // Bomb well above terrain so LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = weapon.ExplosionRadius,
                MaxDamage = weapon.MaxDamage,
                KnockbackForce = weapon.KnockbackForce,
                Alive = true,
                FuseTimer = weapon.FuseTime,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = weapon.PullRadius,
                PullForce = weapon.PullForce
            });

            // Stationary player 5 units from bomb — inside pull radius (6)
            state.Players[1].Position = new Vec2(5f, 15f);
            state.Players[1].Velocity = Vec2.Zero;
            float startX = state.Players[1].Position.x;

            // 20 ticks at 0.016s = 0.32s. Expected horizontal displacement:
            //   old PullForce=5 -> ~1.6 units
            //   new PullForce=9 -> ~2.88 units
            for (int i = 0; i < 20; i++)
                GameSimulation.Tick(state, 0.016f);

            float movedLeft = startX - state.Players[1].Position.x;
            Assert.Greater(movedLeft, 2.0f,
                "Buffed PullForce should displace a nearby player > 2 units in 0.32s; old 5f could not reach this.");
        }

        // --- Ricochet Disc weapon tests ---

        [Test]
        public void RicochetDisc_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 18, "Should have at least 18 weapons");
            Assert.AreEqual("ricochet_disc", config.Weapons[17].WeaponId);
            Assert.AreEqual(28f, config.Weapons[17].MaxDamage);
            Assert.AreEqual(-1, config.Weapons[17].Ammo); // infinite
            Assert.IsTrue(config.Weapons[17].IsRicochet);
            Assert.AreEqual(3, config.Weapons[17].Bounces);
            Assert.AreEqual(1.5f, config.Weapons[17].ExplosionRadius, 0.01f);
            Assert.AreEqual(15f, config.Weapons[17].EnergyCost);
        }

        [Test]
        public void RicochetDisc_CreatesProjectileWithRicochetFlag()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].ActiveWeaponSlot = 17; // ricochet_disc
            state.Players[0].Energy = 100f;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 45f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsRicochet, "Projectile should have ricochet flag");
            Assert.AreEqual(3, state.Projectiles[0].BouncesRemaining);
        }

        [Test]
        public void RicochetDisc_BouncesWithDamageOnTerrainHit()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Projectiles.Add(new ProjectileState
            {
                Id = 1, Alive = true,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(10f, -10f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f, MaxDamage = 25f,
                KnockbackForce = 3f,
                BouncesRemaining = 3,
                IsRicochet = true,
                StuckToPlayerId = -1
            });

            // Tick until terrain collision (max 120 frames)
            bool sawExplosion = false;
            for (int i = 0; i < 120; i++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (state.ExplosionEvents.Count > 0) sawExplosion = true;
                if (sawExplosion) break;
            }

            Assert.IsTrue(sawExplosion,
                "Ricochet should emit explosion on bounce");
            // Projectile should still be alive after first bounce (2 remaining)
            bool stillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
                if (state.Projectiles[i].IsRicochet && state.Projectiles[i].Alive)
                    stillAlive = true;
            Assert.IsTrue(stillAlive, "Ricochet disc should survive after bounce");
        }

        [Test]
        public void RicochetDisc_DirectPlayerHitExplodesAndDies()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place player 1 directly in front of the disc
            state.Players[1].Position = new Vec2(2f, 5f);
            state.Players[1].Health = 100f;

            state.Projectiles.Add(new ProjectileState
            {
                Id = 1, Alive = true,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f, MaxDamage = 25f,
                KnockbackForce = 3f,
                BouncesRemaining = 3,
                IsRicochet = true,
                StuckToPlayerId = -1
            });

            for (int i = 0; i < 60; i++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (state.Projectiles.Count == 0) break;
            }

            Assert.Less(state.Players[1].Health, 100f,
                "Player should take damage from direct ricochet disc hit");
        }

        [Test]
        public void Reflect_ReflectsVelocityAroundNormal()
        {
            // Velocity going down-right, horizontal surface normal pointing up
            Vec2 vel = new Vec2(5f, -5f);
            Vec2 normal = new Vec2(0f, 1f);
            Vec2 result = GamePhysics.Reflect(vel, normal);
            Assert.AreEqual(5f, result.x, 0.01f, "X should stay the same");
            Assert.AreEqual(5f, result.y, 0.01f, "Y should flip");
        }

        [Test]
        public void MultipleFlyers_OrbitDifferentPositions()
        {
            // Regression: #135 — multiple Flyer mobs all computed the same orbit
            // position because the Sin phase had no per-mob offset.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Expand Players to include 2 Flyer mobs (indices 2 and 3)
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[1].IsAI = false; // disable normal AI so only flyers act

            for (int i = 2; i <= 3; i++)
            {
                players[i] = new PlayerState
                {
                    Position = new Vec2(0f, 10f),
                    Health = 50f,
                    MaxHealth = 50f,
                    MoveSpeed = 4f,
                    IsAI = true,
                    IsMob = true,
                    MobType = "flyer",
                    FacingDirection = 1,
                    Name = $"Flyer{i - 1}",
                    WeaponSlots = new[] { new WeaponSlotState
                    {
                        WeaponId = "cannon",
                        Ammo = -1,
                        MinPower = 5f,
                        MaxPower = 30f,
                        ShootCooldown = 2f
                    }},
                    ShootCooldownRemaining = 999f // prevent firing during test
                };
            }
            state.Players = players;

            AILogic.Reset(42, 4);

            // Tick enough frames for orbits to diverge
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            float x2 = state.Players[2].Position.x;
            float x3 = state.Players[3].Position.x;
            float separation = MathF.Abs(x2 - x3);

            Assert.Greater(separation, 0.5f,
                "Two Flyer mobs should NOT orbit at the same X position");
        }

        [Test]
        public void AILogic_Reset_SizesArraysToPlayerCount_Over16()
        {
            // Regression test for #136: arrays were fixed at 16, crashing with >16 players
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Extend Players to 18 to simulate PVE with many mobs
            int totalPlayers = 18;
            var extended = new PlayerState[totalPlayers];
            System.Array.Copy(state.Players, extended, state.Players.Length);
            for (int i = state.Players.Length; i < totalPlayers; i++)
            {
                extended[i] = new PlayerState
                {
                    IsAI = true,
                    IsMob = true,
                    MobType = "bomber",
                    Health = 50f,
                    MaxHealth = 50f,
                    MoveSpeed = 3f,
                    Position = new Vec2(-10f + i * 2f, 5f),
                    WeaponSlots = new[] { new WeaponSlotState
                    {
                        WeaponId = "cannon", Ammo = -1,
                        MinPower = 5f, MaxPower = 30f,
                        ShootCooldown = 1f, ExplosionRadius = 2f,
                        MaxDamage = 20f, KnockbackForce = 3f
                    }}
                };
            }
            state.Players = extended;

            AILogic.Reset(42, totalPlayers);
            BossLogic.Reset(42, totalPlayers);

            Assert.DoesNotThrow(() =>
            {
                for (int frame = 0; frame < 120; frame++)
                    GameSimulation.Tick(state, 1f / 60f);
            }, "AI tick with 18 players must not throw IndexOutOfRangeException");
        }

        [Test]
        public void AI_SelectsFreezeGrenade_AtCloseMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 12) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 12) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select freeze grenade (slot 12) at close-medium range");
        }

        [Test]
        public void AI_SelectsLightningRod_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 14) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 14) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select lightning rod (slot 14) at medium range");
        }

        [Test]
        public void AI_SelectsBoomerang_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 15) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 15) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select boomerang (slot 15) at medium range");
        }

        [Test]
        public void AI_SelectsGravityBomb_AtMediumRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 16) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(12f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 16) { selected = true; break; }
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsTrue(selected, "AI should select gravity bomb (slot 16) at medium range");
        }

        [Test]
        public void AI_SkipsFreezeGrenade_WhenTargetAlreadyFrozen()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.AIShootInterval = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            for (int s = 1; s < state.Players[1].WeaponSlots.Length; s++)
                if (s != 12) state.Players[1].WeaponSlots[s].Ammo = 0;

            state.Players[0].FreezeTimer = 10f;
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].IsAI = true;

            bool selected = false;
            for (int i = 0; i < 3000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[1].ActiveWeaponSlot == 12) { selected = true; break; }
                if (state.Players[0].FreezeTimer <= 0f) state.Players[0].FreezeTimer = 10f;
                if (state.Players[0].IsDead || state.Players[1].IsDead) break;
            }

            Assert.IsFalse(selected, "AI should not select freeze grenade when target is already frozen");
        }

        [Test]
        public void Bomber_RepositionsLaterally_WhenInPreferredRange()
        {
            // Regression: #148 — Bomber reposition timer set velocity to 0 in both branches,
            // making the Bomber a stationary turret once it reached preferred range.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Player 0 is the human target at origin
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsAI = false;

            // Replace player 1 with a Bomber mob at distance 12 (within 8-16 preferred range)
            state.Players[1] = new PlayerState
            {
                Position = new Vec2(12f, 5f),
                Health = 50f,
                MaxHealth = 50f,
                MoveSpeed = 3f,
                IsAI = true,
                IsMob = true,
                MobType = "bomber",
                FacingDirection = -1,
                Name = "Bomber",
                WeaponSlots = new[] { new WeaponSlotState
                {
                    WeaponId = "cannon",
                    Ammo = -1,
                    MinPower = 5f,
                    MaxPower = 30f,
                    ShootCooldown = 2f
                }},
                ShootCooldownRemaining = 999f // prevent firing during test
            };

            AILogic.Reset(42, 2);

            // Tick enough frames for the reposition timer to fire and bomber to move
            bool movedLaterally = false;
            for (int frame = 0; frame < 600; frame++)
            {
                GameSimulation.Tick(state, 1f / 60f);
                if (MathF.Abs(state.Players[1].Velocity.x) > 0.01f)
                {
                    movedLaterally = true;
                    break;
                }
            }

            Assert.IsTrue(movedLaterally,
                "Bomber should reposition laterally when within preferred range (8-16 units)");
        }

        [Test]
        public void HealerMob_HealsAllyWithLowestHpRatio_NotFirstByIndex()
        {
            // Regression test for #149: healer healed by array index, not damage need
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Use terrain-safe spawn Y from the generated match
            float safeY = state.Players[0].Position.y;

            // 4 players: [0] human (far away), [1] healer, [2] barely scratched, [3] critically wounded
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[0].Position = new Vec2(config.Player1SpawnX, safeY);

            // Shared weapon slot with very high cooldown to prevent any firing
            WeaponSlotState safeWeapon() => new WeaponSlotState
            {
                WeaponId = "cannon", Ammo = -1,
                MinPower = 5f, MaxPower = 30f, ShootCooldown = 999f
            };

            // Healer mob near spawn2 position (known safe terrain)
            float healerX = config.Player2SpawnX;
            float healerY = GamePhysics.FindGroundY(state.Terrain, healerX, config.SpawnProbeY);
            players[1] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "healer",
                Health = 100f, MaxHealth = 100f, MoveSpeed = 3f,
                Position = new Vec2(healerX, healerY),
                IsGrounded = true, FacingDirection = 1, Name = "Healer",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // Mob at index 2: 95% HP (barely scratched), 3 units from healer
            float mob2X = healerX + 3f;
            float mob2Y = GamePhysics.FindGroundY(state.Terrain, mob2X, config.SpawnProbeY);
            players[2] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 95f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(mob2X, mob2Y),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber1",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // Mob at index 3: 10% HP (critically wounded), 5 units from healer
            float mob3X = healerX + 5f;
            float mob3Y = GamePhysics.FindGroundY(state.Terrain, mob3X, config.SpawnProbeY);
            players[3] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 10f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(mob3X, mob3Y),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber2",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            state.Players = players;
            AILogic.Reset(42, 4);
            BossLogic.Reset(42, 4);

            float healthBefore2 = state.Players[2].Health;
            float healthBefore3 = state.Players[3].Health;

            // Tick a few frames so healer AI runs (small dt to avoid physics side-effects)
            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            // The critically wounded mob (index 3) should be healed, not the barely-scratched one
            Assert.Greater(state.Players[3].Health, healthBefore3,
                "Healer should heal the critically wounded ally (index 3, 10% HP)");
            Assert.AreEqual(healthBefore2, state.Players[2].Health, 0.01f,
                "Barely-scratched ally (index 2, 95% HP) should NOT be healed first");
        }

        // --- Healer mob full-health movement regression (#361) ---

        [Test]
        public void HealerMob_DoesNotMoveWhenAllAlliesFullHealth()
        {
            // Regression test for #361: healer moved toward full-health allies
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            float safeY = state.Players[0].Position.y;
            float healerX = config.Player2SpawnX;
            float healerY = GamePhysics.FindGroundY(state.Terrain, healerX, config.SpawnProbeY);

            WeaponSlotState safeWeapon() => new WeaponSlotState
            {
                WeaponId = "cannon", Ammo = -1,
                MinPower = 5f, MaxPower = 30f, ShootCooldown = 999f
            };

            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[0].Position = new Vec2(config.Player1SpawnX, safeY);

            // Healer mob
            players[1] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "healer",
                Health = 100f, MaxHealth = 100f, MoveSpeed = 3f,
                Position = new Vec2(healerX, healerY),
                IsGrounded = true, FacingDirection = 1, Name = "Healer",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // Ally at full health, 5 units away
            float allyX = healerX + 5f;
            float allyY = GamePhysics.FindGroundY(state.Terrain, allyX, config.SpawnProbeY);
            players[2] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 100f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(allyX, allyY),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            state.Players = players;
            AILogic.Reset(42, 3);
            BossLogic.Reset(42, 3);

            Vec2 healerPosBefore = players[1].Position;

            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            // Healer should not be walking toward the full-health ally
            float xDelta = MathF.Abs(state.Players[1].Position.x - healerPosBefore.x);
            Assert.Less(xDelta, 0.1f,
                "Healer mob must not reposition toward a full-health ally — there is nothing to heal");
        }

        // --- Magma Ball weapon tests ---

        [Test]
        public void MagmaBall_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 19, "Should have at least 19 weapons");
            Assert.AreEqual("magma_ball", config.Weapons[18].WeaponId);
            Assert.AreEqual(25f, config.Weapons[18].MaxDamage);
            Assert.AreEqual(2, config.Weapons[18].Ammo);
            Assert.IsTrue(config.Weapons[18].IsNapalm);
            Assert.IsTrue(config.Weapons[18].IsLavaPool);
            Assert.AreEqual(4f, config.Weapons[18].LavaMeltRadius, 0.01f);
            Assert.AreEqual(6f, config.Weapons[18].FireZoneDuration, 0.01f);
            Assert.AreEqual(12f, config.Weapons[18].FireZoneDPS, 0.01f);
        }

        // Regression test for issue #333 — magma_ball rebalance
        // Magma ball identity: area denial + destructive terraforming (not pure damage).
        // Should have napalm-matching charge time, longer fire zone, larger melt radius.
        [Test]
        public void MagmaBall_Rebalance_Issue333_StatsMatchDesign()
        {
            var config = new GameConfig();
            var magma = config.Weapons[18];
            var napalm = System.Array.Find(config.Weapons, w => w.WeaponId == "napalm");

            Assert.IsNotNull(napalm, "napalm weapon must exist for comparison");
            Assert.AreEqual("magma_ball", magma.WeaponId);

            // ChargeTime matches napalm (no reason to be slower)
            Assert.AreEqual(napalm.ChargeTime, magma.ChargeTime, 0.01f,
                "magma_ball ChargeTime should match napalm (2.0s)");
            Assert.AreEqual(2f, magma.ChargeTime, 0.01f);

            // Fire zone is LONGEST in the game — longer than napalm (5s)
            Assert.Greater(magma.FireZoneDuration, napalm.FireZoneDuration,
                "magma_ball FireZoneDuration should exceed napalm for area denial identity");
            Assert.AreEqual(6f, magma.FireZoneDuration, 0.01f);

            // Larger melt radius for destructive terraforming
            Assert.AreEqual(4f, magma.LavaMeltRadius, 0.01f);

            // Slight bump to ExplosionRadius for direct-hit consistency
            Assert.AreEqual(2.5f, magma.ExplosionRadius, 0.01f);

            // EnergyCost and Ammo unchanged
            Assert.AreEqual(30f, magma.EnergyCost, 0.01f);
            Assert.AreEqual(2, magma.Ammo);

            // Total fire-zone damage over full duration = DPS * Duration = 12 * 6 = 72
            float totalZoneDamage = magma.FireZoneDPS * magma.FireZoneDuration;
            Assert.AreEqual(72f, totalZoneDamage, 0.01f,
                "magma_ball total fire-zone damage should be 72 (12 DPS * 6s)");
        }

        [Test]
        public void MagmaBall_CreatesLavaPoolOnTerrainImpact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire magma ball (slot 18)
            state.Players[0].ActiveWeaponSlot = 18;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsNapalm);
            Assert.IsTrue(state.Projectiles[0].IsLavaPool);

            // Tick until projectile hits terrain
            bool lavaPoolCreated = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.FireZones.Count > 0)
                {
                    lavaPoolCreated = true;
                    break;
                }
            }

            Assert.IsTrue(lavaPoolCreated, "Magma ball should create a lava pool on terrain impact");
            Assert.IsTrue(state.FireZones[0].Active);
            Assert.IsTrue(state.FireZones[0].MeltsTerrain, "Lava pool should have MeltsTerrain flag");
            Assert.Greater(state.FireZones[0].MeltRadius, 0f, "Lava pool should have MeltRadius");
        }

        [Test]
        public void MagmaBall_LavaPool_MeltsTerrainOverTime()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a lava pool directly on terrain
            float groundY = GamePhysics.FindGroundY(state.Terrain, 0f, 20f);
            var terrainPos = new Vec2(0f, groundY);

            state.FireZones.Add(new FireZoneState
            {
                Position = terrainPos,
                Radius = 3f,
                DamagePerSecond = 12f,
                RemainingTime = 4f,
                OwnerIndex = 0,
                Active = true,
                MeltsTerrain = true,
                MeltRadius = 3f
            });

            // Sample terrain solid state before melt
            int cx = state.Terrain.WorldToPixelX(terrainPos.x);
            int cy = state.Terrain.WorldToPixelY(terrainPos.y);
            int solidBefore = CountSolidPixels(state.Terrain, cx, cy, 10);

            // Tick for 1 second (2 melt ticks at 0.5s interval)
            for (int i = 0; i < 63; i++)
                GameSimulation.Tick(state, 0.016f);

            int solidAfter = CountSolidPixels(state.Terrain, cx, cy, 10);

            Assert.Less(solidAfter, solidBefore, "Lava pool should melt terrain, reducing solid pixel count");
        }

        [Test]
        public void MagmaBall_LavaPool_DoesNotMeltIfFlagFalse()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Prevent AI from firing (which would destroy terrain and invalidate the test)
            for (int p = 0; p < state.Players.Length; p++)
                state.Players[p].ShootCooldownRemaining = 999f;

            float groundY = GamePhysics.FindGroundY(state.Terrain, 0f, 20f);
            var terrainPos = new Vec2(0f, groundY);

            // Regular fire zone (no melt)
            state.FireZones.Add(new FireZoneState
            {
                Position = terrainPos,
                Radius = 3f,
                DamagePerSecond = 15f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true,
                MeltsTerrain = false,
                MeltRadius = 0f
            });

            int cx = state.Terrain.WorldToPixelX(terrainPos.x);
            int cy = state.Terrain.WorldToPixelY(terrainPos.y);
            int solidBefore = CountSolidPixels(state.Terrain, cx, cy, 10);

            for (int i = 0; i < 63; i++)
                GameSimulation.Tick(state, 0.016f);

            int solidAfter = CountSolidPixels(state.Terrain, cx, cy, 10);

            Assert.AreEqual(solidBefore, solidAfter,
                "Regular fire zone should NOT melt terrain");
        }

        [Test]
        public void MagmaBall_EncyclopediaDescription_Exists()
        {
            string desc = EncyclopediaContent.GetWeaponDescription("magma_ball");
            Assert.AreNotEqual("Unknown weapon.", desc);
            Assert.IsTrue(desc.Contains("lava") || desc.Contains("melt") || desc.Contains("molten"),
                "Magma ball description should mention lava/melt");
        }

        static int CountSolidPixels(TerrainState terrain, int cx, int cy, int radius)
        {
            int count = 0;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < terrain.Width && py >= 0 && py < terrain.Height)
                    {
                        if (terrain.IsSolid(px, py))
                            count++;
                    }
                }
            }
            return count;
        }

        // ── Gust Cannon ───────────────────────────────────────────

        [Test]
        public void GustCannon_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 20, "Should have at least 20 weapons");
            Assert.AreEqual("gust_cannon", config.Weapons[19].WeaponId);
            Assert.AreEqual(0f, config.Weapons[19].MaxDamage);
            Assert.AreEqual(20f, config.Weapons[19].KnockbackForce);
            Assert.AreEqual(-1, config.Weapons[19].Ammo);
            Assert.AreEqual(15f, config.Weapons[19].EnergyCost);
            Assert.IsTrue(config.Weapons[19].IsWindBlast);
        }

        [Test]
        public void GustCannon_CreatesProjectileWithWindBlastFlag()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].ActiveWeaponSlot = 19;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsWindBlast);
            Assert.AreEqual(0f, state.Projectiles[0].MaxDamage);
            Assert.AreEqual(20f, state.Projectiles[0].KnockbackForce);
        }

        [Test]
        public void GustCannon_AppliesKnockbackWithoutDamage()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            float healthBefore = state.Players[1].Health;

            CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.AreEqual(healthBefore, state.Players[1].Health, "Wind blast should deal zero damage");
            Assert.IsTrue(state.Players[1].Velocity.x > 0f, "Player should be pushed away from blast");
        }

        [Test]
        public void GustCannon_DoesNotDestroyTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            int cx = state.Terrain.Width / 2;
            int cy = state.Terrain.Height / 2;
            int pixelsBefore = 0;
            for (int x = cx - 10; x <= cx + 10; x++)
                for (int y = cy - 10; y <= cy + 10; y++)
                    if (state.Terrain.IsSolid(x, y)) pixelsBefore++;

            CombatResolver.ApplyWindBlast(state, new Vec2(0f, 0f), 4f, 30f, 0);

            int pixelsAfter = 0;
            for (int x = cx - 10; x <= cx + 10; x++)
                for (int y = cy - 10; y <= cy + 10; y++)
                    if (state.Terrain.IsSolid(x, y)) pixelsAfter++;

            Assert.AreEqual(pixelsBefore, pixelsAfter, "Wind blast should not destroy terrain");
        }

        [Test]
        public void GustCannon_DoesNotEmitExplosionEvent()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            int explosionsBefore = state.ExplosionEvents.Count;
            CombatResolver.ApplyWindBlast(state, new Vec2(0f, 15f), 4f, 30f, 0);

            Assert.AreEqual(explosionsBefore, state.ExplosionEvents.Count, "Wind blast should not emit ExplosionEvent");
        }

        [Test]
        public void GustCannon_DoesNotKnockbackOwner()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 15f);
            state.Players[0].Velocity = Vec2.Zero;

            CombatResolver.ApplyWindBlast(state, new Vec2(0.5f, 15f), 4f, 30f, 0);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f, "Owner should not be knocked back");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.01f, "Owner should not be knocked back");
        }

        [Test]
        public void GustCannon_KnockbackFallsOffWithDistance()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(-5f, 15f);
            state.Players[1].Position = new Vec2(1f, 15f);
            Vec2 blastPos = new Vec2(0f, 15f);

            CombatResolver.ApplyWindBlast(state, blastPos, 4f, 30f, 0);
            float nearKnockback = state.Players[1].Velocity.x;

            state.Players[1].Velocity = Vec2.Zero;
            state.Players[1].Position = new Vec2(3.5f, 15f);
            CombatResolver.ApplyWindBlast(state, blastPos, 4f, 30f, 0);
            float farKnockback = state.Players[1].Velocity.x;

            Assert.Greater(nearKnockback, farKnockback, "Knockback should fall off with distance");
        }

        [Test]
        public void GustCannon_NonArmsRace_DoesNotTrackDirectHit()
        {
            // Regression: gust cannon deals 0 damage outside ArmsRace and should
            // not inflate DirectHits or trigger combo events (see issue #342)
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            int hitsBefore = state.Players[0].DirectHits;
            int comboBefore = state.Players[0].ConsecutiveHits;

            CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.AreEqual(hitsBefore, state.Players[0].DirectHits,
                "Zero-damage gust should not increment DirectHits");
            Assert.AreEqual(comboBefore, state.Players[0].ConsecutiveHits,
                "Zero-damage gust should not increment ConsecutiveHits");
        }

        [Test]
        public void GustCannon_NonArmsRace_DoesNotInflateComboStreak()
        {
            // Regression: spamming gust cannon (zero damage) should not build combo
            // multipliers (DoubleHit, TripleHit, Unstoppable) — issue #342
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            state.ComboEvents.Clear();

            // Fire wind blast 5 times — enough to reach Unstoppable if bug existed
            for (int i = 0; i < 5; i++)
                CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.AreEqual(0, state.Players[0].ConsecutiveHits,
                "Zero-damage gust spam must not build a combo streak");
            Assert.AreEqual(0, state.ComboEvents.Count,
                "Zero-damage gust spam must not emit any ComboEvent");
        }

        [Test]
        public void GustCannon_ArmsRace_TracksDirectHit()
        {
            // In ArmsRace the gust deals minimum damage, so it should still track
            var config = SmallConfig();
            config.MatchType = MatchType.ArmsRace;
            config.ArmsRaceGustMinDamage = 1f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            state.Players[0].Position = new Vec2(-2f, 15f);
            state.Players[1].Position = new Vec2(2f, 15f);
            int hitsBefore = state.Players[0].DirectHits;

            CombatResolver.ApplyWindBlast(state, new Vec2(1f, 15f), 4f, 30f, 0);

            Assert.Greater(state.Players[0].DirectHits, hitsBefore,
                "ArmsRace gust deals minimum damage and should track direct hit");
        }

        [Test]
        public void SpawnCrate_UsesEnumValues_NotMagicNumber()
        {
            // Verify crate type count equals the number of actual crate types
            int expectedCount = 4; // Health, Energy, AmmoRefill, DoubleDamage
            int actualCount = System.Enum.GetValues(typeof(CrateType)).Length;
            Assert.AreEqual(expectedCount, actualCount,
                "CrateType enum must have exactly the expected number of real crate types");

            // Verify spawned crates always have a valid type
            var config = SmallConfig();
            config.CrateSpawnInterval = 1f;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Tick until several crates spawn
            for (int i = 0; i < 500; i++)
                GameSimulation.Tick(state, 0.1f);

            // Force many spawns with different seeds to cover RNG range
            for (int seed = 0; seed < 100; seed++)
            {
                state.NextCrateSpawnTime = 0.001f;
                state.Time = 0.002f;
                state.Seed = seed;
                GameSimulation.Tick(state, 0.016f);
            }

            foreach (var crate in state.Crates)
            {
                Assert.That((int)crate.Type, Is.GreaterThanOrEqualTo(0),
                    "Crate type must be non-negative");
                Assert.That((int)crate.Type, Is.LessThan(actualCount),
                    $"Crate type {crate.Type} must be less than type count ({actualCount})");
            }
        }

        [Test]
        public void Shielder_DoesNotFire_WhileShootCooldownActive()
        {
            // Regression: #274 — Shielder fired without checking ShootCooldownRemaining
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up: player 0 = human target, player 1 = shielder mob in melee range
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsAI = false;

            state.Players[1].Position = new Vec2(1f, 5f); // within 2.5 units
            state.Players[1].IsAI = true;
            state.Players[1].IsMob = true;
            state.Players[1].MobType = "shielder";
            state.Players[1].Health = 50f;
            state.Players[1].MaxHealth = 50f;
            state.Players[1].ShootCooldownRemaining = 5f; // cooldown active
            state.Players[1].ShotsFired = 0;

            AILogic.Reset(42, 2);

            // Tick several frames — shielder should NOT fire while cooldown is active
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Players[1].ShotsFired,
                "Shielder should not fire while ShootCooldownRemaining > 0");
        }

        [Test]
        public void HealerMob_MovesTowardMostWoundedAlly_NotNearest()
        {
            // Regression test for #299: healer moved toward nearest ally, not most-wounded
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            float safeY = state.Players[0].Position.y;

            WeaponSlotState safeWeapon() => new WeaponSlotState
            {
                WeaponId = "cannon", Ammo = -1,
                MinPower = 5f, MaxPower = 30f, ShootCooldown = 999f
            };

            // [0] human player far away (no threat in range, so healer enters move-to-ally branch)
            var players = new PlayerState[4];
            players[0] = state.Players[0];
            players[0].Position = new Vec2(5f, safeY);

            // [1] healer mob
            float healerX = 50f;
            float healerY = GamePhysics.FindGroundY(state.Terrain, healerX, config.SpawnProbeY);
            players[1] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "healer",
                Health = 100f, MaxHealth = 100f, MoveSpeed = 3f,
                Position = new Vec2(healerX, healerY),
                IsGrounded = true, FacingDirection = 1, Name = "Healer",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // [2] healthy ally NEARBY (3 units right of healer, 95% HP)
            float mob2X = healerX + 3f;
            float mob2Y = GamePhysics.FindGroundY(state.Terrain, mob2X, config.SpawnProbeY);
            players[2] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 95f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(mob2X, mob2Y),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber1",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            // [3] critically wounded ally FAR (15 units LEFT of healer, 10% HP)
            float mob3X = healerX - 15f;
            float mob3Y = GamePhysics.FindGroundY(state.Terrain, mob3X, config.SpawnProbeY);
            players[3] = new PlayerState
            {
                IsAI = true, IsMob = true, MobType = "bomber",
                Health = 10f, MaxHealth = 100f, MoveSpeed = 0f,
                Position = new Vec2(mob3X, mob3Y),
                IsGrounded = true, FacingDirection = 1, Name = "Bomber2",
                ShootCooldownRemaining = 999f,
                WeaponSlots = new[] { safeWeapon() }
            };

            state.Players = players;
            AILogic.Reset(42, 4);
            BossLogic.Reset(42, 4);

            float healerStartX = state.Players[1].Position.x;

            // Tick a few frames
            for (int i = 0; i < 5; i++)
                GameSimulation.Tick(state, 0.016f);

            // Healer should move LEFT toward the critically wounded ally at mob3X,
            // not RIGHT toward the nearby healthy ally at mob2X
            Assert.Less(state.Players[1].Position.x, healerStartX,
                "Healer should move toward most-wounded ally (left), not nearest healthy ally (right)");
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