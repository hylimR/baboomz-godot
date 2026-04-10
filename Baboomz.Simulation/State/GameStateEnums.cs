namespace Baboomz.Simulation
{
    public enum CrateType
    {
        Health,     // restores 25 HP
        Energy,     // restores 50 energy
        AmmoRefill, // refills all limited-ammo weapons
        DoubleDamage // 2x damage for 10 seconds
    }

    public enum SkillType
    {
        Teleport,
        GrapplingHook,
        Shield,
        Dash,
        Heal,
        Jetpack,
        Girder,
        Earthquake,
        SmokeScreen,
        WarCry,
        MineLay,
        EnergyDrain,
        Deflect,
        Decoy,
        HookShot,
        ShadowStep,
        Overcharge,
        Mend
    }

    public enum HatType
    {
        None,
        TopHat,
        AviatorCap,
        Crown,
        PirateHat,
        ChefHat,
        VikingHelmet,
        WizardHat,
        SamuraiHelmet,
        DragonCrown,
        Halo,
        GoldenCrown
    }

    public enum EmoteType
    {
        None,
        Taunt,    // "Ha!"
        Laugh,    // "Haha!"
        Cry,      // "Nooo!"
        ThumbsUp, // "GG!"
        Clap,     // "Bravo!"
        Salute,   // "Sir!"
        Dance,    // "Woo!"
        Flex      // "Flex!"
    }

    public enum BiomeHazardType
    {
        Mud,         // halves movement speed
        Quicksand,   // pulls player downward
        Ice,         // zero friction (preserves momentum)
        Lava,        // deals DPS
        Bounce,      // launches player upward
        Firecracker, // launches player upward + random horizontal (no damage, per-player cooldown)
        Gear,        // pushes player horizontally, alternating direction every 3s
        Whirlpool,   // pulls player toward center (radial force)
        Waterspout   // launches player upward with strong Y impulse
    }

    public enum MatchType
    {
        Deathmatch,
        KingOfTheHill,
        Survival,
        TargetPractice,
        Demolition,
        ArmsRace,
        Payload,
        Campaign,
        Roulette,
        CaptureTheFlag,
        Headhunter,
        Territories
    }

    public enum TargetType
    {
        StaticNear,
        StaticMid,
        StaticFar,
        MovingHorizontal,
        MovingVertical
    }

    public enum ComboType
    {
        DoubleHit,
        TripleHit,
        QuadHit,
        Unstoppable,
        DoubleKill,
        MultiKill
    }
}
