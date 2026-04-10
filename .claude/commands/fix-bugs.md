Fix one open GitHub bug issue. Oldest-first, top-priority.

Dispatch the `bug-fixer` agent using the Task tool:
- `subagent_type`: `"bug-fixer"`
- `prompt`: "Fix one open bug from GitHub following your workflow. Pick the oldest open `label:bug` issue without `in-progress`."
- `mode`: `"bypassPermissions"`

After the agent returns, summarize its report to the user (bug number, root cause, files changed, PR URL).
