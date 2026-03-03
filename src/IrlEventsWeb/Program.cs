using IrlEventsWeb.Services;
using IrlEventsWeb.Workers;

namespace IrlEventsWeb;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IGoogleSheetsReader, GoogleSheetsReader>();
        builder.Services.AddHostedService<EventsRefreshWorker>();
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();
        
        app.MapControllerRoute(
            "category", 
            "category/{name}", 
            new { controller = "Home", action = "Category" });

        app.MapDefaultControllerRoute();

        app.Run();
    }
}
