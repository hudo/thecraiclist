---
allowed-tools: Read, Edit, Glob, Grep
---

# Sync CLAUDE.md with Reality

Scan the codebase and update CLAUDE.md (and copilot-instructions.md if present) to reflect the actual project state.

## Steps

1. Scan the codebase structure:
   - Find all `.csproj` and `.slnx`/`.sln` files
   - Find test projects (if any)
   - Find configuration files (launchSettings.json, appsettings.json, etc.)
   - Note key directories and their purpose
2. Read the current `CLAUDE.md`
3. Read `.github/copilot-instructions.md` if it exists
4. Compare documented state against actual state. Check for:
   - File paths that no longer exist or have moved
   - Missing projects or new projects not yet documented
   - Build/run commands that may have changed
   - Architecture details that are outdated
   - Target framework version accuracy
5. Update `CLAUDE.md` with corrections, preserving its overall structure
6. Update `.github/copilot-instructions.md` if it exists and needs changes
7. Report what was changed and why
