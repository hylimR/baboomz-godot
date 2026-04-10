Pick up one small/polish GitHub enhancement issue. Oldest-first.

Dispatch the `todo-doer` agent using the Task tool:
- `subagent_type`: `"todo-doer"`
- `prompt`: "Pick up one small/polish task from GitHub following your workflow. Pick the oldest open `label:enhancement,priority:low` issue without `in-progress`."
- `mode`: `"bypassPermissions"`

After the agent returns, summarize its report to the user (issue number, title, files changed, PR URL).
