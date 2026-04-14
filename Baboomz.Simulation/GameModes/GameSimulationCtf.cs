using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Capture the Flag mode logic: flag placement, pickup, drop, return,
    /// capture scoring, carrier speed penalty, and win condition.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitCtf(GameState state, Random rng)
        {
            var config = state.Config;

            // Place flags near each team's spawn (offset behind spawn point)
            float flag0X = config.Player1SpawnX - 5f;
            float flag1X = config.Player2SpawnX + 5f;
            float flag0Y = GamePhysics.FindGroundY(state.Terrain, flag0X, config.SpawnProbeY, 0.5f);
            float flag1Y = GamePhysics.FindGroundY(state.Terrain, flag1X, config.SpawnProbeY, 0.5f);

            var home0 = new Vec2(flag0X, flag0Y + 1f);
            var home1 = new Vec2(flag1X, flag1Y + 1f);

            state.Ctf = new CtfState
            {
                Flags = new[]
                {
                    new FlagState
                    {
                        HomePosition = home0,
                        Position = home0,
                        TeamIndex = 0,
                        CarrierIndex = -1,
                        DropTimer = 0f,
                        IsHome = true
                    },
                    new FlagState
                    {
                        HomePosition = home1,
                        Position = home1,
                        TeamIndex = 1,
                        CarrierIndex = -1,
                        DropTimer = 0f,
                        IsHome = true
                    }
                },
                Captures = new int[2]
            };
        }

        static void UpdateCtf(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.CaptureTheFlag) return;

            ref CtfState ctf = ref state.Ctf;
            var config = state.Config;
            float pickupR = config.CtfFlagPickupRadius;

            for (int f = 0; f < ctf.Flags.Length; f++)
            {
                ref FlagState flag = ref ctf.Flags[f];

                // Update carried flag position to follow carrier
                if (flag.CarrierIndex >= 0)
                {
                    ref PlayerState carrier = ref state.Players[flag.CarrierIndex];
                    flag.Position = new Vec2(carrier.Position.x, carrier.Position.y + 1.5f);
                    continue;
                }

                // Dropped flag: count down to auto-return
                if (!flag.IsHome && flag.DropTimer > 0f)
                {
                    flag.DropTimer -= dt;
                    if (flag.DropTimer <= 0f)
                    {
                        ReturnFlag(state, f);
                        continue;
                    }
                }

                // Check flag interactions with players
                for (int i = 0; i < state.Players.Length; i++)
                {
                    ref PlayerState p = ref state.Players[i];
                    if (p.IsDead) continue;

                    float dist = Vec2.Distance(p.Position, flag.Position);
                    if (dist > pickupR) continue;

                    int playerTeam = GetCtfTeam(state, i);

                    if (playerTeam == flag.TeamIndex)
                    {
                        // Own flag: return it if not at home
                        if (!flag.IsHome)
                        {
                            ReturnFlag(state, f);
                            state.FlagEvents.Add(new FlagEvent
                            {
                                PlayerIndex = i,
                                FlagTeamIndex = f,
                                Type = FlagEventType.Return
                            });
                        }
                        else
                        {
                            // Own flag at home + carrying enemy flag = capture
                            int enemyFlagIdx = flag.TeamIndex == 0 ? 1 : 0;
                            ref FlagState enemyFlag = ref ctf.Flags[enemyFlagIdx];
                            if (enemyFlag.CarrierIndex == i)
                            {
                                CaptureFlag(state, i, enemyFlagIdx);
                            }
                        }
                    }
                    else
                    {
                        // Enemy flag: pick it up (if not carried)
                        if (flag.CarrierIndex < 0)
                        {
                            PickupFlag(state, i, f);
                        }
                    }
                }
            }

            // Apply carrier speed penalty
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.IsDead) continue;

                bool carrying = IsCarryingFlag(state, i);
                // Set carrier speed as a flat value each frame (no compounding).
                // Compute the effective base speed from DefaultMoveSpeed + WarCry buff,
                // then apply the carrier multiplier once.
                if (carrying)
                {
                    float baseSpeed = config.DefaultMoveSpeed;
                    if (p.WarCryTimer > 0f && p.WarCrySpeedBuff > 0f)
                        baseSpeed *= p.WarCrySpeedBuff;
                    p.MoveSpeed = baseSpeed * config.CtfCarrierSpeedMult;
                }
                else if (p.MoveSpeed < config.DefaultMoveSpeed && p.WarCryTimer <= 0f)
                {
                    p.MoveSpeed = config.DefaultMoveSpeed;
                }
            }

            CheckCtfEnd(state);
        }

        static int GetCtfTeam(GameState state, int playerIndex)
        {
            // In team mode, use TeamIndex; in FFA (2-player), P0 = team 0, P1 = team 1
            if (state.Config.TeamMode)
                return state.Players[playerIndex].TeamIndex;
            return playerIndex < state.Players.Length / 2 ? 0 : 1;
        }

        static bool IsCarryingFlag(GameState state, int playerIndex)
        {
            ref CtfState ctf = ref state.Ctf;
            for (int f = 0; f < ctf.Flags.Length; f++)
            {
                if (ctf.Flags[f].CarrierIndex == playerIndex) return true;
            }
            return false;
        }

        static void PickupFlag(GameState state, int playerIndex, int flagIndex)
        {
            ref FlagState flag = ref state.Ctf.Flags[flagIndex];
            flag.CarrierIndex = playerIndex;
            flag.IsHome = false;
            flag.DropTimer = 0f;

            state.FlagEvents.Add(new FlagEvent
            {
                PlayerIndex = playerIndex,
                FlagTeamIndex = flagIndex,
                Type = FlagEventType.Pickup
            });
        }

        static void CaptureFlag(GameState state, int playerIndex, int flagIndex)
        {
            ref CtfState ctf = ref state.Ctf;
            ref FlagState flag = ref ctf.Flags[flagIndex];

            int capturingTeam = GetCtfTeam(state, playerIndex);
            ctf.Captures[capturingTeam]++;

            // Return captured flag to its home
            ReturnFlag(state, flagIndex);

            state.FlagEvents.Add(new FlagEvent
            {
                PlayerIndex = playerIndex,
                FlagTeamIndex = flagIndex,
                Type = FlagEventType.Capture
            });
        }

        static void ReturnFlag(GameState state, int flagIndex)
        {
            ref FlagState flag = ref state.Ctf.Flags[flagIndex];
            flag.Position = flag.HomePosition;
            flag.CarrierIndex = -1;
            flag.DropTimer = 0f;
            flag.IsHome = true;
        }

        /// <summary>
        /// Drop the flag when a carrier dies. Called from death-handling code.
        /// </summary>
        public static void DropCtfFlag(GameState state, int playerIndex)
        {
            if (state.Config.MatchType != MatchType.CaptureTheFlag) return;

            ref CtfState ctf = ref state.Ctf;
            for (int f = 0; f < ctf.Flags.Length; f++)
            {
                if (ctf.Flags[f].CarrierIndex != playerIndex) continue;

                ctf.Flags[f].Position = state.Players[playerIndex].Position;
                ctf.Flags[f].CarrierIndex = -1;
                ctf.Flags[f].DropTimer = state.Config.CtfFlagDropTime;

                state.FlagEvents.Add(new FlagEvent
                {
                    PlayerIndex = playerIndex,
                    FlagTeamIndex = f,
                    Type = FlagEventType.Drop
                });
            }
        }

        static void CheckCtfEnd(GameState state)
        {
            ref CtfState ctf = ref state.Ctf;
            int target = state.Config.CtfCapturesToWin;

            for (int t = 0; t < ctf.Captures.Length; t++)
            {
                if (ctf.Captures[t] >= target)
                {
                    state.Phase = MatchPhase.Ended;
                    // Winner is a player on the capturing team
                    for (int i = 0; i < state.Players.Length; i++)
                    {
                        if (GetCtfTeam(state, i) == t)
                        {
                            state.WinnerIndex = i;
                            break;
                        }
                    }
                    return;
                }
            }
        }
    }
}
