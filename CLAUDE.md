# Baboomz (Godot 4.6.2 .NET)

Real-time 2D artillery game — Godot port of the Unity 6 version.

## Project Structure

```
Baboomz.csproj                    -- Godot .NET project (SDK 4.6.2)
Baboomz.Simulation/               -- Pure C# class library (NO Godot dependency)
  74 files, 11,789 lines            GameState, GameSimulation, GameConfig, Vec2, AI, Physics, etc.
Baboomz.Simulation.Tests/         -- NUnit test project (1015 passing, 0 failures)
  24 test files                      Run with: dotnet test Baboomz.Simulation.Tests/
Baboomz.E2E.Tests/                -- E2E integration tests (50 passing, 0 failures)
  5 test files                       Run with: dotnet test Baboomz.E2E.Tests/
    MatchLifecycleTests.cs             Full match creation → tick → end lifecycle
    InputPipelineTests.cs              Input → simulation → state pipeline
    GameModeTests.cs                   All 12 game modes create + tick
    SceneFlowTests.cs                  Menu → match → result data flow
    ConfigVariationTests.cs            Difficulty, health, wind config variations

Scripts/
  Core/                            -- Autoloads, bridges, settings
    GameAutoload.cs                  Singleton (registered in project.godot [autoload])
    GameModeContext.cs               Static: selected difficulty, player name, mode
    GameSettings.cs                  ConfigFile-based settings (user://settings.cfg)
    Vec2Extensions.cs                Vec2 <-> Vector2 bridge (NEGATES Y — Godot Y-down)
  Runtime/                         -- Godot renderers & bridges (21 files)
    GameRunner.cs                    Main orchestrator: creates match, ticks simulation, spawns renderers
    InputBridge.cs                   Keyboard + gamepad input -> GameState.PlayerInputs[]
    PlayerRenderer.cs                Sprite2D + aim line + name label
    ProjectileRenderer.cs            Sprite2D + Line2D trail
    ExplosionRenderer.cs             Expanding circle effects
    CameraTracker.cs                 Camera2D: follow player/projectile, screen shake
    GodotTerrainBridge.cs            (in Scripts/Terrain/) Sprite2D with bitmap shader
    TrajectoryPreview.cs             Line2D arc preview using GamePhysics
    KillFeed.cs                      Scrolling damage/kill event log
    MatchCountdown.cs                3, 2, 1, GO! — sets phase Waiting -> Playing
    DeathSlowMo.cs                   Engine.TimeScale = 0.3 on player death
    AudioBridge.cs                   Procedural audio: 6 SFX + 16s looping BGM
    HUDBridge.cs                     Reads GameState -> updates GameHUD labels/bars
    MineRenderer.cs                  Land mine visuals
    BarrelRenderer.cs                Oil barrel visuals
    CrateRenderer.cs                 Weapon crate visuals (color per type)
    MobRenderer.cs                   PvE enemy visuals
    SkillMarkerRenderer.cs           Shield, grapple, smoke visuals
    WindParticles.cs                 CpuParticles2D driven by wind state
    ProceduralSprites.cs             ImageTexture factory (WhitePixel, ColorRect, Circle)
    PlayerRecord.cs                  W/L/D + career stats (user://player_record.cfg)
    CareerStats.cs                   Kills, deaths, damage (user://career_stats.cfg)
  UI/                              -- Code-driven UI (6 files)
    UIBuilder.cs                     Color palette + Control node primitives
    GameHUD.cs                       Main HUD container (HP/EP bars, weapon slots, skills, wind)
    GameHUDBuilder.cs                Builds HUD hierarchy from code
    MainMenuSetup.cs                 Title, name select, difficulty, PLAY/QUIT
    PauseMenu.cs                     ESC toggle, resume/main menu buttons
    MatchResultPanel.cs              Winner, per-player stats, play again/menu
  Terrain/
    GodotTerrainBridge.cs            Bitmap terrain -> Sprite2D with shader mask
  Progression/
    PlayerSaveData.cs                Campaign save data (pure C#)
    SaveManager.cs                   JSON file save/load (user://baboomz_save.json)
    ProgressionService.cs            Stars, currency, upgrades

Scenes/
  Main.tscn                        -- Game scene (GameRunner as root Node2D)
  MainMenu.tscn                    -- Menu scene (MainMenuSetup as root Control)

Shaders/
  BitmapTerrain.gdshader           -- Earth/grass/destruction/indestructible layers
  SkyGradient.gdshader             -- Top-to-bottom sky color gradient

Resources/Levels/                  -- 28 level JSON files (campaign + tutorial)
Art/                               -- 76 PNG assets (characters, backgrounds, terrain, VFX, UI)
```

## Architecture

**Same as Unity version:** simulation is pure data + pure functions, Godot is just a renderer.

```
Simulation (pure C#, zero Godot)     Godot Runtime (thin renderers)
────────────────────────────────     ──────────────────────────────
GameState.cs     - state structs     GameRunner.cs     - orchestrator
GameSimulation.cs - Tick(), Fire()   PlayerRenderer.cs - Sprite2D + bars
GamePhysics.cs   - gravity, coll    ProjectileRenderer.cs - Sprite2D + trail
GameConfig.cs    - tunable values    GodotTerrainBridge.cs - bitmap shader
AILogic.cs       - ballistic AI      InputBridge.cs   - Input -> state
Vec2.cs          - custom math       HUDBridge.cs     - state -> HUD
```

### How it works
1. `GameAutoload` loads as singleton on startup
2. `MainMenu.tscn` loads → player picks name/difficulty → PLAY
3. Scene changes to `Main.tscn` → `GameRunner._Ready()` calls `StartMatch()`
4. `StartMatch()` creates `GameState` via `GameSimulation.CreateMatch()`, spawns all renderer nodes
5. `MatchCountdown` holds phase at `Waiting` for 3s, then sets `Playing`
6. `_Process()`: `GameSimulation.Tick(state, delta)` → renderers read state → update visuals
7. Match ends → `MatchResultPanel` shows stats → Play Again or Main Menu

### Y-axis convention
- **Simulation:** Y-up (positive Y = up), matches Unity convention
- **Godot:** Y-down (positive Y = down)
- **Bridge:** `Vec2Extensions.ToGodot()` negates Y: `new Vector2(v.x, -v.y)`
- ALL sim-to-render conversions go through this bridge

## Building & Running

```bash
# Build from CLI
dotnet build Baboomz.csproj

# Run simulation tests (no Godot needed) — 1015 passing, 0 failures
dotnet test Baboomz.Simulation.Tests/Baboomz.Simulation.Tests.csproj

# Run E2E tests (no Godot needed) — 50 tests covering full match lifecycle
dotnet test Baboomz.E2E.Tests/Baboomz.E2E.Tests.csproj

# Run ALL tests
dotnet test Baboomz.Simulation.Tests/ && dotnet test Baboomz.E2E.Tests/

# Run game — open in Godot editor, press Alt+B to build, F5 to run
# Or via Godot CLI:
# "C:/Program Files/Godot/Godot.exe" --path . --editor  (then Alt+B, F5)
```

## Development Workflow

This project uses **GitHub issues exclusively** for bug tracking and feature requests. No `docs/bugs/` or `docs/todos/` file queues.

**Repo**: [hylimR/baboomz-godot](https://github.com/hylimR/baboomz-godot)

### File a bug or feature
```
/report-bug <description>
```
Adds context, creates a GitHub issue with the right labels. Prefix with `feature:` or `visual:` to force feature mode / add visual label.

### Run the full dev cycle
```
/dev-cycle
```
Runs QA → Dev → Tech Lead in sequence.

### Individual roles (also loop-able)
- `/qa` — audit code, analyze balance, file issues
- `/dev` — pick up open issues, fix, open PRs
- `/tech-lead` — review open PRs, merge or request changes
- `/fix-bugs` — one-shot: fix one open bug via `bug-fixer` agent
- `/do-todo` — one-shot: handle one small task via `todo-doer` agent

### Verification (required before any commit)
```bash
dotnet build Baboomz.csproj
dotnet test Baboomz.Simulation.Tests/Baboomz.Simulation.Tests.csproj
dotnet test Baboomz.E2E.Tests/Baboomz.E2E.Tests.csproj
```

For visual/rendering changes, also capture a screenshot via the Godot MCP:
```
mcp__godot__capture_screenshot  projectPath=D:/Workspace/BaboomzGodot  scene=Scenes/Main.tscn  outputPath=/tmp/verify.png
```

### Code quality rules (enforced by Tech Lead)
- **File size**: ≤300 lines soft cap, ≤400 hard limit
- **Simulation purity**: no `using Godot;` in `Baboomz.Simulation/`
- **Test coverage**: bug fixes require a regression test; features require at least one test
- **Branch naming**: `fix/N-short-desc` or `feat/N-short-desc` where N is the issue number
- **Commit messages**: `fix: description (#N)` or `feat: description (#N)`
- **Visual PRs**: must include screenshot references in the PR body

### GitHub labels
- `bug`, `enhancement` — issue type
- `priority:high` / `priority:medium` / `priority:low` — triage level
- `visual` — rendering, sprites, shaders, art, animation work
- `needs-review`, `in-progress`, `changes-requested` — workflow state

## Coding Conventions

- **Namespace**: `Baboomz` for Godot scripts, `Baboomz.Simulation` for simulation
- **partial class**: REQUIRED for all classes extending Godot node types (source generator)
- **ProcessPriority**: Simulation=0 (default), Renderers=50, Camera=60, Audio=70+
- **No prefabs/scenes for gameplay**: All nodes created from code in `GameRunner.SetupAll()`
- **Persistence**: `ConfigFile` for settings/records, `System.Text.Json` for save data
- **Input**: Raw `Input.IsKeyPressed()` with manual edge detection (prev-frame tracking)

## Simulation Layer Rules (same as Unity)

- **ZERO `using Godot`** in `Baboomz.Simulation/` — pure C#, fully testable without Godot
- **State is pure data, logic is pure functions**
- **To change gameplay**: modify files in `Baboomz.Simulation/`
- **To change visuals**: modify files in `Scripts/Runtime/`
- **To test gameplay**: `dotnet test` (no Godot runtime needed)

## Dependencies

| Package | Purpose |
|---------|---------|
| Godot.NET.Sdk 4.6.2 | Engine bindings + source generators |
| NUnit 4.3.2 | Test framework (simulation tests only) |
| Newtonsoft.Json 13.0.3 | JSON serialization (level data, saves) |

No VContainer, no MessagePipe, no UniTask — replaced with:
- **DI**: `GameAutoload.Instance` singleton
- **Events**: C# `event Action<T>` (or direct method calls)
- **Async**: `async/await` with `ToSignal()`

## What's Ported vs Remaining

### Ported (feature-complete)
- Full simulation (22 weapons, 18 skills, 5 bosses, 8 biomes, all game modes)
- Input (keyboard + gamepad with per-player routing)
- All core renderers (terrain, players, projectiles, explosions, camera)
- Full HUD (HP/EP bars, 22-weapon viewport, skill slots, wind indicator)
- Procedural audio (6 SFX + 16s looping BGM)
- Menu flow (MainMenu -> Game -> MatchResult -> MainMenu)
- Pause (ESC)
- Persistence (W/L/D, career stats, campaign progress, settings)
- TrajectoryPreview, KillFeed, Countdown, DeathSlowMo
- Mines, barrels, crates, mobs, skill markers, wind particles
- Encyclopedia, Loadout, Achievement panels (`Scripts/UI/EncyclopediaPanel.cs`, `LoadoutPanel.cs`, `AchievementPanel.cs`)
- Campaign UI panels built (LevelSelect, Shop — MainMenu wiring tracked in #112)
- Replay viewer with speed controls + HUD (`Scripts/Runtime/GameRunner.Replay.cs`)
- Hat renderer — all 11 hat types (`Scripts/Runtime/HatRenderer.cs`)
- Emote renderer + taunt display (`Scripts/Runtime/EmoteRenderer.cs` + `Baboomz.Simulation/State/EmoteText.cs`)
- Real sprite loading wired to PlayerRenderer, ExplosionRenderer, ParallaxBackgroundRenderer (`Scripts/Runtime/SpriteLoader.cs`)
- Combat feedback — hit markers, low-health vignette, combo renderer (`Scripts/Runtime/{HitMarkerRenderer,LowHealthOverlay,ComboRenderer}.cs`)
- 1015+ simulation tests passing (0 failures)
- 50+ E2E integration tests (match lifecycle, input pipeline, all game modes, scene flow, config)

### Remaining (nice-to-have)
- Mode-specific renderers — placeholder shapes need sprite art upgrade (tracked in #113)
- Campaign UI wiring — LevelSelect / Shop / Achievement flow integrated into MainMenu (tracked in #112)
- Editor tools (Level Preview, Level Validator as Godot plugins — no `addons/` directory yet)

## Migrated From

Original Unity project: `D:\Workspace\Baboomz`
Feasibility analysis: `D:\Workspace\Baboomz\docs\plans\2026-04-09-godot-migration-feasibility.md`
Execution plan: `D:\Workspace\Baboomz\docs\plans\2026-04-09-godot-migration-execution.md`
