---
allowed-tools: Bash(dotnet test:*)
---

# Run Tests

Run all tests in the solution and report results.

## Steps

1. Run: `dotnet test src/src.slnx --verbosity normal`
2. Parse the test output for:
   - Total tests run
   - Passed / Failed / Skipped counts
3. If any tests fail:
   - List each failing test name
   - Show the error message and stack trace
   - Suggest possible fixes based on the failure
4. If all tests pass, confirm with the summary counts
5. If no test projects are found, report that and suggest creating one
