---
name: report-bug
description: Report a bug or propose a new feature with a simple description. The agent finds relevant code, adds context, and creates a GitHub issue.
---

# Report Bug / Propose Feature

Turn a simple description into a fully-contextualized GitHub issue — either a bug report or a feature proposal.

## Usage

```
/report-bug terrain - player clips through edge when moving fast
/report-bug AI never uses sheep weapon
/report-bug freeze grenade doesn't work on bosses
/report-bug feature: add grappling hook weapon that pulls player to terrain
/report-bug feat: show damage numbers as floating text above hit player
/report-bug visual: player should use real character sprites instead of rectangles
```

The skill auto-detects the type based on the description. Prefixing with `feature:` / `feat:` forces feature mode. Prefixing with `visual:` forces feature mode AND adds the `visual` label.

## Workflow

### 1. Detect issue type

Classify as **bug**, **feature**, or **visual** based on the description:

| Signal | Type |
|--------|------|
| Starts with `feature:`, `feat:`, `proposal:` | Feature |
| Starts with `visual:` | Feature + `visual` label |
| Contains "add", "new", "should", "would be nice", "wish", "idea" | Feature (if no bug indicators) |
| Contains "broken", "doesn't work", "crash", "wrong", "bug", "fail", "clips", "never" | Bug |
| Mentions "sprite", "texture", "rendering", "shader", "animation", "explosion frames", "parallax" | Feature + `visual` label |
| Describes existing behavior that's incorrect | Bug |
| Describes behavior that doesn't exist yet | Feature |
| Ambiguous | Ask the user |

### 2. Parse the description

Extract:
- **Area**: which system is affected (terrain, AI, weapons, skills, physics, UI, rendering, etc.)
- **For bugs**: the symptom — what goes wrong
- **For features**: the desired behavior — what should happen

### 3. Find the relevant code

Based on the area, search for the likely source:

| Area | Files to check |
|------|---------------|
| Weapons | `Baboomz.Simulation/Config/GameConfigWeapons.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulation.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulationSpecial.cs`, `Baboomz.Simulation/Projectiles/ProjectileSimulationSticky.cs` |
| Skills | `Baboomz.Simulation/Skills/SkillSystem.cs`, `Baboomz.Simulation/Skills/SkillSystemActivation.cs`, `Baboomz.Simulation/Skills/SkillSystemEffects.cs`, `Baboomz.Simulation/Skills/SkillSystemRope.cs` |
| AI | `Baboomz.Simulation/AI/AILogic.cs`, `Baboomz.Simulation/AI/AILogicMobs.cs`, `Baboomz.Simulation/AI/AILogicWeapons.cs`, `Baboomz.Simulation/AI/AILogicLoadout.cs` |
| Terrain/Physics | `Baboomz.Simulation/Physics/GamePhysics.cs`, `Baboomz.Simulation/Terrain/TerrainState.cs`, `Baboomz.Simulation/Terrain/TerrainGenerator.cs`, `Baboomz.Simulation/Simulation/GameSimulationPlayer.cs` |
| Combat | `Baboomz.Simulation/Combat/CombatResolver.cs`, `Baboomz.Simulation/Combat/CombatResolverSpecial.cs` |
| Environment | `Baboomz.Simulation/Simulation/GameSimulationEnvironment.cs`, `Baboomz.Simulation/Simulation/GameSimulationCrates.cs`, `Baboomz.Simulation/Simulation/GameSimulationHazards.cs` |
| Game Modes | `Baboomz.Simulation/GameModes/*.cs` |
| Match flow | `Baboomz.Simulation/Simulation/GameSimulation.cs`, `Baboomz.Simulation/State/GameState.cs` |
| Freeze/Buffs | `Baboomz.Simulation/Simulation/GameSimulationBuffs.cs`, `Baboomz.Simulation/Simulation/GameSimulationPlayer.cs` |
| Bosses | `Baboomz.Simulation/Boss/BossLogic.cs`, `Baboomz.Simulation/Boss/Boss{IronSentinel,SandWyrm,GlacialCannon,ForgeColossus,BaronCogsworth}.cs` |
| Progression | `Baboomz.Simulation/Progression/{AchievementTracker,ChallengeSystem,RankSystem,WeaponMastery}.cs` |
| Rendering | `Scripts/Runtime/PlayerRenderer.cs`, `Scripts/Runtime/ProjectileRenderer.cs`, `Scripts/Runtime/ExplosionRenderer.cs`, `Scripts/Terrain/GodotTerrainBridge.cs`, `Scripts/Runtime/ProceduralSprites.cs` |
| UI/HUD | `Scripts/UI/GameHUD.cs`, `Scripts/UI/UIBuilder.cs`, `Scripts/UI/GameHUDBuilder.cs`, `Scripts/Runtime/HUDBridge.cs` |
| Shaders | `Shaders/BitmapTerrain.gdshader`, `Shaders/SkyGradient.gdshader` |
| Config | `Baboomz.Simulation/Config/GameConfig.cs`, `Baboomz.Simulation/Config/GameConfigWeapons.cs`, `Baboomz.Simulation/Config/GameConfigSkills.cs` |
| Art assets | `Art/Characters/`, `Art/Terrain/`, `Art/VFX/`, `Art/UI/`, `Art/Backgrounds/` |

Use `Grep` and `Read` to find the specific code location.

- **For bugs**: identify the likely root cause in the source
- **For features**: identify where the new behavior would be added and what existing code it interacts with

### 3b. Check for duplicates

```bash
gh issue list --state open --label bug
gh issue list --state open --label enhancement
```

If a similar issue exists, comment on it instead of creating a duplicate.

---

## Bug Path

### 4b. Determine severity

| Priority | Criteria |
|----------|----------|
| `priority:high` | Crashes, data corruption, blocks gameplay |
| `priority:medium` | Incorrect behavior, balance issues, wrong logic |
| `priority:low` | Edge case, polish, minor visual issue |

### 5b. Create the bug issue

```bash
gh issue create \
  --title "[BUG] <clear title derived from description>" \
  --label "bug,priority:<level>" \
  --body "$(cat <<'ISSUE'
## Description
<User's original description, expanded with context>

## Location
`<file.cs>:<line>` — <function/method name>

## Root Cause Analysis
<What the code does wrong, based on reading the source>

## Impact
<What breaks in gameplay>

## Suggested Fix
<Specific code change recommendation>

## Steps to Reproduce
1. <Based on understanding of the code flow>

---
*Filed via `/report-bug`*
ISSUE
)"
```

---

## Feature Path

### 4f. Determine scope and priority

| Priority | Criteria |
|----------|----------|
| `priority:high` | Core gameplay improvement, highly requested, fills obvious gap |
| `priority:medium` | Nice-to-have gameplay addition, quality of life improvement |
| `priority:low` | Polish, cosmetic, edge-case convenience |

| Scope | Criteria |
|-------|----------|
| **small** | 1 file change, config tweak, add to existing system |
| **medium** | 2-3 files, new behavior within existing architecture |
| **large** | New system, new state fields, multiple files + tests |

### 5f. Create the feature issue

```bash
# Add ,visual to labels if this is a rendering/art/sprite/shader issue
gh issue create \
  --title "[FEAT] <feature name> — <one-line pitch>" \
  --label "enhancement,priority:<level>" \
  --body "$(cat <<'ISSUE'
## Concept
<What is it and why is it valuable? User's description expanded with context.>

## Design

### Behavior
<How it works step by step>

### Stats (if weapon/skill)
| Stat | Value | Rationale |
|------|-------|-----------|
| ... | ... | ... |

## Integration Points
<Which existing files/systems this touches, based on reading the code>
- `<file.cs>` — <what changes here>

## Balance Fit (if gameplay)
<Where it sits relative to existing weapons/skills/systems>

## Implementation Notes
- Estimated scope: small/medium/large
- New structs/flags needed: <list or "none">
- Test coverage: <what tests should be added>

---
*Filed via `/report-bug`*
ISSUE
)"
```

**For visual issues**, add `,visual` to the labels and mention verification via `mcp__godot__capture_screenshot`.

---

### 6. Confirm to user

**For bugs:**
```
Filed: #N — [BUG] title
Priority: high/medium/low
Location: file.cs:line
```

**For features:**
```
Filed: #N — [FEAT] title
Priority: high/medium/low
Scope: small/medium/large
Integration: file1.cs, file2.cs
Labels: enhancement[, visual]
```

## Rules

- **Always read the code** before filing — don't guess at locations or integration points
- **One issue per invocation** — if the description implies multiple items, file the primary one and mention the others
- **Don't fix bugs or implement features** — just file the issue. The `/dev` session will pick it up.
- **Deduplicate** — check open issues first. If similar, comment on the existing one.
- **Features need integration analysis** — always identify which files the feature would touch and how it fits with existing systems
- **Balance context for gameplay features** — if proposing a weapon/skill/mechanic, compare against existing ones
- **Visual label rule** — any issue mentioning sprites, shaders, animations, parallax, explosion effects, terrain textures, or UI art gets the `visual` label
