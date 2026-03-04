---
allowed-tools: Bash(dotnet build:*)
---

# Build & Validate

Run the solution build and analyze the output.

## Steps

1. Run: `dotnet build src/src.slnx`
2. Analyze the build output for errors and warnings
3. Report a summary:
   - Build result (success/failure)
   - Number of errors and warnings (if any)
   - For each error/warning: file, line number, code, and message
4. If there are errors, suggest actionable next steps to fix them
5. If the build succeeds with no warnings, confirm the project is clean
