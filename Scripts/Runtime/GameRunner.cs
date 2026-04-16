using Godot;
using System.Collections.Generic;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Main game orchestrator. Creates match state, ticks simulation,
    /// spawns renderers as child nodes. Equivalent of Unity's GameRunner.
    /// </summary>
    public partial class GameRunner : Node2D
    {
        public GameState State { get; private set; }

        // Renderers
        internal PlayerRenderer[] playerRenderers;
        private CameraTracker _cameraTracker;
        private AudioBridge _audioBridge;
        private MatchResultPanel _matchResultPanel;
        private readonly Dictionary<int, ProjectileRenderer> _projectileRenderers = new();
        private readonly HashSet<int> _knownProjectileIds = new();

        // Match config
        private GameConfig _matchConfig;
        private bool _matchResultShown;
        private float _matchEndTimer = -1f;
        private const float MatchEndDelay = 2f;

        public override void _Ready()
        {
            StartMatch();
        }

        public void StartMatch(int seed = -1, GameConfig config = null)
        {
            config ??= new GameConfig();
            ApplyDifficulty(config, GameModeContext.SelectedDifficulty);
            config.Player1Name = GameModeContext.PlayerName;
            PlayerRecord.Load();
            config.UnlockedTier = UnlockRegistry.GetTier(PlayerRecord.Wins);

            // #162: load the selected campaign level so it actually drives the
            // match. Previously SelectedLevelId was written by LevelSelectPanel
            // but read by no one, so the campaign silently fell through to a
            // random-match. LevelLoader overrides MatchType, map size, spawn,
            // difficulty scaling. Terrain seed (when present in the file)
            // overrides the caller's seed so campaign levels stay deterministic.
            Simulation.TutorialStepDef[] tutorialSteps = null;
            if (!string.IsNullOrEmpty(GameModeContext.SelectedLevelId))
            {
                var (loaded, levelSeed, steps) = LevelLoader.TryApply(config, GameModeContext.SelectedLevelId);
                if (loaded && levelSeed.HasValue && seed < 0)
                    seed = levelSeed.Value;
                if (loaded && steps != null)
                    tutorialSteps = steps;
            }

            _matchConfig = config;

            if (seed < 0) seed = (int)(GD.Randi() % 99999);

            State = GameSimulation.CreateMatch(config, seed,
                GameModeContext.SelectedSkillSlot0, GameModeContext.SelectedSkillSlot1);
            AILogic.Reset(seed, State.Players.Length);
            BossLogic.Reset(seed, State.Players.Length);

            // Initialize tutorial if the level has tutorial steps
            if (tutorialSteps != null && tutorialSteps.Length > 0)
            {
                State.Tutorial = Simulation.TutorialSystem.CreateFromSteps(tutorialSteps);
                Simulation.TutorialSystem.InitStepTracking(State.Tutorial, State);
            }

            GD.Print($"Match started: seed={seed}, players={State.Players.Length}, phase={State.Phase}");

            _matchResultShown = false;
            _matchEndTimer = -1f;
            _isReplayMode = false;

            // Start recording for replay
            StartRecording();

            SetupAll();
        }

        public override void _Process(double delta)
        {
            if (State == null) return;

            if (_isReplayMode)
            {
                ProcessReplayTick((float)delta);
                UpdateReplayHUD();
                return;
            }

            if (State.Phase == MatchPhase.Playing)
            {
                GameSimulation.Tick(State, (float)delta);
                SyncProjectileRenderers();
            }

            // Match end detection
            if (State.Phase == MatchPhase.Ended && !_matchResultShown)
            {
                if (_matchEndTimer < 0f)
                {
                    _matchEndTimer = MatchEndDelay;
                    StopAndSaveRecording();
                }

                _matchEndTimer -= (float)delta;
                if (_matchEndTimer <= 0f)
                {
                    _matchResultShown = true;
                    GD.Print($"Match ended! Winner: {State.WinnerIndex}");
                    _matchResultPanel?.ShowResult(State);
                }
            }
        }

        private void SyncProjectileRenderers()
        {
            // Spawn renderers for new projectiles
            foreach (var proj in State.Projectiles)
            {
                if (proj.Alive && !_knownProjectileIds.Contains(proj.Id))
                {
                    _knownProjectileIds.Add(proj.Id);
                    var pr = new ProjectileRenderer();
                    pr.Name = $"Projectile_{proj.Id}";
                    AddChild(pr);
                    pr.Init(proj.Id, State);
                    _projectileRenderers[proj.Id] = pr;
                    _audioBridge?.OnProjectileFired();
                }
            }

            // Clean up dead renderers
            var dead = new List<int>();
            foreach (var kvp in _projectileRenderers)
            {
                if (!IsInstanceValid(kvp.Value) || kvp.Value.IsQueuedForDeletion())
                    dead.Add(kvp.Key);
            }
            foreach (int id in dead)
            {
                _projectileRenderers.Remove(id);
                _knownProjectileIds.Remove(id);
            }
        }

        private static void ApplyDifficulty(GameConfig config, Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    config.AIAimErrorMargin = 12f;
                    config.AIShootInterval = 5f;
                    config.DefaultMaxHealth = 150f;
                    config.AIDifficultyLevel = 0;
                    break;
                case Difficulty.Normal:
                    config.AIDifficultyLevel = 1;
                    break;
                case Difficulty.Hard:
                    config.AIAimErrorMargin = 2f;
                    config.AIShootInterval = 2f;
                    config.DefaultMaxHealth = 80f;
                    config.AIDifficultyLevel = 2;
                    break;
            }
        }
    }
}
