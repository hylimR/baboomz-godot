using System.Collections.Generic;

namespace Baboomz.Simulation
{
    public enum MatchPhase
    {
        Waiting,
        Playing,
        Ended
    }

    public class GameState
    {
        public MatchPhase Phase;
        public float Time;
        public float WindForce;        // horizontal force applied to projectiles
        public float WindAngle;        // degrees (for HUD display)
        public PlayerState[] Players;
        public List<ProjectileState> Projectiles;
        public List<ExplosionEvent> ExplosionEvents;
        public List<DamageEvent> DamageEvents;
        public List<SplashEvent> SplashEvents;
        public List<SkillEvent> SkillEvents;
        public TerrainState Terrain;
        public List<MineState> Mines;
        public InputState[] PlayerInputs;
        public ref InputState Input => ref PlayerInputs[0];
        public GameConfig Config;
        public int WinnerIndex;        // -1 = no winner / draw
        public int FirstBloodPlayerIndex; // index of player who dealt first damage (-1 = none yet)
        public int WinnerTeamIndex;    // -1 = no team winner (used in team mode)
        public int NextProjectileId;
        public float NextWindChangeTime;
        public int Seed;
        public TerrainBiome Biome;
        public float WaterLevel;               // current water Y position (rises during sudden death)
        public bool SuddenDeathActive;         // true once water starts rising
        public List<CrateState> Crates;
        public float NextCrateSpawnTime;
        public List<BarrelState> Barrels;
        public int BarrelDetonationsThisTick;
        public List<FireZoneState> FireZones;
        public List<SmokeZone> SmokeZones;
        public List<EmoteEvent> EmoteEvents;
        public List<HitscanEvent> HitscanEvents;
        public List<BiomeHazardState> BiomeHazards;
        public List<EnergyDrainEvent> EnergyDrainEvents;
        public List<AchievementEvent> AchievementEvents;
        public List<ComboEvent> ComboEvents;
        public ReplayData ReplayRecording; // non-null = recording active
        public KothState Koth;             // King of the Hill state (only used when MatchType == KingOfTheHill)
        public SurvivalState Survival;     // Survival mode state (only used when MatchType == Survival)
        public DemolitionState Demolition; // Demolition mode state (only used when MatchType == Demolition)
        public ArmsRaceState ArmsRace;     // Arms Race mode state (only used when MatchType == ArmsRace)
        public PayloadState Payload;       // Payload mode state (only used when MatchType == Payload)
        public CtfState Ctf;               // Capture the Flag state (only used when MatchType == CaptureTheFlag)
        public HeadhunterState Headhunter; // Headhunter mode state (only used when MatchType == Headhunter)
        public TerritoryState Territory;   // Territories mode state (only used when MatchType == Territories)
        public List<CrystalDamageEvent> CrystalDamageEvents;
        public List<FlagEvent> FlagEvents;
        public List<TokenCollectEvent> TokenCollectEvents;

        // Tutorial state (only used when tutorial is active)
        public TutorialState Tutorial;

        // Target Practice state (only used when MatchType == TargetPractice)
        public List<TargetState> Targets;
        public List<TargetHitEvent> TargetHitEvents;
        public int TargetScore;
        public int TargetConsecutiveHits;   // for streak bonus
        public float TargetLastHitTime;     // for speed bonus
        public float TargetTimeRemaining;   // countdown timer

        // Weapon mastery tracking (per-player, per-weapon)
        public Dictionary<string, int>[] WeaponHits;   // [playerIndex][weaponId] = hit count
        public Dictionary<string, int>[] WeaponKills;  // [playerIndex][weaponId] = kill count
        public HashSet<string>[] WeaponsUsed;           // [playerIndex] = set of weapon IDs fired
        public Dictionary<string, float>[] WeaponDamage; // [playerIndex][weaponId] = total damage
        public HashSet<SkillType>[] SkillsActivated;     // [playerIndex] = distinct skill types used

        public GameState()
        {
            Projectiles = new List<ProjectileState>();
            ExplosionEvents = new List<ExplosionEvent>();
            DamageEvents = new List<DamageEvent>();
            SplashEvents = new List<SplashEvent>();
            SkillEvents = new List<SkillEvent>();
            Mines = new List<MineState>();
            Crates = new List<CrateState>();
            Barrels = new List<BarrelState>();
            FireZones = new List<FireZoneState>();
            SmokeZones = new List<SmokeZone>();
            EmoteEvents = new List<EmoteEvent>();
            HitscanEvents = new List<HitscanEvent>();
            BiomeHazards = new List<BiomeHazardState>();
            EnergyDrainEvents = new List<EnergyDrainEvent>();
            AchievementEvents = new List<AchievementEvent>();
            ComboEvents = new List<ComboEvent>();
            Targets = new List<TargetState>();
            TargetHitEvents = new List<TargetHitEvent>();
            CrystalDamageEvents = new List<CrystalDamageEvent>();
            FlagEvents = new List<FlagEvent>();
            TokenCollectEvents = new List<TokenCollectEvent>();
            PlayerInputs = new InputState[4];
            WinnerIndex = -1;
            FirstBloodPlayerIndex = -1;
        }

        public void InitWeaponTracking(int playerCount)
        {
            WeaponHits = new Dictionary<string, int>[playerCount];
            WeaponKills = new Dictionary<string, int>[playerCount];
            WeaponsUsed = new HashSet<string>[playerCount];
            WeaponDamage = new Dictionary<string, float>[playerCount];
            SkillsActivated = new HashSet<SkillType>[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                WeaponHits[i] = new Dictionary<string, int>();
                WeaponKills[i] = new Dictionary<string, int>();
                WeaponsUsed[i] = new HashSet<string>();
                WeaponDamage[i] = new Dictionary<string, float>();
                SkillsActivated[i] = new HashSet<SkillType>();
            }
        }
    }

    public struct PlayerState
    {
        public Vec2 Position;
        public Vec2 Velocity;
        public float Health;
        public float MaxHealth;
        public float Energy;
        public float MaxEnergy;
        public float EnergyRegen;
        public float HealthRegen;
        public float MoveSpeed;
        public float JumpForce;
        public float AimAngle;
        public float AimPower;
        public float ShootCooldownRemaining;
        public float DamageMultiplier;
        public float ArmorMultiplier;
        public float CooldownMultiplier;
        public bool IsGrounded;
        public bool IsDead;
        public bool IsAI;
        public bool IsCharging;
        public int ActiveWeaponSlot;
        public int FacingDirection;    // 1 = right, -1 = left
        public string Name;
        public float LastGroundedY;    // for fall damage calculation
        public int TeamIndex;          // -1 = no team (FFA), 0+ = team number

        // Match statistics
        public float TotalDamageDealt;
        public float TotalDamageTaken;  // cumulative damage received (not reduced by healing)
        public int ShotsFired;
        public int DirectHits;
        public float MaxSingleDamage;
        // Weapon data stored per-slot for simplicity
        public WeaponSlotState[] WeaponSlots;

        public SkillSlotState[] SkillSlots;
        public Vec2 SkillTargetPosition;
        public float RopeLength;
        public float DoubleDamageTimer;
        public float OverchargeTimer;   // >0 = next shot fired will deal 2x (consumed on fire or expires in 5s)
        public float WarCryTimer;
        public float WarCrySpeedBuff; // stored speed multiplier to restore on expiry
        public float WarCryDamageBuff; // stored damage multiplier to restore on expiry
        public float RetreatTimer;
        public float FreezeTimer;
        public EmoteType ActiveEmote;
        public float EmoteTimer;
        public bool IsMob;
        public string MobType;
        public bool IsInvulnerable;
        public float ShieldHP;
        public float MaxShieldHP;
        public string BossType;
        public int BossPhase;
        public HatType Hat;
        public float FirecrackerCooldown; // per-player cooldown for Firecracker hazard zones
        public float DeflectTimer;         // >0 = deflect field active, reflects projectiles
        public int RopeHookCount;          // number of re-hooks used this activation (max 5)
        public float RopeRehookWindow;     // >0 = time remaining to re-hook after rope detach
        public float DecoyTimer;           // >0 = decoy is active (counts down)
        public Vec2 DecoyPosition;         // position where the hologram stands
        public bool IsInvisible;           // true while decoy is active (player hidden from AI)
        public bool IsSwimming;            // true while submerged below water level
        public float SwimTimer;            // seconds spent swimming (drowns at SwimDuration)

        // Kill distance tracking (for Close Quarters challenge)
        public int TotalKills;             // total enemy kills
        public int CloseRangeKills;        // kills where attacker was within 5 units of target

        // Challenge tracking stats
        public int TerrainPixelsDestroyed;  // approximate pixels cleared (for Boom Boom challenge)
        public int ChainLightningTargets;   // max chain targets hit in one hitscan (for Chain Lightning challenge)
        public bool HitWhileJetpacking;     // hit enemy while jetpack skill active (for Jetpack Ace)
        public float ShieldDamageBlocked;   // total damage absorbed by shield skill (for Shield Wall)
        public int ClusterBananaSubHits;    // cluster/banana sub-projectile direct hits (for Bombardier)
        public bool FreezeToHitCombo;       // hit a frozen enemy (for Freeze Tag)
        public bool GravityBombVoidKill;    // kill via gravity bomb pull into void (for Gravity Master)

        // Knockback kill attribution
        public int LastDamagedByIndex;     // player who last dealt damage/knockback (-1 = none)
        public float LastDamagedByTimer;   // grace window — clears after 5s of no new damage

        // Combo tracking
        public int ConsecutiveHits;        // consecutive projectile hits without a miss
        public int KillsInWindow;          // kills within the kill-streak time window
        public float LastHitTime;          // time of last damage-dealing hit
        public float LastKillTime;         // time of last kill
    }

    // WeaponSlotState, ProjectileState, event structs, enums, and hazard types
    // are in GameStateTypes.cs (same namespace)

    public struct InputState
    {
        public float MoveX;               // -1..1
        public bool JumpPressed;
        public bool FirePressed;
        public bool FireHeld;
        public bool FireReleased;
        public float AimDelta;            // W/S for angle change
        public int WeaponSlotPressed;     // 0-21, or -1 for none
        public int WeaponScrollDelta;     // +1 = next weapon, -1 = previous, 0 = none
        public bool Skill1Pressed;        // Q key
        public bool Skill2Pressed;        // E key
        public int EmotePressed;          // 1-4 = emote type, 0 = none
    }
}
