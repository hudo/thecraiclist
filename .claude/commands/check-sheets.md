---
allowed-tools: Read, Bash(curl:*)
---

# Test Google Sheets Connection

Verify the Google Sheets API connection and validate the data format.

## Steps

1. Read `src/IrlEventsWeb/Services/GoogleSheetsReader.cs` to find:
   - The sheet ID and GID
   - The expected header-to-property mapping (`HeaderToProperty`)
   - The API URL pattern used
2. Read `src/IrlEventsWeb/Properties/launchSettings.json` to find the dev API key
3. Make a test API call using curl to the Google Sheets API (using the CSV export URL or Sheets API endpoint)
4. Report:
   - Connection status (success/failure, HTTP status code)
   - Sheet name (if available)
   - Column headers found in the response
   - Row count (number of data rows)
5. Validate that the headers match the expected `HeaderToProperty` mapping
6. If there are mismatches, report which headers are missing or unexpected
7. If the connection fails, suggest troubleshooting steps (API key validity, sheet permissions, etc.)

**Note:** Do not expose the full API key in output. Show only the first 8 characters if needed for debugging.
