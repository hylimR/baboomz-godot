using System.Collections.Generic;

namespace Baboomz.Simulation
{
    public struct ReplayFrame
    {
        public float DeltaTime;
        public InputState Input;
    }

    public class ReplayData
    {
        public int Seed;
        public GameConfig Config;
        public List<ReplayFrame> Frames;

        public ReplayData()
        {
            Frames = new List<ReplayFrame>();
        }
    }

    /// <summary>
    /// Records and replays matches using the deterministic simulation.
    /// Recording captures InputState + dt each tick.
    /// Playback recreates the match from seed + config + recorded inputs.
    /// </summary>
    public static class ReplaySystem
    {
        /// <summary>Start recording a match. Call before first Tick.</summary>
        public static ReplayData StartRecording(GameState state)
        {
            var data = new ReplayData
            {
                Seed = state.Seed,
                Config = state.Config
            };
            state.ReplayRecording = data;
            return data;
        }

        /// <summary>Stop recording and return the replay data.</summary>
        public static ReplayData StopRecording(GameState state)
        {
            var data = state.ReplayRecording;
            state.ReplayRecording = null;
            return data;
        }

        /// <summary>Record one frame. Called automatically from Tick when recording is active.</summary>
        public static void RecordFrame(GameState state, float dt)
        {
            if (state.ReplayRecording == null) return;
            state.ReplayRecording.Frames.Add(new ReplayFrame
            {
                DeltaTime = dt,
                Input = state.Input
            });
        }

        /// <summary>
        /// Replay a recorded match. Returns the final GameState after all frames.
        /// Creates a fresh match from the replay's seed + config, then feeds
        /// recorded inputs through Tick.
        /// </summary>
        public static GameState Replay(ReplayData replay)
        {
            var state = GameSimulation.CreateMatch(replay.Config, replay.Seed);

            // Disable recording during playback to avoid re-recording
            state.ReplayRecording = null;

            foreach (var frame in replay.Frames)
            {
                state.Input = frame.Input;
                GameSimulation.Tick(state, frame.DeltaTime);
            }

            return state;
        }
    }
}
