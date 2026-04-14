---
name: qa
description: Daily QA & Design review — audits code for bugs, analyzes balance, checks feature completeness, and files multiple GitHub issues. Combines QA tester, game designer, and product owner roles. Run daily.
---

# QA & Design Review

Daily audit session combining **QA testing**, **game design/balance analysis**, and **product ownership**. Finds bugs, proposes features, checks game completeness — all in one pass.

## Usage

```
/loop 10m /qa
```

## Per-Cycle Workflow

### 0. Switch to main

**Always start the cycle on `main`:**

```bash
git checkout main && git pull origin main
```

### 1. Check backlog — don't flood

```bash
gh issue list --label bug --state open
gh issue list --label enhancement --state open
```

- If **10+ total open issues** (bugs + enhancements), **skip this cycle** (let Dev catch up)
- Read titles of open issues to avoid filing duplicates

### 2. Pick audit areas (do ALL three phases)

**Phase A — Bug Hunting (QA Tester)**

Rotate through these code areas:

| Area | Files to read |
|------|---------------|
| Weapons | `Baboomz.Simulation/Config/GameConfigWeapons.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulation.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulationSpecial.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulationSticky.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulationSpawners.cs` |
| Skills | `Baboomz.Simulation/Skills/SkillSystem.cs`, `Baboomz.Simulation/Skills/SkillSystemActivation.cs`, `Baboomz.Simulation/Skills/SkillSystemEffects.cs`, `Baboomz.Simulation/Skills/SkillSystemEnvironmental.cs`, `Baboomz.Simulation/Skills/SkillSystemRope.cs` |
| AI | `Baboomz.Simulation/AI/AILogic.cs`, `Baboomz.Simulation/AI/AILogicMobs.cs`, `Baboomz.Simulation/AI/AILogicWeapons.cs`, `Baboomz.Simulation/AI/AILogicLoadout.cs` |
| Physics | `Baboomz.Simulation/Physics/GamePhysics.cs`, `Baboomz.Simulation/Simulation/GameSimulationPlayer.cs` |
| Environment | `Baboomz.Simulation/Simulation/GameSimulationEnvironment.cs`, `Baboomz.Simulation/Simulation/GameSimulationCrates.cs`, `Baboomz.Simulation/GameModes/GameSimulationFireZones.cs`, `Baboomz.Simulation/Simulation/GameSimulationHazards.cs` |
| Combat | `Baboomz.Simulation/Combat/CombatResolver.cs`, `Baboomz.Simulation/Combat/CombatResolverSpecial.cs`, `Baboomz.Simulation/Simulation/GameSimulation.cs`, `Baboomz.Simulation/Simulation/GameSimulationFiring.cs`, `Baboomz.Simulation/Simulation/GameSimulationHitscan.cs` |
| Bosses | `Baboomz.Simulation/Boss/BossLogic.cs`, `Baboomz.Simulation/Boss/BossIronSentinel.cs`, `Baboomz.Simulation/Boss/BossSandWyrm.cs`, `Baboomz.Simulation/Boss/BossGlacialCannon.cs`, `Baboomz.Simulation/Boss/BossForgeColossus.cs`, `Baboomz.Simulation/Boss/BossBaronCogsworth.cs` |
| Game Modes | `Baboomz.Simulation/GameModes/GameSimulationArmsRace.cs`, `GameSimulationCtf.cs`, `GameSimulationDemolition.cs`, `GameSimulationHeadhunter.cs`, `GameSimulationKoth.cs`, `GameSimulationPayload.cs`, `GameSimulationRoulette.cs`, `GameSimulationSurvival.cs`, `GameSimulationTerritories.cs`, `TargetPractice.cs` |
| Buffs/Progression | `Baboomz.Simulation/Simulation/GameSimulationBuffs.cs`, `Baboomz.Simulation/Progression/AchievementTracker.cs`, `Baboomz.Simulation/Progression/ChallengeSystem.cs`, `Baboomz.Simulation/Progression/RankSystem.cs`, `Baboomz.Simulation/Progression/WeaponMastery.cs` |
| Terrain | `Baboomz.Simulation/Terrain/TerrainState.cs`, `Baboomz.Simulation/Terrain/TerrainGenerator.cs`, `Baboomz.Simulation/Terrain/TerrainFeatures.cs`, `Baboomz.Simulation/Terrain/TerrainBiome.cs` |
| Rendering | `Scripts/Runtime/PlayerRenderer.cs`, `Scripts/Runtime/ProjectileRenderer.cs`, `Scripts/Runtime/ExplosionRenderer.cs`, `Scripts/Runtime/HUDBridge.cs`, `Scripts/Terrain/GodotTerrainBridge.cs` |

**Phase B — Balance & Design (Game Designer)**

Rotate through these design focuses:

| Focus | What to analyze |
|-------|----------------|
| Weapons | DPS matrix, ammo economy, weapon synergies, new weapon concepts |
| Skills | Skill utility comparison, cooldown/energy analysis, new skill ideas |
| Balance | Win rate estimation, DPS/energy ratios, outlier detection |
| Game Modes | New mode concepts (KOTH, survival, capture) |
| Progression | Unlock ideas, achievement gaps, mastery system |
| Polish | Game feel, VFX gaps, UI improvements |
| Story/Lore | Boss backstories, biome lore, campaign narrative |
| Map Design | New biome concepts, terrain features, environmental hazards |

**Phase C — Completeness Check (Product Owner)**

Audit against the feature list in CLAUDE.md "What's Ported vs Remaining" section:
- What remaining features from the Unity version are still missing?
- What recently added features lack polish?
- Are there regressions in existing features?
- What's the highest-impact gap right now?

Pay special attention to the **visual replication gap** — the Unity version has full character sprites, terrain textures, explosion animations, and parallax backgrounds. The Godot port currently uses placeholder rectangles and procedural circles. File these gaps with the `visual` label.

**Use Godot MCP to verify visual gaps before filing.** Before filing a `visual` issue, run the project and capture a screenshot so the issue body can reference the actual current state:

```
mcp__godot__run_project           projectPath=D:/Workspace/game/BaboomzGodot-Lead  scene=Scenes/Main.tscn
mcp__godot__capture_screenshot    projectPath=D:/Workspace/game/BaboomzGodot-Lead  scene=Scenes/Main.tscn     outputPath=/tmp/qa-main.png
mcp__godot__capture_screenshot    projectPath=D:/Workspace/game/BaboomzGodot-Lead  scene=Scenes/MainMenu.tscn outputPath=/tmp/qa-menu.png
mcp__godot__get_debug_output
mcp__godot__stop_project
```

Also use `get_debug_output` during runtime checks to surface errors/warnings that code-reading alone would miss — file those as bugs.

---

### 3. Phase A — Bug Hunting

Use an `Explore` subagent to read the focus area files and find **real bugs**:

- Array bounds issues
- NaN/infinity from division
- State not reset between matches
- Feature interactions that break (e.g., freeze + rope swing)
- Missing edge case handling
- Dead code paths
- SOLID violations (>300 lines, `using Godot` in `Baboomz.Simulation/`)

**Only report REAL bugs** — not theoretical ones. Must have file:line reference.

### 4. Phase B — Balance & Design Analysis

For the current design focus:

**If analyzing weapons/skills:**
1. Read stats from `Baboomz.Simulation/Config/GameConfigWeapons.cs` and `GameConfig.cs`
2. Compute DPS = MaxDamage / ShootCooldown, Burst = MaxDamage * ProjectileCount
3. Compare DPS/Energy ratios across all weapons
4. Flag outliers (>2x or <0.5x median)
5. Think about player fantasy, counterplay, skill ceiling

**If proposing new content:**
- What does it make the player *feel*?
- How does the opponent respond?
- Where does it sit in the DPS/utility matrix?
- What's the implementation scope (small/medium/large)?

### 5. Phase C — Completeness Check

1. Read CLAUDE.md "What's Ported vs Remaining" section
2. Check against Worms/DDT benchmarks:
   - Core gameplay (move/jump/aim/shoot, terrain destruction, win conditions)
   - Weapon variety
   - Movement options (teleport, dash, jetpack, rope)
   - Game modes, team play, campaign
   - Visual polish, audio, UI completeness
3. Identify top 3 gaps by player impact

---

### 6. File issues (multiple per cycle)

File **all findings** from all three phases. No per-phase cap — file everything worth filing.

**For bugs:**

```bash
gh issue create \
  --title "[BUG] Brief description" \
  --label "bug,priority:medium" \
  --body "$(cat <<'EOF'
## Location
`file.cs:line`

## Issue
What's wrong — describe the actual code problem.

## Impact
What breaks in gameplay.

## Suggested Fix
One-line suggested approach.

---
*Filed via `/qa` — Phase A (Bug Hunt)*
EOF
)"
```

**For feature/design proposals:**

```bash
gh issue create \
  --title "[FEAT] feature name — one-line pitch" \
  --label "enhancement,priority:medium" \
  --body "$(cat <<'ISSUE'
## Concept
<What is it and why is it valuable?>

## Design

### Behavior
<How it works step by step>

### Stats (if weapon/skill)
| Stat | Value | Rationale |
|------|-------|-----------|

## Balance Fit
<Where it sits relative to existing weapons/skills>

## Implementation Notes
- Files to modify: <list>
- Estimated scope: small/medium/large

---
*Filed via `/qa` — Phase B (Design)*
ISSUE
)"
```

**For balance issues:**

```bash
gh issue create \
  --title "[BALANCE] weapon/skill — over/underperforming" \
  --label "enhancement,priority:medium" \
  --body "$(cat <<'ISSUE'
## Analysis
<DPS table, comparison data>

## Problem
<What's wrong with current tuning>

## Proposed Change
| Stat | Current | Proposed | Reason |
|------|---------|----------|--------|

## Impact
<How this affects gameplay feel>

---
*Filed via `/qa` — Phase B (Balance)*
ISSUE
)"
```

**For missing features / completeness gaps:**

```bash
gh issue create \
  --title "[FEAT] feature — fills gap vs Unity version" \
  --label "enhancement,priority:high" \
  --body "$(cat <<'ISSUE'
## Gap
<What's missing compared to the Unity version>

## User Impact
<How much this hurts player experience>

## Proposed Design
<Brief behavior description>

## Acceptance Criteria
- [ ] <testable condition>
- [ ] <testable condition>

## Implementation Notes
- Estimated scope: small/medium/large
- Dependencies: <what needs to exist first>

---
*Filed via `/qa` — Phase C (Completeness)*
ISSUE
)"
```

**For visual/rendering gaps (add `visual` label):**

```bash
gh issue create \
  --title "[VISUAL] <system> — replace placeholder with real art" \
  --label "enhancement,visual,priority:medium" \
  --body "$(cat <<'ISSUE'
## Current State
<What the placeholder looks like — e.g., "32x48 colored rectangle for players">

## Target State
<What it should look like — reference the Unity implementation>

## Assets Available
<Which PNG files in Art/ should be wired up>

## Implementation Plan
- Files to modify: <list>
- New files needed: <list>
- Verification: `mcp__godot__capture_screenshot` on Main.tscn

---
*Filed via `/qa` — Phase C (Visual)*
ISSUE
)"
```

Adjust priority labels:
- `priority:high` — crashes, blocking gameplay, P0/P1 feature gap
- `priority:medium` — incorrect behavior, balance issues, nice-to-have features
- `priority:low` — code quality, edge cases, polish, lore

### 7. Spawn Dev if issues are piling up

After filing issues, check if Dev needs help:

```bash
gh issue list --label bug,enhancement --state open
gh pr list --state open --author @me
```

If there are **5+ open issues** and **no Dev PRs in progress** (0 open PRs from @me), spawn a Dev subagent to start working them down:

```
Agent(subagent_type="general-purpose", mode="bypassPermissions", run_in_background=true,
  prompt="Run the /dev skill: pick up open GitHub issues, fix them on branches, and open PRs. Work on multiple issues. Follow the skill instructions exactly.")
```

This keeps the pipeline flowing — QA files issues, Dev picks them up, Tech Lead reviews.

### 8. Return to main (end of cycle)

**Always end the cycle back on `main`:**

```bash
git checkout main && git pull origin main
```

### 9. Report

```
## QA & Design Report — [date]

### Phase A — Bug Hunt
- Area audited: [area]
- Bugs filed: N
- #X: title

### Phase B — Design & Balance
- Focus: [area]
- Proposals filed: N
- #X: title
- Balance status: healthy / N outliers flagged

### Phase C — Completeness
- Top gaps identified: N
- Issues filed: N
- #X: title

### Totals
- Total issues filed: N
- Open bug count: N
- Open enhancement count: N
```

## Rules

- **Read-only** — never modify code, never create branches
- **File everything worth filing** — no artificial cap per phase
- **Skip if 10+ total open issues** — prevent backlog explosion
- **No duplicates** — always check open issues first
- **Simulation layer for bugs** — renderers can be audited but visual bugs need screenshot verification
- **File:line required for bugs** — every bug must reference exact location
- **Balance context for features** — every weapon/skill proposal must compare to existing
- **Read the code before filing** — verify assumptions against actual source
- **Scope estimates required** — every feature proposal tags small/medium/large
- **Use `visual` label** — for any rendering, sprite, shader, or art-related issue
- **Start and end the cycle on main** — switch to `main` at step 0, switch back to `main` at step 8
- **Use Godot MCP for verification** — before filing visual/runtime bugs, use `run_project`, `get_debug_output`, and `capture_screenshot` (project path `D:/Workspace/game/BaboomzGodot-Lead`) to confirm the issue reproduces and to attach screenshots in issue bodies
