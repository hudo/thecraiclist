# Plan: Implement 8 Custom Slash Commands

## Context
The project has no custom Claude Code commands. We'll create 8 slash commands as `.claude/commands/<name>.md` files to streamline common development workflows. These are user-invocable via `/<name>`.

## Files to Create

All under `.claude/commands/`:

### 1. `build.md` — Build & Validate
- Runs `dotnet build src/src.slnx`
- Analyzes output for errors/warnings
- Reports summary with actionable next steps
- **allowed-tools:** `Bash(dotnet build:*)`

### 2. `test.md` — Run Tests
- Runs `dotnet test src/src.slnx`
- Parses xUnit output (pass/fail/skip counts)
- On failure: shows failing test names and error messages
- **allowed-tools:** `Bash(dotnet test:*)`

### 3. `deploy-check.md` — Pre-Deploy Validation
- Runs Release build: `dotnet build src/src.slnx -c Release`
- Runs publish: `dotnet publish src/IrlEventsWeb/IrlEventsWeb.csproj -c Release -o ./deploy-check-output`
- Checks for warnings/errors
- Cleans up temp output folder
- Reports deploy readiness
- **allowed-tools:** `Bash(dotnet build:*)`, `Bash(dotnet publish:*)`, `Bash(rm:*)`

### 4. `add-category.md` — Add New Event Category
- Takes argument: `<name> <emoji> <color>`
- Reads `HomeController.cs`
- Adds entry to `CategoryStyles` dictionary
- Validates hex color format and emoji
- **allowed-tools:** `Read`, `Edit`

### 5. `refresh-claude-md.md` — Sync CLAUDE.md with Reality
- Scans codebase structure (files, projects, test projects)
- Reads current CLAUDE.md and copilot-instructions.md
- Updates CLAUDE.md to reflect actual state (file paths, commands, architecture)
- Also updates copilot-instructions.md if needed
- **allowed-tools:** `Read`, `Edit`, `Glob`, `Grep`

### 6. `add-view.md` — Scaffold a Razor View
- Takes argument: `<controller-action-name>`
- Reads existing views for pattern reference (_Layout.cshtml, Index.cshtml)
- Creates new .cshtml file following project conventions (dark theme, CSS variables, responsive)
- Adds corresponding controller action if needed
- **allowed-tools:** `Read`, `Write`, `Edit`, `Glob`

### 7. `check-sheets.md` — Test Google Sheets Connection
- Reads GoogleSheetsReader.cs config (sheet ID, GID)
- Makes API call to Google Sheets using dev API key from launchSettings.json
- Reports: connection status, sheet name, column headers, row count
- Validates header format matches expected `HeaderToProperty` mapping
- **allowed-tools:** `Read`, `Bash(curl:*)`

### 8. `review-pr.md` — Pre-Push Code Review
- Runs `git diff` and `git status` to see changes
- Reviews against project conventions (from CLAUDE.md, copilot-instructions.md)
- Checks for: hardcoded secrets, missing null checks, style inconsistencies, security issues
- Reports findings with severity levels
- **allowed-tools:** `Read`, `Grep`, `Glob`, `Bash(git:*)`

## Files to Modify

### `.claude/settings.json`
Add permissions for the new commands' bash operations:
- `Bash(dotnet test:*)`
- `Bash(dotnet publish:*)`
- `Bash(rm -rf ./deploy-check-output)`
- `Bash(curl:*)`

## Verification
1. Run `/build` and confirm it builds and reports results
2. Run `/test` and confirm it runs tests and reports results
3. Run `/deploy-check` and confirm publish simulation works
4. Inspect each `.md` file for correct frontmatter format
