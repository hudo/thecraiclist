using IrlEventsWeb.Models;
using IrlEventsWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace IrlEventsWeb.Controllers;

public class HomeController : Controller
{
    private static readonly Dictionary<string, (string Icon, string Color)> CategoryStyles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["music"]      = ("\U0001F3B5", "#e74c3c"),
            ["cinema"]     = ("\U0001F3AC", "#9b59b6"),
            ["exhibition"] = ("\U0001F5BC\uFE0F", "#f39c12"),
            ["festival"]   = ("\U0001F389", "#1abc9c"),
            ["event"]      = ("\U0001F3AA", "#e67e22"),
            ["theatre"]    = ("\U0001F3AD", "#e84393"),
            ["comedy"]     = ("\U0001F602", "#00b894"),
            ["convention"] = ("\U0001F3AE", "#3498db"),
        };

    private static readonly (string Icon, string Color) DefaultStyle = ("\U0001F4CC", "#7f8c8d");

    private readonly IGoogleSheetsReader _sheetsReader;

    public HomeController(IGoogleSheetsReader sheetsReader)
    {
        _sheetsReader = sheetsReader;
    }

    public async Task<IActionResult> Category(string name)
    {
        if (string.IsNullOrEmpty(name))
            return RedirectToAction(nameof(Index));

        var events = await _sheetsReader.GetCachedEventsAsync();

        var group = events
            .Where(x => x.StartDate > DateTime.UtcNow.AddDays(-7)) // Show only upcoming and recent events
            .Where(e => e.Category.Equals(name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.StartDate)
            .ToList();

        if (group.Count == 0)
            return RedirectToAction(nameof(Index));

        var (icon, color) = CategoryStyles.GetValueOrDefault(name, DefaultStyle);
        var category = new Category
        {
            Name = char.ToUpper(name[0]) + name[1..].ToLower(),
            Icon = icon,
            Color = color,
            TotalCount = group.Count,
            Events = group.Take(200).ToList(),
        };

        return View(category);
    }

    public async Task<IActionResult> Events(CancellationToken ct)
    {
        var events = await _sheetsReader.FetchEventsAsync(ct);
        return Json(events);
    }

    public async Task<IActionResult> Index()
    {
        var events = await _sheetsReader.GetCachedEventsAsync();

        var categories = events
            .Where(x => x.StartDate > DateTime.UtcNow.AddDays(-7)) // Show only upcoming and recent events
            .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var (icon, color) = CategoryStyles.GetValueOrDefault(g.Key, DefaultStyle);
                var ordered = g.OrderBy(x => x.StartDate).ToList();
                return new Category
                {
                    Name = char.ToUpper(g.Key[0]) + g.Key[1..].ToLower(),
                    Icon = icon,
                    Color = color,
                    TotalCount = ordered.Count,
                    Events = ordered.Take(10).ToList(),
                };
            })
            .ToList();

        var viewModel = new HomeViewModel { Categories = categories };
        return View(viewModel);
    }
}
