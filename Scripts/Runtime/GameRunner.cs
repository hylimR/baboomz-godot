using Godot;
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
            // Renderers will be added here as they are ported
            // For now, just start the playing phase directly
            State.Phase = MatchPhase.Playing;
            GD.Print("Match phase set to Playing — simulation ticking");
        }

        public override void _Process(double delta)
        {
            if (State == null) return;

            if (State.Phase == MatchPhase.Playing)
            {
                GameSimulation.Tick(State, (float)delta);
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
