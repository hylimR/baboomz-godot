---
name: dev
description: Developer role — picks up multiple GitHub issues, fixes them on branches, and opens PRs. Batches work per session. Run in its own Claude session with /loop.
---

# Developer

Pick up open GitHub issues, fix them on branches, and open PRs. Work on **multiple issues per session** — loop until the backlog is clear or you hit a blocker.

## Usage

```
/loop 5m /dev
```

## Per-Cycle Workflow

### 0. Reset to main

```bash
git checkout main && git pull origin main
```

### 1. Check for work

```bash
# Check if any of our PRs had changes requested — fix those first
gh pr list --state open --author @me --label changes-requested

# Otherwise list open issues
gh issue list --label bug,enhancement --state open --sort created --limit 10
```

**Priority:**
1. Fix PRs with `changes-requested` (re-review pending)
2. Pick oldest open issues without `in-progress` label
3. If no issues, report idle and skip

### 2. Handle changes-requested PRs

If a PR has `changes-requested`:

```bash
gh pr view N                    # read the review comments
gh pr diff N                    # see current diff
git checkout fix/N-short-desc   # switch to the branch
```

- Read the Tech Lead's feedback
- Fix the issues
- Run the **verification block** (see §5)
- Commit: `fix: address review feedback (#N)`
- Push: `git push`
- Remove `changes-requested`, ensure `needs-review` label
- Return to main
- **Then continue to the next issue**

### 3. Batch issue selection

Assess the open issues and decide how many to tackle this cycle:

| Issue complexity | Strategy |
|-----------------|----------|
| **Extremely large** (new system, 5+ files, architectural) | Work on this ONE issue only for the entire cycle |
| **Normal** (1-3 files, config change, localized fix) | Batch multiple — keep going until backlog is clear or cycle ends |

For each issue you pick up:

```bash
# Claim it
gh issue edit N --add-label in-progress

# Create branch from latest main
git checkout main && git pull origin main
git checkout -b fix/N-short-desc
# or: git checkout -b feat/N-short-desc (for enhancements)
```

### 4. Implement the fix

1. Read the issue body for location, impact, suggested fix
2. Read the relevant source files
3. Implement the fix with **minimal changes**
4. Add a regression test in `Baboomz.Simulation.Tests/` (unit) or `Baboomz.E2E.Tests/` (integration)
5. Follow SOLID: max 300 lines per file, no `using Godot` in `Baboomz.Simulation/`

### 5. Verification block

**Every commit that touches code must pass this block.** Run these in order:

```bash
# Compile
dotnet build Baboomz.csproj

# Unit tests (1015 tests, ~3s)
dotnet test Baboomz.Simulation.Tests/Baboomz.Simulation.Tests.csproj

# E2E tests (50 tests, ~1s)
dotnet test Baboomz.E2E.Tests/Baboomz.E2E.Tests.csproj
```

**Do NOT create a PR if the build or any test fails.** Fix until green.

**For issues with the `visual` label**, additionally capture screenshots:

```
mcp__godot__capture_screenshot  projectPath=D:/Workspace/BaboomzGodot  scene=Scenes/Main.tscn     outputPath=/tmp/verify-main.png
mcp__godot__capture_screenshot  projectPath=D:/Workspace/BaboomzGodot  scene=Scenes/MainMenu.tscn outputPath=/tmp/verify-menu.png
```

Attach the screenshot paths in the PR body so the Tech Lead can review visually.

### 6. Commit and push

```bash
# Stage specific files (never git add -A)
git add Baboomz.Simulation/... Baboomz.Simulation.Tests/...

# Commit with issue reference
git commit -m "fix: description (#N)"

# Push
git push -u origin fix/N-short-desc
```

### 7. Create PR

```bash
gh pr create \
  --title "fix: description" \
  --label "needs-review" \
  --body "$(cat <<'EOF'
Closes #N

## Summary
- What was changed and why

## Verification
- [x] `dotnet build Baboomz.csproj` — clean
- [x] `dotnet test Baboomz.Simulation.Tests/` — 1015 passing
- [x] `dotnet test Baboomz.E2E.Tests/` — 50 passing
- [ ] Regression test added

## Visual (if applicable)
- Screenshot: /tmp/verify-main.png
- Screenshot: /tmp/verify-menu.png

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

### 8. Loop back for next issue

```bash
git checkout main && git pull origin main
```

**If the issue you just completed was normal-sized, go back to step 3 and pick the next issue.** Continue until:
- No more open issues
- You hit an extremely large issue (do it, then stop)
- Cycle time limit reached

### 9. Spawn helpers if needed

Before wrapping up, check if other roles need to run:

**Spawn QA if no issues to work on:**

```bash
gh issue list --label bug,enhancement --state open --limit 1
```

If there are **0 open issues** and you finished everything, spawn a QA subagent to generate more work:

```
Agent(subagent_type="general-purpose", mode="bypassPermissions", run_in_background=true,
  prompt="Run the /qa skill: audit the Baboomz Godot simulation code, analyze balance, check feature completeness, and file GitHub issues. Follow the skill instructions exactly.")
```

**Spawn Tech Lead if unreviewed PRs are piling up:**

```bash
gh pr list --state open --label needs-review
```

If there are **3+ PRs with `needs-review`**, spawn a Tech Lead subagent to review and merge them:

```
Agent(subagent_type="general-purpose", mode="bypassPermissions", run_in_background=true,
  prompt="Run the /tech-lead skill: review all open PRs with needs-review label, apply the code quality checklist, approve and merge good PRs, request changes on bad ones. Follow the skill instructions exactly.")
```

### 10. Cleanup merged branches (end of cycle)

```bash
git fetch --prune
git branch --merged main | grep -v main | xargs -r git branch -d
```

### 11. Report

```
## Dev Report — [date]
- Issues completed: N
  - #X: title — PR #Y
  - #X: title — PR #Y
- Issues skipped: N (reason)
- Status: all done / blocked on #X / idle
- Tests: 1015 Simulation + 50 E2E passing
```

## Rules

- **Multiple issues per cycle** — keep going until done or blocked
- **Exception: extremely large tasks** — if a single issue is architectural / 5+ files / new system, focus on just that one
- **Never merge your own PR** — that's the Tech Lead's job
- **Tests must pass** — no PR with failing tests, no PR with failing `dotnet build`
- **Minimal changes** — fix the bug, add the test, nothing else
- **Reference issues** — every commit and PR links to the issue number
- **Don't touch issues labeled `in-progress`** by someone else
- **Return to main between issues** — always branch from fresh main
- **Visual issues require screenshots** — PRs with `visual` label must include capture_screenshot output in the body
- **Simulation purity** — if your change touches `Baboomz.Simulation/`, verify no `using Godot` was introduced
