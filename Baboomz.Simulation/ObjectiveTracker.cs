using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Tracks campaign level objectives. Pure C# — no Unity dependency.
    /// Updated each tick by GameSimulation. Signals completion via IsComplete.
    /// </summary>
    public class ObjectiveTracker
    {
        public string ObjectiveType { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }

        // survive_time
        public float TimeRemaining { get; private set; }

        // survive_waves
        public int CurrentWave { get; private set; }
        public int TotalWaves { get; private set; }
        public float WaveSpawnTimer { get; private set; }
        public bool WaveActive { get; private set; }
        private LevelWaveData[] waves;
        private bool wavesStarted;

        // destroy_target
        public int TargetsRemaining { get; private set; }
        public int TotalTargets { get; private set; }

        // defeat_boss
        public int BossPlayerIndex { get; private set; } = -1;

        // Track player for failure detection
        private int playerIndex = 0;

        public ObjectiveTracker(LevelObjectiveData objective)
        {
            ObjectiveType = objective?.type ?? "eliminate_all";

            switch (ObjectiveType)
            {
                case "survive_time":
                    TimeRemaining = objective.timeLimit;
                    break;

                case "survive_waves":
                    TotalWaves = objective.waveCount;
                    waves = objective.waves;
                    CurrentWave = 0;
                    WaveActive = false;
                    wavesStarted = false;
                    break;

                case "destroy_target":
                    TotalTargets = objective.targetCount;
                    TargetsRemaining = objective.targetCount;
                    break;

                case "defeat_boss":
                    // BossPlayerIndex set externally after boss is spawned
                    break;
            }
        }

        /// <summary>
        /// Tick the objective tracker. Called each frame by the game loop.
        /// </summary>
        public void Update(GameState state, float dt)
        {
            if (IsComplete || IsFailed) return;

            // Check player death — all objectives fail if player dies
            if (playerIndex < state.Players.Length && state.Players[playerIndex].IsDead)
            {
                IsFailed = true;
                return;
            }

            switch (ObjectiveType)
            {
                case "eliminate_all":
                    UpdateEliminateAll(state);
                    break;
                case "defeat_boss":
                    UpdateDefeatBoss(state);
                    break;
                case "survive_time":
                    UpdateSurviveTime(state, dt);
                    break;
                case "survive_waves":
                    UpdateSurviveWaves(state, dt);
                    break;
                case "destroy_target":
                    UpdateDestroyTarget(state);
                    break;
            }
        }

        void UpdateEliminateAll(GameState state)
        {
            // Win when all non-player combatants are dead
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (i == playerIndex) continue;
                if (!state.Players[i].IsDead) return; // still alive
            }
            IsComplete = true;
        }

        void UpdateDefeatBoss(GameState state)
        {
            if (BossPlayerIndex < 0 || BossPlayerIndex >= state.Players.Length) return;
            if (state.Players[BossPlayerIndex].IsDead)
            {
                IsComplete = true;
            }
        }

        void UpdateSurviveTime(GameState state, float dt)
        {
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsComplete = true;
            }
        }

        void UpdateSurviveWaves(GameState state, float dt)
        {
            if (waves == null || waves.Length == 0)
            {
                IsComplete = true;
                return;
            }

            // Start first wave
            if (!wavesStarted)
            {
                wavesStarted = true;
                if (waves.Length > 0)
                {
                    WaveSpawnTimer = waves[0].delay;
                    WaveActive = true;
                }
            }

            // Waiting for wave spawn delay
            if (WaveActive && CurrentWave < waves.Length)
            {
                if (WaveSpawnTimer > 0f)
                {
                    WaveSpawnTimer -= dt;
                    return; // still waiting
                }

                // Wave spawns are handled externally (by GameRunner/LevelLoader)
                // Once spawned, WaveActive is set to false by the caller
            }

            // Check if current wave enemies are all dead
            if (!WaveActive && CurrentWave < TotalWaves)
            {
                bool allDead = true;
                for (int i = 0; i < state.Players.Length; i++)
                {
                    if (i == playerIndex) continue;
                    if (!state.Players[i].IsDead)
                    {
                        allDead = false;
                        break;
                    }
                }

                if (allDead)
                {
                    CurrentWave++;
                    if (CurrentWave >= TotalWaves)
                    {
                        IsComplete = true;
                    }
                    else if (CurrentWave < waves.Length)
                    {
                        WaveSpawnTimer = waves[CurrentWave].delay;
                        WaveActive = true;
                    }
                }
            }
        }

        void UpdateDestroyTarget(GameState state)
        {
            // Target structures are tracked externally — TargetsRemaining is
            // decremented by the caller when a target structure is destroyed.
            if (TargetsRemaining <= 0)
            {
                IsComplete = true;
            }
        }

        /// <summary>Call when a wave has been spawned into the game.</summary>
        public void MarkWaveSpawned()
        {
            WaveActive = false;
        }

        /// <summary>Call when a target structure is destroyed.</summary>
        public void OnTargetDestroyed()
        {
            TargetsRemaining = Math.Max(0, TargetsRemaining - 1);
        }

        /// <summary>Set the boss player index for defeat_boss objectives.</summary>
        public void SetBossIndex(int index)
        {
            BossPlayerIndex = index;
        }

        /// <summary>Set which player index is the human player (for failure detection).</summary>
        public void SetPlayerIndex(int index)
        {
            playerIndex = index;
        }
    }
}
