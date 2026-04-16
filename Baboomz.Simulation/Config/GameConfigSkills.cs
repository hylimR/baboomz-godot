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
                EnergyCost = 40f, Cooldown = 8f, Duration = 0f,
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
                EnergyCost = 20f, Cooldown = 4f, Duration = 0.2f,  // balanced: 3→4s cooldown (balance 2026-03-25)
                Range = 0f, Value = 40f  // burst velocity
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
                EnergyCost = 30f, Cooldown = 6f, Duration = 2f,
                Range = 0f, Value = 12f  // upward force
            },
            new SkillDef
            {
                SkillId = "girder", Type = SkillType.Girder,
                EnergyCost = 30f, Cooldown = 15f, Duration = 0f,
                Range = 12f, Value = 4f  // girder width
            },
            new SkillDef
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 35f, Cooldown = 16f, Duration = 0f,
                Range = 0f, Value = 20f  // damage to all grounded
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
                EnergyCost = 40f, Cooldown = 18f, Duration = 5f,
                Range = 0f, Value = 1.5f  // damage multiplier (team); solo gets 1.75x
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
                EnergyCost = 20f, Cooldown = 14f, Duration = 0f,
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
                EnergyCost = 30f, Cooldown = 16f, Duration = 4f,
                Range = 0f, Value = 30f  // decoy HP (survives chip damage, dies to real weapons)
            },
            new SkillDef
            {
                SkillId = "hookshot", Type = SkillType.HookShot,
                EnergyCost = 30f, Cooldown = 12f, Duration = 0f,
                Range = 10f, Value = 10f  // damage dealt to pulled target
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
                EnergyCost = 30f, Cooldown = 14f, Duration = 0f,
                Range = 12f, Value = 3f  // repair radius in world units
            }
        };
        public int DefaultSkillSlot0 = 0;  // teleport
        public int DefaultSkillSlot1 = 3;  // dash
    }
}
