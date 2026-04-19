namespace Baboomz.Simulation
{
    /// <summary>Skill definitions and skill-related config (partial class of GameConfig).</summary>
    public partial class GameConfig
    {
        // Skills
        public SkillDef[] Skills = new[]
        {
            new SkillDef
            {
                SkillId = "teleport", Type = SkillType.Teleport,
                // Balance #171: 40E→28E. At 40E/15u Teleport was 2.2x worse E/u than
                // Dash (18E/15u). 28E/15u = 1.87 E/u — still ~1.6x pricier than Dash,
                // preserving the premium for instant terrain-bypass repositioning.
                EnergyCost = 28f, Cooldown = 8f, Duration = 0f,
                Range = 15f, Value = 0f
            },
            new SkillDef
            {
                SkillId = "grapple", Type = SkillType.GrapplingHook,
                EnergyCost = 25f, Cooldown = 5f, Duration = 2f,  // swing duration
                Range = 20f, Value = 15f  // pull speed (legacy, unused in swing mode)
            },
            new SkillDef
            {
                SkillId = "shield", Type = SkillType.Shield,
                EnergyCost = 35f, Cooldown = 12f, Duration = 3f,
                Range = 0f, Value = 3f  // armor multiplier
            },
            new SkillDef
            {
                SkillId = "dash", Type = SkillType.Dash,
                // Balance #155: Dash was strictly dominated at 20E/4s/0.2s/40u → ~8u for 2.5 E/u.
                // Bump duration 0.2→0.3s, velocity 40→50u (→~15u reach), cost 20→18E, CD 4→3s
                // so it owns the "cheap frequent short disengage" niche vs Grapple/Jetpack/Teleport.
                EnergyCost = 18f, Cooldown = 3f, Duration = 0.3f,
                Range = 0f, Value = 50f  // burst velocity
            },
            new SkillDef
            {
                SkillId = "heal", Type = SkillType.Heal,
                // Balance #49: 40E/15s/25HP → 35E/12s/35HP to match Shield's cost tier
                EnergyCost = 35f, Cooldown = 12f, Duration = 2f,
                Range = 0f, Value = 35f  // total HP restored
            },
            new SkillDef
            {
                SkillId = "jetpack", Type = SkillType.Jetpack,
                EnergyCost = 30f, Cooldown = 5f, Duration = 2f,
                Range = 0f, Value = 15f  // upward force
            },
            new SkillDef
            {
                SkillId = "girder", Type = SkillType.Girder,
                EnergyCost = 25f, Cooldown = 12f, Duration = 0f,
                Range = 12f, Value = 4f  // girder width
            },
            new SkillDef
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                // Balance #141: Mine Layer (PR #125) at 25E/10s delivers 105 damage (35 x 3 mines).
                // Earthquake's old 20 damage / 16s made it ~12x less efficient per energy.
                // Bump damage 20 -> 35 (matches one mine trigger) and cooldown 16 -> 14s so
                // Earthquake becomes a viable anti-camping counter to Mine Layer area denial.
                EnergyCost = 35f, Cooldown = 14f, Duration = 0f,
                Range = 0f, Value = 35f  // damage to all grounded
            },
            new SkillDef
            {
                SkillId = "smoke", Type = SkillType.SmokeScreen,
                EnergyCost = 25f, Cooldown = 10f, Duration = 4f,
                Range = 8f, Value = 5f  // smoke radius
            },
            new SkillDef
            {
                SkillId = "warcry", Type = SkillType.WarCry,
                // Balance #126: 40E -> 30E to close the gap with Overcharge's solo value
                // Balance #172: 18s→15s CD. Conditional team buff (1.5×) at 18s was too
                // infrequent (~3.3 casts/match). 15s aligns with Earthquake/Mend tier
                // and yields ~4 casts/match — viable rotation skill for team play.
                EnergyCost = 30f, Cooldown = 15f, Duration = 5f,
                Range = 0f, Value = 1.5f  // damage multiplier (team); solo gets 1.9x (#126)
            },
            new SkillDef
            {
                SkillId = "mine_layer", Type = SkillType.MineLay,
                // Balance #125: 12s/30dmg/2max → 10s/35dmg/3max for genuine area denial
                EnergyCost = 25f, Cooldown = 10f, Duration = 0f,
                Range = 10f, Value = 35f  // mine damage
            },
            new SkillDef
            {
                SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                // Balance #156: 14s CD matched damage skills (Earthquake/Mend) despite
                // pure-utility payload. 14s→10s drops it to the Smoke Screen / Mine Layer
                // utility tier (~6 casts/match instead of 4), making it a real tempo tool
                // instead of a once-a-match anti-Overcharge gimmick. Payload unchanged.
                EnergyCost = 20f, Cooldown = 10f, Duration = 0f,
                Range = 12f, Value = 30f  // energy drained from target
            },
            new SkillDef
            {
                SkillId = "deflect", Type = SkillType.Deflect,
                EnergyCost = 30f, Cooldown = 13f, Duration = 1.5f,
                Range = 2.5f, Value = 0f  // Range = deflection radius around player
            },
            new SkillDef
            {
                SkillId = "decoy", Type = SkillType.Decoy,
                // Balance #173: 16s→13s CD. Decoy had the longest CD among evasion skills
                // (Deflect 13s, Shadow Step 12s) despite conditional activation and fragile
                // 30HP dummy. 13s aligns with Deflect — still riskier due to enemy-dependent value.
                EnergyCost = 30f, Cooldown = 13f, Duration = 4f,
                Range = 0f, Value = 30f  // decoy HP (survives chip damage, dies to real weapons)
            },
            new SkillDef
            {
                SkillId = "hookshot", Type = SkillType.HookShot,
                // Balance #164: 30E/12s/10dmg → 25E/10s/20dmg. HookShot sat at
                // 0.33 dmg/E — ~3x worse than Earthquake and 12x worse than
                // Mine Layer — so the "pull + finisher" fantasy never paid off.
                // Align cost and CD with the utility tier (Drain, Mine Lay) and
                // bump damage to ~1/3 cannon shot so a hooked target actually
                // feels threatened, while keeping dmg/E (0.80) the lowest among
                // damage skills (fits its hybrid utility role).
                EnergyCost = 25f, Cooldown = 10f, Duration = 0f,
                Range = 10f, Value = 20f  // damage dealt to pulled target
            },
            new SkillDef
            {
                SkillId = "shadow_step", Type = SkillType.ShadowStep,
                EnergyCost = 25f, Cooldown = 12f, Duration = 3f,
                Range = 0f, Value = 0f
            },
            new SkillDef
            {
                SkillId = "overcharge", Type = SkillType.Overcharge,
                EnergyCost = 0f, Cooldown = 18f, Duration = 5f,
                Range = 60f, Value = 2f  // Range = min energy gate, Value = damage multiplier
            },
            new SkillDef
            {
                SkillId = "mend", Type = SkillType.Mend,
                EnergyCost = 20f, Cooldown = 10f, Duration = 0f,
                Range = 12f, Value = 3f  // repair radius in world units
            },
            new SkillDef
            {
                SkillId = "magnetic_mine", Type = SkillType.MagneticMine,
                EnergyCost = 25f, Cooldown = 10f, Duration = 0f,
                Range = 10f, Value = 30f  // mine damage
            },
            new SkillDef
            {
                SkillId = "petrify", Type = SkillType.Petrify,
                EnergyCost = 35f, Cooldown = 14f, Duration = 2f, // Duration = freeze time applied to targets
                Range = 10f, Value = 2f  // Value = AoE radius
            },
            new SkillDef
            {
                SkillId = "landslide", Type = SkillType.Landslide,
                EnergyCost = 35f, Cooldown = 12f, Duration = 0f,
                Range = 10f, Value = 6f  // column height in world units
            },
            new SkillDef
            {
                SkillId = "sprint", Type = SkillType.Sprint,
                // Between Dash (18E/3s) and Teleport (28E/8s) — sustained horizontal repositioning
                // without terrain bypass. 1.5x speed for 2s covers ~15u at default MoveSpeed.
                EnergyCost = 22f, Cooldown = 7f, Duration = 2f,
                Range = 0f, Value = 1.5f  // speed multiplier
            }
        };
        public int DefaultSkillSlot0 = 0;  // teleport
        public int DefaultSkillSlot1 = 3;  // dash
    }
}
