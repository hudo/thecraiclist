namespace IrlEventsWeb.Models;

public class Category
{
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required string Color { get; init; }
    public int TotalCount { get; set; }
    public List<Event> Events { get; set; } = [];
}
