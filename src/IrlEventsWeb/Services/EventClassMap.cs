using CsvHelper.Configuration;
using IrlEventsWeb.Models;

namespace IrlEventsWeb.Services;

public sealed class EventClassMap : ClassMap<Event>
{
    public EventClassMap()
    {
        Map(m => m.Category).Name(".");
        Map(m => m.Name).Name("event");
        Map(m => m.StartDate).Name("start date");
        Map(m => m.DateAdded).Name("new");
        Map(m => m.Venue).Name("venue");
        Map(m => m.Link).Name("link");
    }
}
