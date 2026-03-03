namespace IrlEventsWeb.Models;

public class Event
{
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public string Venue { get; set; } = "";
    public string Link { get; set; } = "";
}
