using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Step-by-step playback controller for a recorded match.
    /// Pure simulation layer — no Unity dependencies.
    ///
    /// Usage:
    ///   var player = new ReplayPlayer(replayData);
    ///   while (!player.IsFinished)
    ///       player.Step();   // advance one recorded frame
    /// </summary>
    public class ReplayPlayer
    {
        private readonly ReplayData data;

        /// <summary>Current playback GameState (read-only for renderers).</summary>
        public GameState State { get; private set; }

        /// <summary>Index of the next frame to apply (0-based).</summary>
        public int FrameIndex { get; private set; }

        /// <summary>Total number of recorded frames.</summary>
        public int TotalFrames => data?.Frames?.Count ?? 0;

        /// <summary>True when all frames have been played back.</summary>
        public bool IsFinished => FrameIndex >= TotalFrames;

        /// <summary>Playback speed multiplier (0.5 = half-speed, 4.0 = 4x speed).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>True if playback is paused.</summary>
        public bool IsPaused { get; private set; }

        private float timeAccumulator;

        public ReplayPlayer(ReplayData replayData)
        {
            data = replayData ?? throw new ArgumentNullException(nameof(replayData));
            State = GameSimulation.CreateMatch(data.Config, data.Seed);
            // Disable recording during playback
            State.ReplayRecording = null;
            FrameIndex = 0;
            timeAccumulator = 0f;
        }

        /// <summary>
        /// Advance playback by <paramref name="realDeltaTime"/> seconds.
        /// Applies as many recorded frames as needed, scaled by Speed.
        /// Call this from Unity Update().
        /// </summary>
        public void Tick(float realDeltaTime)
        {
            if (IsPaused || IsFinished) return;

            timeAccumulator += realDeltaTime * Speed;

            while (!IsFinished && timeAccumulator > 0f)
            {
                var frame = data.Frames[FrameIndex];
                if (timeAccumulator < frame.DeltaTime) break;

                timeAccumulator -= frame.DeltaTime;
                Step();
            }
        }

        /// <summary>Apply exactly one recorded frame.</summary>
        public void Step()
        {
            if (IsFinished) return;
            var frame = data.Frames[FrameIndex];
            State.Input = frame.Input;
            GameSimulation.Tick(State, frame.DeltaTime);
            FrameIndex++;
        }

        /// <summary>Seek to a specific frame index (recreates state from scratch).</summary>
        public void SeekTo(int targetFrame)
        {
            targetFrame = Math.Max(0, Math.Min(targetFrame, TotalFrames));
            // Recreate from beginning — deterministic simulation guarantees correctness
            State = GameSimulation.CreateMatch(data.Config, data.Seed);
            State.ReplayRecording = null;
            FrameIndex = 0;
            timeAccumulator = 0f;
            while (FrameIndex < targetFrame)
                Step();
        }

        /// <summary>Seek to the frame just before the next kill event.</summary>
        public void SeekToNextKill()
        {
            // Find a frame where a player that was alive becomes dead
            int current = FrameIndex;
            bool[] wasAlive = new bool[State.Players.Length];
            for (int i = 0; i < wasAlive.Length; i++)
                wasAlive[i] = !State.Players[i].IsDead;

            while (!IsFinished)
            {
                Step();
                for (int i = 0; i < State.Players.Length; i++)
                {
                    if (wasAlive[i] && State.Players[i].IsDead)
                        return; // found next kill
                }
                for (int i = 0; i < wasAlive.Length; i++)
                    wasAlive[i] = !State.Players[i].IsDead;
            }
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        public void TogglePause() => IsPaused = !IsPaused;
    }
}
