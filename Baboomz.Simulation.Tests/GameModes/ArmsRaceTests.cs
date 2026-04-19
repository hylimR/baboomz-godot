using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class ArmsRaceTests
    {
        static GameConfig ArmsRaceConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.ArmsRace,
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
                ArmsRaceMaxTime = 180f,
                ArmsRaceGustMinDamage = 1f
            };
        }

        [Test]
        public void CreateMatch_ArmsRace_InitializesState()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);

            Assert.IsNotNull(state.ArmsRace.CurrentWeaponIndex);
            Assert.AreEqual(2, state.ArmsRace.CurrentWeaponIndex.Length);
            Assert.AreEqual(0, state.ArmsRace.CurrentWeaponIndex[0]);
            Assert.AreEqual(0, state.ArmsRace.CurrentWeaponIndex[1]);
        }

        [Test]
        public void CreateMatch_ArmsRace_AllAmmoInfinite()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);

            for (int p = 0; p < state.Players.Length; p++)
            {
                for (int w = 0; w < state.Players[p].WeaponSlots.Length; w++)
                {
                    if (state.Players[p].WeaponSlots[w].WeaponId != null)
                        Assert.AreEqual(-1, state.Players[p].WeaponSlots[w].Ammo,
                            $"Player {p} weapon slot {w} should have infinite ammo");
                }
            }
        }

        [Test]
        public void CreateMatch_ArmsRace_DisablesCrates_ViaModeFlag()
        {
            var config = ArmsRaceConfig();
            config.CrateSpawnInterval = 20f; // would normally spawn crates
            var state = GameSimulation.CreateMatch(config, 42);

            // Mode flag must be set — config must NOT be mutated
            Assert.IsTrue(state.ArmsRace.DisableCrates,
                "ArmsRace.DisableCrates flag must be true");
            Assert.AreEqual(20f, state.Config.CrateSpawnInterval, 0.01f,
                "Config.CrateSpawnInterval must not be mutated by Arms Race init");
        }

        [Test]
        public void CreateMatch_ArmsRace_DisablesSuddenDeath_ViaModeFlag()
        {
            var config = ArmsRaceConfig();
            config.SuddenDeathTime = 90f; // would normally trigger sudden death
            var state = GameSimulation.CreateMatch(config, 42);

            // Mode flag must be set — config must NOT be mutated
            Assert.IsTrue(state.ArmsRace.DisableSuddenDeath,
                "ArmsRace.DisableSuddenDeath flag must be true");
            Assert.AreEqual(90f, state.Config.SuddenDeathTime, 0.01f,
                "Config.SuddenDeathTime must not be mutated by Arms Race init");
        }

        [Test]
        public void InitArmsRace_DoesNotMutateConfigForSubsequentRounds()
        {
            // Regression test: config must remain usable in subsequent Deathmatch rounds
            var config = ArmsRaceConfig();
            config.CrateSpawnInterval = 20f;
            config.SuddenDeathTime = 90f;

            // Simulate an Arms Race round
            var armsRaceState = GameSimulation.CreateMatch(config, 42);

            // Simulate a subsequent Deathmatch round using the SAME config
            config.MatchType = MatchType.Deathmatch;
            var dmState = GameSimulation.CreateMatch(config, 99);

            Assert.AreEqual(20f, dmState.Config.CrateSpawnInterval, 0.01f,
                "CrateSpawnInterval must be intact for the subsequent Deathmatch round");
            Assert.AreEqual(90f, dmState.Config.SuddenDeathTime, 0.01f,
                "SuddenDeathTime must be intact for the subsequent Deathmatch round");
        }

        [Test]
        public void OnArmsRaceDamage_AdvancesWeapon()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);

            Assert.AreEqual(0, state.ArmsRace.CurrentWeaponIndex[0]);
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            Assert.AreEqual(1, state.ArmsRace.CurrentWeaponIndex[0]);
        }

        [Test]
        public void OnArmsRaceDamage_SelfDamage_DoesNotAdvance()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);

            GameSimulation.OnArmsRaceDamage(state, 0, 0); // self-damage
            Assert.AreEqual(0, state.ArmsRace.CurrentWeaponIndex[0],
                "Self-damage should not advance weapon");
        }

        [Test]
        public void OnArmsRaceDamage_CompletesAllWeapons_WinsMatch()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);
            int totalWeapons = state.Config.Weapons.Length;

            // Advance through all weapons
            for (int i = 0; i < totalWeapons; i++)
                GameSimulation.OnArmsRaceDamage(state, 0, 1);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex);
        }

        [Test]
        public void OnArmsRaceDamage_NonArmsRace_NoEffect()
        {
            var config = ArmsRaceConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            // Should not crash or change anything
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            Assert.AreEqual(MatchPhase.Playing, state.Phase);
        }

        [Test]
        public void UpdateArmsRace_ForcesWeaponSlot()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);

            // Advance player 0 to weapon 3
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            GameSimulation.OnArmsRaceDamage(state, 0, 1);

            Assert.AreEqual(3, state.ArmsRace.CurrentWeaponIndex[0]);

            // Manually set wrong slot
            state.Players[0].ActiveWeaponSlot = 0;

            // UpdateArmsRace should force it back
            GameSimulation.UpdateArmsRace(state, 0.016f);
            Assert.AreEqual(3, state.Players[0].ActiveWeaponSlot);
        }

        [Test]
        public void UpdateArmsRace_TimerExpires_HigherWeaponIndexWins()
        {
            var config = ArmsRaceConfig();
            config.ArmsRaceMaxTime = 10f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Player 0 advances to weapon 3
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            GameSimulation.OnArmsRaceDamage(state, 0, 1);

            // Player 1 advances to weapon 1
            GameSimulation.OnArmsRaceDamage(state, 1, 0);

            // Simulate time passing
            state.Time = 10f;
            GameSimulation.UpdateArmsRace(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Player 0 should win (weapon 3 > weapon 1)");
        }

        [Test]
        public void UpdateArmsRace_TimerExpires_TiedIndex_MostDamageWins()
        {
            var config = ArmsRaceConfig();
            config.ArmsRaceMaxTime = 10f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Both advance to weapon 1
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            GameSimulation.OnArmsRaceDamage(state, 1, 0);

            // Player 1 dealt more damage
            state.Players[0].TotalDamageDealt = 50f;
            state.Players[1].TotalDamageDealt = 80f;

            state.Time = 10f;
            GameSimulation.UpdateArmsRace(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex, "Player 1 should win (tied index, more damage)");
        }

        [Test]
        public void OnArmsRaceDamage_AlreadyCompleted_DoesNotAdvanceFurther()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);
            int totalWeapons = state.Config.Weapons.Length;

            // Complete all weapons
            for (int i = 0; i < totalWeapons; i++)
                GameSimulation.OnArmsRaceDamage(state, 0, 1);

            Assert.AreEqual(totalWeapons, state.ArmsRace.CurrentWeaponIndex[0]);

            // Try to advance again
            GameSimulation.OnArmsRaceDamage(state, 0, 1);
            Assert.AreEqual(totalWeapons, state.ArmsRace.CurrentWeaponIndex[0],
                "Should not advance past total weapon count");
        }

        [Test]
        public void GustCannon_ArmsRace_DealsMinDamage()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);
            float healthBefore = state.Players[1].Health;

            // Place players close together for wind blast
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(2f, 5f);

            CombatResolver.ApplyWindBlast(state, state.Players[0].Position, 4f, 20f, 0);

            Assert.AreEqual(healthBefore - 1f, state.Players[1].Health, 0.01f,
                "Gust cannon should deal 1 minimum damage in Arms Race");
        }

        [Test]
        public void GustCannon_NonArmsRace_DealsNoDamage()
        {
            var config = ArmsRaceConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);
            float healthBefore = state.Players[1].Health;

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(2f, 5f);

            CombatResolver.ApplyWindBlast(state, state.Players[0].Position, 4f, 20f, 0);

            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Gust cannon should deal no damage outside Arms Race");
        }

        [Test]
        public void ArmsRace_PlayerDeath_EndsMatch()
        {
            var state = GameSimulation.CreateMatch(ArmsRaceConfig(), 42);

            // Kill player 1
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            // Tick should end the match via CheckMatchEnd
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex);
        }
        [Test]
        public void HookShot_ArmsRace_AdvancesWeapon()
        {
            var config = ArmsRaceConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "hookshot", Type = SkillType.HookShot,
                EnergyCost = 0f, Cooldown = 0f, Range = 20f, Value = 10f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            int weaponBefore = state.ArmsRace.CurrentWeaponIndex[0];
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(weaponBefore + 1, state.ArmsRace.CurrentWeaponIndex[0],
                "HookShot damage should advance weapon in Arms Race");
        }

        [Test]
        public void Earthquake_ArmsRace_AdvancesWeapon()
        {
            var config = ArmsRaceConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].IsGrounded = true;

            int weaponBefore = state.ArmsRace.CurrentWeaponIndex[0];
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(weaponBefore + 1, state.ArmsRace.CurrentWeaponIndex[0],
                "Earthquake damage should advance weapon in Arms Race");
        }
    }
}
