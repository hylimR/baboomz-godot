using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public partial class PayloadTests
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
    }
}
