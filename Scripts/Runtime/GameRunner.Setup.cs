using Godot;

namespace Baboomz
{
    /// <summary>
    /// Renderer and node spawning for GameRunner. Creates all gameplay
    /// nodes (input, terrain, players, camera, audio, HUD, etc.) in SetupAll().
    /// </summary>
    public partial class GameRunner
    {
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

            // Terrain debris
            var debris = new TerrainDebrisRenderer();
            debris.Name = "TerrainDebris";
            AddChild(debris);
            debris.Init(State);

            // Mode-specific renderers (auto-hide for non-matching types)
            SpawnModeRenderers();

            // Hat + emote renderers
            var hatRenderer = new HatRenderer();
            hatRenderer.Name = "HatRenderer";
            AddChild(hatRenderer);
            hatRenderer.Init(State);

            var emoteRenderer = new EmoteRenderer();
            emoteRenderer.Name = "EmoteRenderer";
            AddChild(emoteRenderer);
            emoteRenderer.Init(State);

            // Hit markers
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

            // Countdown
            var countdown = new MatchCountdown();
            countdown.Name = "Countdown";
            AddChild(countdown);
            countdown.Init(State);

            // Death slow-mo
            var deathSlowMo = new DeathSlowMo();
            deathSlowMo.Name = "DeathSlowMo";
            AddChild(deathSlowMo);
            deathSlowMo.Init(State);

            // Low-HP vignette overlay
            var lowHpOverlay = new LowHealthOverlay();
            lowHpOverlay.Name = "LowHealthOverlay";
            AddChild(lowHpOverlay);
            lowHpOverlay.Init(State, _audioBridge);

            // HUD layer
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

            var pauseMenu = new PauseMenu();
            pauseMenu.Name = "PauseMenu";
            hudLayer.AddChild(pauseMenu);

            _matchResultPanel = new MatchResultPanel();
            _matchResultPanel.Name = "MatchResultPanel";
            hudLayer.AddChild(_matchResultPanel);

            RenderingServer.SetDefaultClearColor(new Color(0.31f, 0.70f, 0.96f));
            GD.Print("Match setup complete — countdown started");
        }

        private void SpawnModeRenderers()
        {
            var ctfFlags = new CtfFlagRenderer();
            ctfFlags.Name = "CtfFlags";
            AddChild(ctfFlags);
            ctfFlags.Init(State);

            var kothZone = new KothZoneRenderer();
            kothZone.Name = "KothZone";
            AddChild(kothZone);
            kothZone.Init(State);

            var payloadCart = new PayloadCartRenderer();
            payloadCart.Name = "PayloadCart";
            AddChild(payloadCart);
            payloadCart.Init(State);

            var territoryZones = new TerritoryZoneRenderer();
            territoryZones.Name = "TerritoryZones";
            AddChild(territoryZones);
            territoryZones.Init(State);

            var demoCrystals = new DemolitionCrystalRenderer();
            demoCrystals.Name = "DemoCrystals";
            AddChild(demoCrystals);
            demoCrystals.Init(State);

            var hhTokens = new HeadhunterTokenRenderer();
            hhTokens.Name = "HeadhunterTokens";
            AddChild(hhTokens);
            hhTokens.Init(State);
        }
    }
}
