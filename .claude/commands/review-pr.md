---
allowed-tools: Read, Grep, Glob, Bash(git:*)
---

# Pre-Push Code Review

Review all uncommitted and staged changes against project conventions before pushing.

## Steps

1. Run `git status` to see what files are modified, staged, and untracked
2. Run `git diff` to see unstaged changes
3. Run `git diff --cached` to see staged changes
4. Read `CLAUDE.md` and `.github/copilot-instructions.md` for project conventions
5. Review all changes checking for:

   **Security (Critical)**
   - Hardcoded secrets, API keys, or connection strings
   - Sensitive data in comments or log statements

   **Code Quality (Warning)**
   - Missing null checks on nullable reference types
   - Unused imports or variables introduced
   - TODO/HACK comments without issue references

   **Style (Info)**
   - Inconsistencies with existing code style
   - Naming convention violations
   - Formatting issues

   **Architecture (Warning)**
   - Changes that don't align with documented architecture
   - New dependencies that may not be needed

6. Report findings grouped by severity:
   - **CRITICAL** — Must fix before pushing
   - **WARNING** — Should fix, may cause issues
   - **INFO** — Consider fixing for consistency
7. If no issues found, confirm the changes look good to push
