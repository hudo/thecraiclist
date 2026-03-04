---
allowed-tools: Read, Edit
---

# Add New Event Category

Add a new event category to the CategoryStyles dictionary in HomeController.cs.

## Arguments

Provide: `<name> <emoji> <color>`

Example: `/add-category Theatre 🎭 #8B5CF6`

## Steps

1. Validate the arguments:
   - `name` must be a non-empty string
   - `emoji` must be a single emoji character
   - `color` must be a valid hex color (e.g., `#FF5733` or `#F00`)
2. Read `src/IrlEventsWeb/Controllers/HomeController.cs`
3. Find the `CategoryStyles` dictionary
4. Check that the category doesn't already exist (case-insensitive)
5. Add the new entry following the existing format and style
6. Report what was added and confirm the edit

If arguments are missing or invalid, explain the expected format and ask for correction.
