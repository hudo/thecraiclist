# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

IrlEventsReader is a .NET 10.0 application for reading and displaying Irish cultural events sourced from a Google Sheets spreadsheet. It has two components:

- **`src/`** — ASP.NET Core web API (currently a minimal starter template). Production application folder
- **`pocs/`** - proof of concept and playground non-production code. Ignore this folder unless requested explicitely. 

## Build & Run Commands

```bash
# Web application
dotnet build src/
dotnet run --project src/
# Runs on http://localhost:5111 (or https://localhost:7290)
```

No test projects, linting tools, or CI/CD pipelines exist yet.

## Architecture

- All projects target `net10.0` with nullable reference types and implicit usings enabled
- The console reader fetches CSV from a hardcoded Google Sheets URL (sheet ID `1koOd6LfRzT54TmawJcuzJH0rbG218-1wsojIlx1zDtE`), parses with CsvHelper, and serializes to JSON
- Event data fields: category (`.`), event name, start date (DD/MM/YYYY), date added (`new`), venue, link
- Solution files: `src/src.slnx` (web), `pocs/console-reader/IrlEventsReader.sln` (POC)
