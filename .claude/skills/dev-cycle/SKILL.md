---
name: dev-cycle
description: Run a full game development cycle — QA audit → Dev fixes → Tech Lead review, all via GitHub issues. Use to trigger the complete team workflow in one shot.
---

# Full Development Cycle

Run the virtual game dev team in sequence, simulating a real sprint cycle. Everything flows through GitHub issues — no file-based backlogs.

## Cycle Steps

Execute these in order. Each step produces artifacts (issues or PRs) that feed the next.

### Step 0: Reset and triage

```bash
git checkout main && git pull origin main

# Snapshot the current state
gh issue list --state open --limit 20
gh pr list --state open --limit 10
```

Decide based on the state:
- **10+ open issues** → skip QA, go straight to Dev (catch-up mode)
- **3+ open PRs with `needs-review`** → start with Tech Lead (clear the review queue)
- **0 open issues and 0 open PRs** → run QA to seed work
- **Otherwise** → run all three phases (QA → Dev → Tech Lead)

### Step 1: QA audit

Invoke `/qa` or run inline following the QA skill:

1. Audit a rotating focus area (Weapons / Skills / AI / Physics / Bosses / Rendering)
2. File bugs to GitHub with `bug` label
3. Propose features with `enhancement` label
4. Check visual replication gaps and file with `visual` label
5. Return to main

Output: N new issues filed.

### Step 2: Dev fixes

Invoke `/dev` or run inline following the Dev skill:

1. Pick oldest open issue(s) without `in-progress` label
2. Claim each with `in-progress` label
3. Branch: `fix/N-short-desc` or `feat/N-short-desc`
4. Implement minimal change + regression test
5. Run the verification block:
   ```bash
   dotnet build Baboomz.csproj
   dotnet test Baboomz.Simulation.Tests/
   dotnet test Baboomz.E2E.Tests/
   ```
6. For `visual` issues, capture screenshots with `mcp__godot__capture_screenshot`
7. Commit, push, open PR with `needs-review` label
8. Return to main and loop for next issue

Output: N PRs opened.

### Step 3: Tech Lead review

Invoke `/tech-lead` or run inline following the Tech Lead skill:

1. For each `needs-review` PR: run checklist (size, purity, tests, diff minimality)
2. If visual PR, verify screenshot references exist in PR body
3. Check mergeability — dispatch rebase subagent for conflicts
4. Approve and squash-merge or request changes
5. Clean up merged branches

Output: N PRs merged / N changes-requested.

### Step 4: Final verification

Run the full verification loop on main:

```bash
git checkout main && git pull origin main
dotnet build Baboomz.csproj
dotnet test Baboomz.Simulation.Tests/Baboomz.Simulation.Tests.csproj
dotnet test Baboomz.E2E.Tests/Baboomz.E2E.Tests.csproj
```

Zero errors, all 1015 + 50 tests passing. If something broke after merge, file an urgent bug.

### Step 5: Report

Output a sprint summary:

```
## Dev Cycle Report — [date]

### Issues Filed (QA): N
- #X: [BUG] title
- #X: [FEAT] title
- #X: [VISUAL] title

### PRs Opened (Dev): N
- #X: fix: title → closes #Y
- #X: feat: title → closes #Y

### PRs Merged (Tech Lead): N
- #X: title
- #X: title → changes requested

### Code Quality
- Files over 300 lines: X
- Simulation purity: clean / X violations
- Test count: 1015 Simulation + 50 E2E (all passing)

### Next Cycle Priorities
1. <from oldest open issue>
2. <from open PR changes-requested>
3. <from proactive audit findings>
```

## How to Run

**Full cycle (one shot):**
```
/dev-cycle
```

**Automated recurring:**
```
/loop 1h /dev-cycle
```

**Individual roles:**
```
/qa          — just QA audit
/dev         — just Dev work
/tech-lead   — just PR review
/report-bug  — file a single bug or feature
```

## Rules

- **GitHub issues are the source of truth** — no `docs/bugs/` or `docs/todos/` file queues
- **Each phase is independent** — QA doesn't block on Dev, Dev doesn't block on Tech Lead; they loop
- **Verification is mandatory** — every phase that modifies code runs the full verification block
- **Backlog pressure controls flow**:
  - 10+ issues → skip QA this cycle
  - 5+ issues + 0 dev PRs → Dev spawns from QA
  - 3+ needs-review PRs → Tech Lead spawned first
- **One subagent per spawned role** — don't flood with parallel agents
- **Main must stay green** — if Final verification fails, file an urgent bug and halt the cycle
