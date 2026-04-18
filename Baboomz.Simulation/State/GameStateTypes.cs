namespace Baboomz.Simulation
{
    public struct WeaponSlotState
    {
        public string WeaponId;
        public int Ammo;            // -1 = unlimited
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
        public int Bounces;
        public float FuseTime;
        public float Bounciness;
        public int ClusterCount;
        public bool IsAirstrike;
        public int AirstrikeCount;
        public bool IsNapalm;
        public float FireZoneDuration;
        public float FireZoneDPS;
        public bool IsDrill;
        public float DrillRange;
        public bool IsSheep;
        public bool IsFreeze;
        public bool IsSticky;
        public bool IsHitscan;
        public float ChainRange;
        public float ChainDamage;
        public bool IsBoomerang;
        public bool IsGravityBomb;
        public float PullRadius;
        public float PullForce;
        public bool IsRicochet;
        public bool IsLavaPool;
        public float LavaMeltRadius;
        public bool IsWindBlast;
        public bool IsPiercing;
        public int MaxPierceCount;
        public bool IsFlak;
        public float FlakMinDist;
        public float FlakMaxDist;
    }

    public struct ExplosionEvent
    {
        public Vec2 Position;
        public float Radius;
        public int OwnerIndex;
    }

    public struct DamageEvent
    {
        public int TargetIndex;
        public float Amount;
        public Vec2 Position;
        public int SourceIndex; // player who dealt the damage (-1 = environmental)
    }

    public struct MineState
    {
        public Vec2 Position;
        public float TriggerRadius;
        public float ExplosionRadius;
        public float Damage;
        public bool Active;
        public float Lifetime;  // >0 = temporary (counts down), <=0 = permanent
        public int OwnerIndex;  // player who placed this mine (-1 = environment)
        public float PlacedTime; // state.Time when this mine was placed (oldest-first overflow eviction)
        public bool IsHoming;
        public float DetectionRange;
        public float MoveSpeed;
        public float ActivationDelay; // >0 = dormant (counts down before arming)
    }

    public struct BarrelState
    {
        public Vec2 Position;
        public float ExplosionRadius;
        public float Damage;
        public bool Active;
        public int OwnerIndex;
    }

    public struct FireZoneState
    {
        public Vec2 Position;
        public float Radius;
        public float DamagePerSecond;
        public float RemainingTime;
        public int OwnerIndex;
        public bool Active;
        public string SourceWeaponId; // weapon that created this zone (for mastery tracking)
        public bool MeltsTerrain;    // true = lava pool that erodes terrain each tick
        public float MeltRadius;     // radius of terrain melt area
        public float MeltTimer;      // accumulator for melt interval (0.5s)
        public float DamageEventTimer; // throttle DamageEvent emission (0.5s intervals)
    }

    public struct SmokeZone
    {
        public Vec2 Position;
        public float Radius;
        public float RemainingTime;
    }

    public struct CrateState
    {
        public Vec2 Position;
        public Vec2 Velocity;       // falling crates have velocity
        public CrateType Type;
        public bool Active;
        public bool Grounded;
    }

    public struct SplashEvent
    {
        public Vec2 Position;
        public float Size; // 0-1 scale
    }

    public struct SkillSlotState
    {
        public string SkillId;
        public SkillType Type;
        public float EnergyCost;
        public float Cooldown;
        public float CooldownRemaining;
        public float Duration;
        public float DurationRemaining;
        public float Range;
        public float Value;
        public float Value2;
        public bool IsActive;
    }

    public struct SkillEvent
    {
        public int PlayerIndex;
        public SkillType Type;
        public Vec2 Position;
        public Vec2 TargetPosition;
    }

    public struct EmoteEvent
    {
        public int PlayerIndex;
        public EmoteType Emote;
    }

    public struct HitscanEvent
    {
        public Vec2 Origin;
        public Vec2 HitPoint;
        public int PrimaryTargetIndex;   // -1 = hit terrain only
        public int ChainTargetIndex;     // -1 = no chain
        public Vec2 ChainHitPoint;
    }

    public struct BiomeHazardState
    {
        public Vec2 Position;
        public float Radius;
        public BiomeHazardType Type;
        public bool Active;
    }

    public struct EnergyDrainEvent
    {
        public int CasterIndex;
        public int TargetIndex;
        public float AmountDrained;
    }

    public struct ArmsRaceState
    {
        public int[] CurrentWeaponIndex; // per player: which weapon they must use next
        public bool DisableCrates;       // true = crate spawning suppressed for this mode
        public bool DisableSuddenDeath;  // true = sudden death suppressed for this mode
    }

    public struct CrystalState
    {
        public float HP;
        public float MaxHP;
        public Vec2 Position;
        public int TeamIndex;       // 0 = player 1's crystal, 1 = player 2's crystal
    }

    public struct DemolitionState
    {
        public CrystalState[] Crystals;      // one per team (index 0 = P1's, index 1 = P2's)
        public int[] LivesRemaining;         // respawn lives per player
        public float[] RespawnTimers;        // >0 = waiting to respawn
        public float RespawnDelay;           // seconds before respawn
    }

    public struct CrystalDamageEvent
    {
        public int CrystalIndex;
        public float Amount;
        public Vec2 Position;
    }

    public struct SurvivalState
    {
        public int WaveNumber;          // current wave (1-based)
        public int Score;
        public float BreakTimer;        // >0 = between waves (counts down)
        public bool WaveActive;         // true = mobs alive, false = break or pre-start
        public int MobsAliveCount;      // tracked each tick
        public int MobsSpawnedTotal;    // total mobs spawned this wave (including boss)
        public float HighScore;         // best score (loaded from config)

        public SurvivalModifier ActiveModifier;
        public float SavedGravity;
        public float SavedWindForce;
        public float SavedWindAngle;
    }

    public struct TargetState
    {
        public Vec2 Position;
        public TargetType Type;
        public int Points;
        public float RespawnTimer;  // >0 = waiting to respawn
        public bool Active;
        public float MovePhase;     // oscillation phase for moving targets
        public float SpawnY;        // initial Y at spawn — vertical movers bob around this
    }

    public struct TargetHitEvent
    {
        public int TargetIndex;
        public int Points;
        public Vec2 Position;
    }

    public struct ComboEvent
    {
        public int PlayerIndex;
        public ComboType Type;
        public float Time;
    }
}
