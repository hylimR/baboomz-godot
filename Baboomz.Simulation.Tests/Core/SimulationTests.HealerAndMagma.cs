using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
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
    }
}
