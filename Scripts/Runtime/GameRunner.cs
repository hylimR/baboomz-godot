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
            config.UnlockedTier = UnlockRegistry.GetTier(0); // TODO: load wins from save
            _matchConfig = config;

            if (seed < 0) seed = (int)(GD.Randi() % 99999);

            State = GameSimulation.CreateMatch(config, seed,
                GameModeContext.SelectedSkillSlot0, GameModeContext.SelectedSkillSlot1);
            AILogic.Reset(seed, State.Players.Length);
            BossLogic.Reset(seed, State.Players.Length);

            GD.Print($"Match started: seed={seed}, players={State.Players.Length}, phase={State.Phase}");

            _matchResultShown = false;
            _matchEndTimer = -1f;

            SetupAll();
        }

        private void SetupAll()
        {
            // Input
            var inputBridge = new InputBridge();
            inputBridge.Name = "InputBridge";
            AddChild(inputBridge);
            inputBridge.SetState(State);

            // Terrain
            var terrain = new GodotTerrainBridge();
            terrain.Name = "Terrain";
            AddChild(terrain);
            terrain.Init(State);

            // Players
            playerRenderers = new PlayerRenderer[State.Players.Length];
            for (int i = 0; i < State.Players.Length; i++)
            {
                var pr = new PlayerRenderer();
                pr.Name = State.Players[i].Name;
                AddChild(pr);
                pr.Init(i, State);
                playerRenderers[i] = pr;
            }

            // Camera
            _cameraTracker = new CameraTracker();
            _cameraTracker.Name = "Camera";
            AddChild(_cameraTracker);
            _cameraTracker.Init(State);

            State.Phase = MatchPhase.Playing;
            GD.Print("Match phase set to Playing — simulation ticking");
        }

        public override void _Process(double delta)
        {
            if (State == null) return;

            if (State.Phase == MatchPhase.Playing)
            {
                GameSimulation.Tick(State, (float)delta);
                SyncProjectileRenderers();
            }

            // Match end detection
            if (State.Phase == MatchPhase.Ended && !_matchResultShown)
            {
                if (_matchEndTimer < 0f)
                    _matchEndTimer = MatchEndDelay;

                _matchEndTimer -= (float)delta;
                if (_matchEndTimer <= 0f)
                {
                    _matchResultShown = true;
                    GD.Print($"Match ended! Winner: {State.WinnerIndex}");
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
