namespace Baboomz.Simulation
{
    public struct ProjectileState
    {
        public int Id;
        public Vec2 Position;
        public Vec2 Velocity;
        public int OwnerIndex;
        public float ExplosionRadius;
        public float MaxDamage;
        public float KnockbackForce;
        public bool DestroysIndestructible;
        public bool Alive;
        public int BouncesRemaining;
        public float FuseTimer;
        public int ClusterCount;           // >0 = spawns N sub-projectiles on impact
        public bool IsAirstrike;           // true = on terrain hit, spawn rain of projectiles from above
        public int AirstrikeCount;         // number of bombs to drop
        public bool IsNapalm;             // true = on terrain hit, create fire zone
        public float FireZoneDuration;
        public float FireZoneDPS;
        public bool IsDrill;              // true = tunnels through terrain, no gravity
        public float DrillRange;          // max travel distance before drill expires (0 = fallback to 30f)
        public bool IsSheep;              // true = walks along terrain surface
        public bool IsFreeze;             // true = freezes targets instead of damaging
        public bool IsSticky;             // true = sticks to terrain or player on contact
        public int StuckToPlayerId;       // player index this projectile is stuck to (-1 = not stuck)
        public bool StuckToTerrain;       // true = stuck to terrain surface
        public bool IsBoomerang;          // true = returns to thrower after apex
        public bool IsReturning;          // true = boomerang is on return trip
        public bool HasAscended;          // true = boomerang had upward velocity at some point
        public bool IsGravityBomb;        // true = vortex pull while stuck, then explode
        public float PullRadius;          // radius of gravity vortex pull
        public float PullForce;           // pull speed toward center (units/sec)
        public bool IsRicochet;           // true = reflects off terrain with damage per bounce
        public bool IsLavaPool;           // true = creates lava pool that melts terrain
        public float LavaMeltRadius;      // radius of terrain melt area
        public bool IsWindBlast;          // true = no terrain destruction, no explosion, knockback only
        public float TravelDistance;       // accumulated travel distance (used by boomerang min-travel fallback and drill range cap)
        public bool IsPiercing;           // true = passes through first player hit
        public int MaxPierceCount;        // max players to pierce before stopping
        public int PierceCount;           // how many players have been pierced so far
        public int LastPiercedPlayerId;   // last player pierced (-1 = none)
        public bool IsFlak;               // true = detonates at burst distance, rains fragments downward
        public float FlakBurstDistance;   // distance from launch to detonate
        public Vec2 LaunchPosition;       // where this projectile was fired from
        public string SourceWeaponId;      // weapon that created this projectile (for mastery tracking)
    }
}
