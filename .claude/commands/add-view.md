---
allowed-tools: Read, Write, Edit, Glob
---

# Scaffold a Razor View

Create a new Razor view following the project's conventions.

## Arguments

Provide: `<controller-action-name>`

Example: `/add-view About` creates `Views/Home/About.cshtml` and adds an `About()` action to `HomeController`.

## Steps

1. Read existing views for pattern reference:
   - `src/IrlEventsWeb/Views/Shared/_Layout.cshtml` for layout structure
   - `src/IrlEventsWeb/Views/Home/Index.cshtml` for content conventions
2. Identify the project's conventions:
   - Dark theme with CSS variables
   - Responsive design patterns
   - Any shared partials or sections used
3. Create the new `.cshtml` file at `src/IrlEventsWeb/Views/Home/<name>.cshtml`
   - Follow the existing dark theme and responsive design conventions
   - Include proper `@{ ViewData["Title"] = "<name>"; }` header
   - Use CSS variables from the layout
4. Read `src/IrlEventsWeb/Controllers/HomeController.cs`
5. Check if a controller action already exists for this view
6. If not, add an `IActionResult` action method returning `View()`
7. Report what was created and any manual steps needed (e.g., adding nav links)
