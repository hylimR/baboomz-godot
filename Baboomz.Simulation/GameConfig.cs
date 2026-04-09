namespace Baboomz.Simulation
{
    public partial class GameConfig
    {
        // Match
        public MatchType MatchType;                // Deathmatch (default) or KingOfTheHill
        public bool TeamMode;                      // true = team-based win condition
        public float DeathBoundaryY = -30f;
        public float Gravity = 9.81f;
        public float WindChangeInterval = 10f;
        public float MaxWindStrength = 3f;
        public float MapWidth = 100f;
        public int UnlockedTier = 4;               // weapon/skill unlock tier (0-4, default = all unlocked)
        public MasteryTier[] WeaponMasteryTiers;   // per-weapon-slot mastery tier (null = no mastery bonuses)
        /// <summary>Indices into Weapons[] for the player loadout (null = all weapons). Must have exactly 4 entries when set.</summary>
        public int[] PlayerWeaponLoadout;
        /// <summary>Indices into Weapons[] for the AI weapon loadout (null = all weapons). Set by CreateMatch using AILogic.PickWeaponLoadout.</summary>
        public int[] AIWeaponLoadout;

        // Player defaults
        public float DefaultMaxHealth = 100f;
        public float DefaultMaxEnergy = 100f;
        public float DefaultEnergyRegen = 10f;
        public float DefaultMoveSpeed = 5f;
        public float DefaultJumpForce = 10f;
        public float DefaultDamageMultiplier = 1f;
        public float DefaultArmorMultiplier = 1f;
        public float DefaultCooldownMultiplier = 1f;

        // Aim defaults
        public float DefaultMinPower = 10f;
        public float DefaultMaxPower = 30f;
        public float DefaultChargeTime = 2f;
        public float DefaultShootCooldown = 2f;
        public float AimAngleSpeed = 90f;

        // Weapon array and DefaultWeapon in GameConfigWeapons.cs (partial class)

        // Terrain
        public int TerrainWidth = 4096;
        public int TerrainHeight = 1024;
        public float TerrainPPU = 16f;
        public float TerrainMinHeight = -5f;
        public float TerrainMaxHeight = 15f;
        public float TerrainHillFrequency = 0.1f;
        public float TerrainFloorDepth = -20f;

        public float Player1SpawnX = -15f;
        public float Player2SpawnX = 15f;
        public float SpawnProbeY = 30f;
        public string Player1Name = "Player1";
        public string Player2Name = "CPU";

        public float AIAimErrorMargin = 5f;
        public float AIMoveInterval = 3f;
        public float AIMoveDuration = 0.5f;
        public float AIShootInterval = 4f;
        public float AIShootIntervalRandomness = 2f;
        /// <summary>0 = Easy, 1 = Normal, 2 = Hard. Used by AI loadout selection.</summary>
        public int AIDifficultyLevel = 1;

        public float DamageMinRadius = 0.5f;
        // Fall damage
        public float FallDamageMinDistance = 5f;   // no damage below this fall distance
        public float FallDamagePerMeter = 8f;
        public float FallDamageMax = 50f;

        public float SuddenDeathTime = 90f;  // 0 = disabled
        public float WaterRiseSpeed = 1f;

        // Swimming (water contact)
        public float SwimSpeedMultiplier = 0.4f;  // movement speed while swimming (40%)
        public float SwimDuration = 3f;            // seconds before drowning
        public float SwimSinkSpeed = 0.5f;         // units/sec player sinks while swimming

        public float CrateSpawnInterval = 20f;  // 0 = disabled
        public float CrateCollectRadius = 1.5f;   // proximity to collect a crate
        public float CrateHealthAmount = 25f;      // HP restored by health crate
        public float CrateEnergyAmount = 50f;      // energy restored by energy crate
        public float CrateDoubleDamageDuration = 10f; // seconds of 2x damage

        public float RetreatDuration = 0f;  // 0 = disabled

        // King of the Hill
        public float KothZoneRadius = 4f;
        public float KothPointsPerSecond = 5f;
        public float KothPointsToWin = 100f;
        public float KothRelocateInterval = 30f;    // zone moves every N seconds
        public float KothRelocateWarning = 3f;      // warning flash before relocation

        // Target Practice
        public float TargetPracticeRoundDuration = 60f;
        public float TargetRadius = 1.5f;
        public float TargetRespawnTime = 3f;
        public int TargetStaticNearCount = 2;
        public int TargetStaticMidCount = 2;
        public int TargetStaticFarCount = 1;
        public int TargetMovingHorizontalCount = 1;
        public int TargetMovingVerticalCount = 1;
        public int TargetNearPoints = 50;
        public int TargetMidPoints = 100;
        public int TargetFarPoints = 200;
        public int TargetMovingPoints = 150;
        public float TargetMoveSpeedH = 3f;
        public float TargetMoveSpeedV = 2f;
        public float TargetMoveAmplitude = 5f;
        public float TargetNearMaxDist = 10f;
        public float TargetMidMaxDist = 20f;
        // Scoring bonuses
        public int TargetStreakBonus = 50;          // +50 per hit after 3 consecutive
        public int TargetStreakThreshold = 3;
        public int TargetLongRangeBonus = 100;      // hit > 25 units away
        public float TargetLongRangeDistance = 25f;
        public int TargetSpeedBonus = 25;           // hit within 1s of last hit
        public float TargetSpeedBonusWindow = 1f;

        // Biome weather modifiers (defaults = no effect)
        public float TerrainDestructionMult = 1f;
        public float MoveSpeedMult = 1f;
        public float KnockbackMult = 1f;
        public float FireZoneDurationMult = 1f;

        // Biome baseline (saved on first BiomeModifiers.Apply, restored on subsequent calls)
        public bool BiomeBaselineSaved;
        public float BaseTerrainDestructionMult;
        public float BaseWindChangeInterval;
        public float BaseMaxWindStrength;
        public float BaseMoveSpeedMult;
        public float BaseFallDamagePerMeter;
        public float BaseFireZoneDurationMult;
        public float BaseCrateSpawnInterval;
        public float BaseDefaultEnergyRegen;
        public float BaseKnockbackMult;
        public float BaseGravity;
        public float BaseDefaultCooldownMultiplier;
        public float BaseWaterRiseSpeed;

        // Oil barrels
        public int BarrelCount = 2;               // barrels per map (0 = disabled)
        public float BarrelDamage = 40f;
        public float BarrelExplosionRadius = 3.5f;

        // Mines
        public int MineCount = 3;
        public float MineTriggerRadius = 1.5f;
        public float MineExplosionRadius = 3f;
        public float MineDamage = 45f;

        // Skills array and DefaultSkillSlot0/1 in GameConfigSkills.cs (partial class)

        // Demolition mode
        public float DemolitionCrystalHP = 300f;           // HP per crystal
        public float DemolitionCrystalWidth = 3f;          // hitbox width (units)
        public float DemolitionCrystalHeight = 5f;         // hitbox height (units)
        public float DemolitionCrystalOffset = 10f;        // units behind spawn point
        public int DemolitionLivesPerPlayer = 3;           // respawn lives
        public float DemolitionRespawnDelay = 3f;          // seconds before respawn

        // Payload mode
        public float PayloadPushMult = 0.5f;          // multiplier on KnockbackForce for payload push
        public float PayloadPushRadiusMult = 1.5f;    // explosion radius × this = push detection radius
        public float PayloadFriction = 0.8f;           // velocity deceleration per second
        public float PayloadMatchTime = 120f;          // match duration (seconds)
        public float PayloadStalemateTime = 30f;       // seconds of near-zero movement before friction halves
        public float PayloadStalemateThreshold = 0.1f; // velocity below this = "stalled"
        public float PayloadRespawnDelay = 3f;         // seconds before respawn after death
        public int PayloadLivesPerPlayer = -1;         // -1 = unlimited

        // Arms Race
        public float ArmsRaceMaxTime = 180f;         // timer-based tiebreaker (seconds)
        public float ArmsRaceGustMinDamage = 1f;     // min damage for gust cannon so it can advance

        // Capture the Flag
        public int CtfCapturesToWin = 3;           // first to N captures wins
        public float CtfFlagDropTime = 10f;         // seconds before dropped flag returns home
        public float CtfFlagPickupRadius = 2f;      // proximity to pick up / capture flag
        public float CtfCarrierSpeedMult = 0.7f;    // movement speed penalty while carrying flag

        // Headhunter mode
        public int HeadhunterTokensOnDeath = 3;              // tokens dropped when a player dies
        public int HeadhunterTokensToWin = 10;               // first to N tokens wins
        public float HeadhunterRespawnDelay = 5f;            // seconds before respawn
        public float HeadhunterRespawnHealthFraction = 0.5f; // respawn HP = max * this
        public float HeadhunterTokenCollectRadius = 1.5f;    // proximity to collect token

        // Territories mode
        public float TerritoryZoneRadius = 12f;       // smaller than KOTH's 20 for tighter control
        public float TerritoryPointsPerSecond = 1f;   // per zone per second
        public float TerritoryPointsToWin = 300f;     // first team to this score wins
        public int TerritoryZoneCount = 3;             // number of capture zones

        // Survival mode
        public float SurvivalBreakDuration = 5f;       // seconds between waves
        public float SurvivalHealthRegen = 20f;         // HP restored between waves
        public int SurvivalWaveMobBase = 2;             // starting mob count (wave 1)
        public int SurvivalBossInterval = 5;            // boss every N waves
        public int SurvivalScorePerWave = 100;          // base score per wave cleared (multiplied by wave #)
        public int SurvivalScorePerKill = 50;
        public int SurvivalScorePerBossKill = 500;
        public int SurvivalScoreDirectHitBonus = 25;
        public int SurvivalScoreNoDamageBonus = 200;

        // Gamepad
        public float GamepadDeadzone = 0.15f;        // stick deadzone (0-1)
        public float GamepadAimSensitivity = 1.5f;   // multiplier on right-stick aim delta

        // Mob defaults
        public MobDef[] MobTypes = new[]
        {
            new MobDef { MobType = "turret", Health = 50f, Speed = 0f, Damage = 20f, BehaviorType = MobBehavior.Turret },
            new MobDef { MobType = "walker", Health = 30f, Speed = 3f, Damage = 15f, BehaviorType = MobBehavior.Walker }
        };
    }

    public struct WeaponDef
    {
        public string WeaponId;
        public float MinPower;
        public float MaxPower;
        public float ChargeTime;
        public float ShootCooldown;
        public float ExplosionRadius;
        public float MaxDamage;
        public float KnockbackForce;
        public int ProjectileCount;
        public float SpreadAngle;
        public bool DestroysIndestructible;
        public float EnergyCost;
        public int Ammo; // -1 = unlimited
        public int Bounces;
        public float FuseTime;
        public float Bounciness;
        public int ClusterCount; // >0 = spawns N sub-projectiles on impact
        public bool IsAirstrike; // fires marker; on hit, rains bombs from above
        public int AirstrikeCount; // bombs dropped (default 5)
        public bool IsNapalm;      // on terrain hit, creates lingering fire zone
        public float FireZoneDuration; // seconds fire zone lasts
        public float FireZoneDPS;      // damage per second in fire zone
        public bool IsDrill;       // tunnels through terrain, no gravity
        public float DrillRange;   // max travel distance before drill expires (0 = fallback to 30f)
        public bool IsSheep;       // walks along terrain surface
        public bool IsFreeze;      // freezes targets instead of damaging
        public bool IsSticky;      // sticks to terrain or player on contact
        public bool IsHitscan;     // instant-hit ray, no projectile
        public float ChainRange;   // max distance to chain to secondary target
        public float ChainDamage;  // damage dealt to chain target
        public bool IsBoomerang;   // returns to thrower after apex
        public bool IsGravityBomb; // vortex pull + delayed explosion
        public float PullRadius;   // radius of gravity vortex pull
        public float PullForce;    // pull speed toward center (units/sec)
        public bool IsRicochet;    // reflects off terrain with damage per bounce
        public bool IsLavaPool;    // creates fire zone that melts terrain over time
        public float LavaMeltRadius; // radius of terrain melt (can differ from explosion radius)
        public bool IsWindBlast;   // no terrain destruction, no explosion VFX, knockback only
        public bool IsPiercing;    // passes through first player hit, continues traveling
        public int MaxPierceCount; // max players to pierce before stopping (0 = no pierce)
        public bool IsFlak;        // detonates mid-air at charge-determined distance, rains fragments downward
        public float FlakMinDist;  // burst distance at 0% charge
        public float FlakMaxDist;  // burst distance at 100% charge
    }

    public struct SkillDef
    {
        public string SkillId;
        public SkillType Type;
        public float EnergyCost;
        public float Cooldown;
        public float Duration;  // 0 = instant
        public float Range;
        public float Value;     // skill-specific meaning
    }

    public struct MobDef
    {
        public string MobType;
        public float Health;
        public float Speed;
        public float Damage;
        public MobBehavior BehaviorType;
        public float ShieldHP;       // Shielder: frontal shield HP (absorbs damage before health)
        public float HealRate;       // Healer: HP/s healed on nearest ally
        public float HoverHeight;    // Flyer: units above ground
        public float AttackRange;    // range at which mob attacks (0 = use default per behavior)
        public float DetectionRange; // range at which mob detects player (0 = use default 40)
    }

    public enum MobBehavior
    {
        Turret,
        Walker,
        Bomber,    // lobs bouncing grenades from medium range
        Shielder,  // frontal shield, slow melee advance
        Flyer,     // hovers above terrain, rapid weak shots
        Healer,    // heals nearest ally, no attacks, flees player
        Boss       // behavior determined by BossType string on PlayerState
    }
}
