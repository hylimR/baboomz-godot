namespace Baboomz.Simulation
{
    public struct KothState
    {
        public Vec2 ZonePosition;
        public float ZoneRadius;
        public float[] Scores;
        public float RelocateTimer;
        public bool IsContested;
        public float RelocateWarningTimer; // >0 means zone is about to move
    }

    public struct TerritoryState
    {
        public Vec2[] ZonePositions;  // 3 zones
        public float ZoneRadius;
        public float[] TeamScores;    // per-team scores
        public int[] ZoneOwner;       // -1 = neutral, 0+ = team index
        public bool[] ZoneContested;  // true when both teams occupy
    }

    public struct PayloadState
    {
        public Vec2 Position;
        public float VelocityX;
        public float GoalLeftX;       // player 1's goal (payload crossing this = player 2 wins)
        public float GoalRightX;      // player 2's goal (payload crossing this = player 1 wins)
        public float StalemateTimer;  // time payload has been nearly stationary
        public float Friction;        // current friction (can reduce during stalemate)
        public float MatchTimer;      // countdown to time limit
    }

    public struct FlagState
    {
        public Vec2 HomePosition;     // flag's base location
        public Vec2 Position;         // current world position (home, on carrier, or dropped)
        public int TeamIndex;         // which team owns this flag (0 or 1)
        public int CarrierIndex;      // -1 = not carried, else playerIndex
        public float DropTimer;       // >0 = flag dropped, counts down to auto-return
        public bool IsHome;           // true = at home base
    }

    public struct CtfState
    {
        public FlagState[] Flags;     // one per team (index 0 = team 0's flag, index 1 = team 1's flag)
        public int[] Captures;        // captures per team
    }

    public struct FlagEvent
    {
        public int PlayerIndex;       // player involved
        public int FlagTeamIndex;     // which team's flag
        public FlagEventType Type;
    }

    public enum FlagEventType
    {
        Pickup,
        Drop,
        Return,
        Capture
    }

    public struct TokenPickup
    {
        public Vec2 Position;
        public bool Active;
    }

    public struct HeadhunterState
    {
        public int[] TokensCollected;     // per-player token count
        public float[] RespawnTimers;     // >0 = waiting to respawn
        public TokenPickup[] Tokens;      // dropped token pickups on the map
        public int TokenCount;            // number of active tokens in Tokens[]
    }

    public struct TokenCollectEvent
    {
        public int PlayerIndex;
        public Vec2 Position;
    }
}
