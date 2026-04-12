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
            _isReplayMode = false;

            // Start recording for replay
            StartRecording();

            SetupAll();
        }

        private void SetupAll()
        {
            // Input
            var inputBridge = new InputBridge();
            inputBridge.Name = "InputBridge";
            AddChild(inputBridge);
            inputBridge.SetState(State);

            // Parallax background (behind terrain — CanvasLayer with negative index)
            var parallax = new ParallaxBackgroundRenderer();
            parallax.Name = "Parallax";
            AddChild(parallax);
            parallax.Init();

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

            // Explosions
            var explosions = new ExplosionRenderer();
            explosions.Name = "Explosions";
            AddChild(explosions);
            explosions.Init(State, _cameraTracker);

            // Terrain debris (biome-colored chunks on every explosion)
            var debris = new TerrainDebrisRenderer();
            debris.Name = "TerrainDebris";
            AddChild(debris);
            debris.Init(State);

            // CTF flags (only spawns visuals when MatchType.CaptureTheFlag)
            var ctfFlags = new CtfFlagRenderer();
            ctfFlags.Name = "CtfFlags";
            AddChild(ctfFlags);
            ctfFlags.Init(State);

            // KOTH zone (only renders when MatchType.KingOfTheHill)
            var kothZone = new KothZoneRenderer();
            kothZone.Name = "KothZone";
            AddChild(kothZone);
            kothZone.Init(State);

            // Payload cart (only renders when MatchType.Payload)
            var payloadCart = new PayloadCartRenderer();
            payloadCart.Name = "PayloadCart";
            AddChild(payloadCart);
            payloadCart.Init(State);

            // Territories zones (only renders when MatchType.Territories)
            var territoryZones = new TerritoryZoneRenderer();
            territoryZones.Name = "TerritoryZones";
            AddChild(territoryZones);
            territoryZones.Init(State);

            // Demolition crystals (only renders when MatchType.Demolition)
            var demoCrystals = new DemolitionCrystalRenderer();
            demoCrystals.Name = "DemoCrystals";
            AddChild(demoCrystals);
            demoCrystals.Init(State);

            // Headhunter tokens (only renders when MatchType.Headhunter)
            var hhTokens = new HeadhunterTokenRenderer();
            hhTokens.Name = "HeadhunterTokens";
            AddChild(hhTokens);
            hhTokens.Init(State);

            // Hat renderer (displays equipped hats above player heads)
            var hatRenderer = new HatRenderer();
            hatRenderer.Name = "HatRenderer";
            AddChild(hatRenderer);
            hatRenderer.Init(State);

            // Emote renderer (speech bubbles on EmoteEvents)
            var emoteRenderer = new EmoteRenderer();
            emoteRenderer.Name = "EmoteRenderer";
            AddChild(emoteRenderer);
            emoteRenderer.Init(State);

            // Hit markers (world-space damage feedback)
            var hitMarkers = new HitMarkerRenderer();
            hitMarkers.Name = "HitMarkers";
            AddChild(hitMarkers);
            hitMarkers.Init(State);

            // Combo display
            var comboRenderer = new ComboRenderer();
            comboRenderer.Name = "ComboRenderer";
            AddChild(comboRenderer);
            comboRenderer.Init(State);

            // Audio
            _audioBridge = new AudioBridge();
            _audioBridge.Name = "Audio";
            AddChild(_audioBridge);
            _audioBridge.Init(State);

            // Trajectory preview
            var trajectory = new TrajectoryPreview();
            trajectory.Name = "TrajectoryPreview";
            AddChild(trajectory);
            trajectory.Init(State);

            // Kill feed
            var killFeed = new KillFeed();
            killFeed.Name = "KillFeed";
            AddChild(killFeed);
            killFeed.Init(State);

            // Countdown (sets phase to Waiting, then Playing after 3s)
            var countdown = new MatchCountdown();
            countdown.Name = "Countdown";
            AddChild(countdown);
            countdown.Init(State);

            // Death slow-mo
            var deathSlowMo = new DeathSlowMo();
            deathSlowMo.Name = "DeathSlowMo";
            AddChild(deathSlowMo);
            deathSlowMo.Init(State);

            // Low-HP red vignette overlay (CanvasLayer below HUD).
            // Audio bridge is passed so the overlay can emit a one-shot heartbeat
            // cue when the local player first crosses the low-HP threshold (#36).
            var lowHpOverlay = new LowHealthOverlay();
            lowHpOverlay.Name = "LowHealthOverlay";
            AddChild(lowHpOverlay);
            lowHpOverlay.Init(State, _audioBridge);

            // HUD (on a CanvasLayer so it renders above everything)
            var hudLayer = new CanvasLayer();
            hudLayer.Name = "HUDLayer";
            hudLayer.Layer = 10;
            AddChild(hudLayer);

            var hud = new GameHUD();
            hud.Name = "GameHUD";
            hudLayer.AddChild(hud);
            hud.BuildUI();

            var hudBridge = new HUDBridge();
            hudBridge.Name = "HUDBridge";
            AddChild(hudBridge);
            hudBridge.Init(State, hud);

            // PauseMenu (on HUD layer so it renders above game)
            var pauseMenu = new PauseMenu();
            pauseMenu.Name = "PauseMenu";
            hudLayer.AddChild(pauseMenu);

            // MatchResultPanel (on HUD layer, hidden until match ends)
            _matchResultPanel = new MatchResultPanel();
            _matchResultPanel.Name = "MatchResultPanel";
            hudLayer.AddChild(_matchResultPanel);

            // Sky background color
            RenderingServer.SetDefaultClearColor(new Color(0.31f, 0.70f, 0.96f));

            GD.Print("Match setup complete — countdown started");
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
