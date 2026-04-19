using System;

namespace Baboomz.Simulation
{
    /// <summary>AI skill loadout selection and skill activation helpers (partial class of AILogic).</summary>
    public static partial class AILogic
    {
        /// <summary>
        /// Pick 4 weapon slot indices for AI based on difficulty.
        /// Easy: random. Normal: balanced (1 heavy + 3 utility). Hard: optimized DPS set.
        /// </summary>
        public static int[] PickWeaponLoadout(GameConfig config, int seed)
        {
            var r = new Random(seed + 999);
            int count = config.Weapons.Length;
            if (count <= 4) return BuildRange(count);

            // Weapon index groups (by WeaponId position in default config)
            // 0=Cannon, 1=Shotgun, 2=Rocket, 3=Cluster, 4=Dynamite, 5=Napalm,
            // 6=Airstrike, 7=Drill, 8=Blowtorch, 9=HolyHandGrenade, 10=Sheep,
            // 11=BananaBomb, 12=FreezeGrenade, 13=StickyBomb, 14=LightningRod,
            // 15=Boomerang, 16=GravityBomb, 17=RicochetDisc, 18=MagmaBall,
            // 19=GustCannon, 20=Harpoon, 21=FlakCannon
            int[] heavy   = { 2, 6, 9, 11, 16 };  // high damage / area
            int[] utility = { 1, 3, 4, 5, 7, 8, 12, 13, 14, 15, 17, 18, 19, 20, 21 }; // versatile
            int cannon = 0; // always include cannon (infinite ammo)

            int s0, s1, s2, s3;
            if (config.AIDifficultyLevel <= 0)
            {
                // Easy: random 4 distinct slots
                s0 = r.Next(count); s1 = UniqueFrom(r, count, s0);
                s2 = UniqueFrom(r, count, s0, s1); s3 = UniqueFrom(r, count, s0, s1, s2);
            }
            else if (config.AIDifficultyLevel >= 2)
            {
                // Hard: cannon + 2 heavy + 1 utility
                s0 = cannon;
                s1 = heavy[r.Next(heavy.Length)]; while (s1 == s0) s1 = heavy[r.Next(heavy.Length)];
                s2 = heavy[r.Next(heavy.Length)]; while (s2 == s0 || s2 == s1) s2 = heavy[r.Next(heavy.Length)];
                s3 = utility[r.Next(utility.Length)];
            }
            else
            {
                // Normal: cannon + 1 heavy + 2 utility
                s0 = cannon;
                s1 = heavy[r.Next(heavy.Length)];
                s2 = utility[r.Next(utility.Length)]; while (s2 == s1) s2 = utility[r.Next(utility.Length)];
                s3 = utility[r.Next(utility.Length)]; while (s3 == s1 || s3 == s2) s3 = utility[r.Next(utility.Length)];
            }

            return new[] { s0, s1, s2, s3 };
        }

        static int UniqueFrom(Random r, int count, int a, int b = -1, int c = -1)
        {
            int n;
            do { n = r.Next(count); } while (n == a || n == b || n == c);
            return n;
        }

        static int[] BuildRange(int count)
        {
            var arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            return arr;
        }

        /// <summary>
        /// Pick 2 skill slot indices for AI based on difficulty.
        /// Easy: random. Normal: weighted toward mobility + utility. Hard: strategic picks.
        /// </summary>
        public static int[] PickLoadout(GameConfig config, int seed)
        {
            var r = new Random(seed + 777); // offset seed so loadout differs from other RNG
            int count = config.Skills.Length;
            if (count < 2) return new[] { 0, Math.Min(1, count - 1) };

            int s0, s1;

            // Mobility indices: teleport(0), dash(3), jetpack(5), shadow_step(15), sprint(20)
            // Defensive indices: shield(2), heal(4)
            // Utility indices: girder(6), earthquake(7), smoke(8), warcry(9), mine_layer(10), energy_drain(11), deflect(12), decoy(13), hook_shot(14), overcharge(16), mend(17), magnetic_mine(18), petrify(19)
            int[] mobility = { 0, 3, 5, 15, 20 };
            int[] defensive = { 2, 4 };
            int[] utility = { 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 18, 19 };

            if (config.AIDifficultyLevel <= 0)
            {
                // Easy: fully random picks
                s0 = r.Next(count);
                s1 = r.Next(count);
                while (s1 == s0)
                    s1 = r.Next(count);
            }
            else if (config.AIDifficultyLevel >= 2)
            {
                // Hard: strategic — mobility + defensive for maximum survivability
                s0 = mobility[r.Next(mobility.Length)];
                s1 = defensive[r.Next(defensive.Length)];
                while (s1 == s0)
                    s1 = defensive[r.Next(defensive.Length)];
            }
            else
            {
                // Normal: one mobility + one from defensive/utility
                s0 = mobility[r.Next(mobility.Length)];
                int[] pool = r.Next(2) == 0 ? defensive : utility;
                s1 = pool[r.Next(pool.Length)];
                while (s1 == s0)
                    s1 = r.Next(count);
            }

            return new[] { s0, s1 };
        }

        // --- Skill helpers ---

        static void TryUseSkills(GameState state, int index, float dt)
        {
            ref PlayerState ai = ref state.Players[index];
            if (ai.SkillSlots == null) return;

            float dangerY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel) + 5f;
            if (ai.Position.y < dangerY && ai.Velocity.y < 0f)
            {
                if (TryActivateSkillByType(state, index, SkillType.Jetpack)) return;
                // GrapplingHook as alternative to Jetpack for falling rescue
                if (TryActivateSkillByType(state, index, SkillType.GrapplingHook)) return;
            }

            float hpPercent = ai.MaxHealth > 0f ? ai.Health / ai.MaxHealth : 0f;
            if (hpPercent < 0.4f)
            {
                if (TryActivateSkillByType(state, index, SkillType.Heal)) return;
            }

            if (hpPercent < 0.6f && HasIncomingProjectile(state, index, 5f))
            {
                if (TryActivateSkillByType(state, index, SkillType.Shield)) return;
            }

            if (HasIncomingProjectile(state, index, 3f))
            {
                if (TryActivateSkillByType(state, index, SkillType.Dash)) return;
            }

            if (rng.NextDouble() < 0.005 * dt * 60.0)
            {
                if (TryActivateSkillByType(state, index, SkillType.Teleport)) return;
                if (TryActivateSkillByType(state, index, SkillType.ShadowStep)) return;
            }

            // Hook Shot when enemy is within range (pull them toward us)
            if (rng.NextDouble() < 0.008 * dt * 60.0 && HasEnemyInRange(state, index, 10f))
            {
                if (TryActivateSkillByType(state, index, SkillType.HookShot)) return;
            }

            // Earthquake when 2+ enemies grounded
            if (rng.NextDouble() < 0.01 * dt * 60.0 && CountGroundedEnemies(state, index) >= 2)
                TryActivateSkillByType(state, index, SkillType.Earthquake);

            // Petrify when 2+ enemies are clustered within range (AoE freeze)
            if (rng.NextDouble() < 0.01 * dt * 60.0 && CountEnemiesInRange(state, index, 10f) >= 2)
                TryActivateSkillByType(state, index, SkillType.Petrify);

            // GrapplingHook for mobility (near map edge or after knockback)
            float halfMap = state.Config.MapWidth / 2f;
            if (MathF.Abs(ai.Position.x) > halfMap * 0.8f && rng.NextDouble() < 0.01 * dt * 60.0)
                TryActivateSkillByType(state, index, SkillType.GrapplingHook);

            // Mend for defensive terrain cover when HP is low
            if (hpPercent < 0.5f && rng.NextDouble() < 0.008 * dt * 60.0)
                TryActivateSkillByType(state, index, SkillType.Mend);

            // Magnetic Mine when enemy is within detection range
            if (rng.NextDouble() < 0.008 * dt * 60.0 && HasEnemyInRange(state, index, 10f))
                TryActivateSkillByType(state, index, SkillType.MagneticMine);

            // Overcharge when saved up energy and an enemy is in firing range (commit to a big shot)
            if (ai.Energy >= 80f && rng.NextDouble() < 0.015 * dt * 60.0 && HasEnemyInRange(state, index, 25f))
                TryActivateSkillByType(state, index, SkillType.Overcharge);

            // Sprint for repositioning (random chance, similar to Teleport/ShadowStep)
            if (rng.NextDouble() < 0.006 * dt * 60.0)
                TryActivateSkillByType(state, index, SkillType.Sprint);
        }

        static bool HasEnemyInRange(GameState state, int selfIndex, float range)
        {
            int selfTeam = state.Players[selfIndex].TeamIndex;
            Vec2 pos = state.Players[selfIndex].Position;
            for (int e = 0; e < state.Players.Length; e++)
            {
                if (e == selfIndex || state.Players[e].IsDead) continue;
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[e].TeamIndex == selfTeam) continue;
                if (Vec2.Distance(pos, state.Players[e].Position) < range) return true;
            }
            return false;
        }

        static int CountGroundedEnemies(GameState state, int selfIndex)
        {
            int count = 0;
            int selfTeam = state.Players[selfIndex].TeamIndex;
            for (int e = 0; e < state.Players.Length; e++)
            {
                if (e == selfIndex || state.Players[e].IsDead || !state.Players[e].IsGrounded) continue;
                // Skip teammates in team mode
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[e].TeamIndex == selfTeam) continue;
                count++;
            }
            return count;
        }

        static int CountEnemiesInRange(GameState state, int selfIndex, float range)
        {
            int count = 0;
            int selfTeam = state.Players[selfIndex].TeamIndex;
            Vec2 pos = state.Players[selfIndex].Position;
            for (int e = 0; e < state.Players.Length; e++)
            {
                if (e == selfIndex || state.Players[e].IsDead) continue;
                if (state.Config.TeamMode && selfTeam >= 0 && state.Players[e].TeamIndex == selfTeam) continue;
                if (Vec2.Distance(pos, state.Players[e].Position) < range) count++;
            }
            return count;
        }

        static bool TryActivateSkillByType(GameState state, int playerIndex, SkillType type)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            for (int s = 0; s < p.SkillSlots.Length; s++)
            {
                if (p.SkillSlots[s].SkillId != null && p.SkillSlots[s].Type == type
                    && p.SkillSlots[s].CooldownRemaining <= 0f && !p.SkillSlots[s].IsActive
                    && p.Energy >= p.SkillSlots[s].EnergyCost)
                {
                    SkillSystem.ActivateSkill(state, playerIndex, s);
                    return true;
                }
            }
            return false;
        }

        static bool HasIncomingProjectile(GameState state, int playerIndex, float radius)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                var proj = state.Projectiles[i];
                if (!proj.Alive || proj.OwnerIndex == playerIndex) continue;
                float dist = Vec2.Distance(proj.Position, p.Position);
                if (dist < radius) return true;
            }
            return false;
        }
    }
}
