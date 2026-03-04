---
allowed-tools: Bash(dotnet build:*), Bash(dotnet publish:*), Bash(rm:*)
---

# Pre-Deploy Validation

Simulate a production deployment to catch issues before pushing.

## Steps

1. Run Release build: `dotnet build src/src.slnx -c Release`
2. If the build fails, report errors and stop
3. Run publish: `dotnet publish src/IrlEventsWeb/IrlEventsWeb.csproj -c Release -o ./deploy-check-output`
4. Check the publish output for warnings or errors
5. Clean up: `rm -rf ./deploy-check-output`
6. Report deploy readiness:
   - Build result (Release config)
   - Publish result
   - Any warnings that should be addressed before deploying
   - Final verdict: READY or NOT READY for deployment
