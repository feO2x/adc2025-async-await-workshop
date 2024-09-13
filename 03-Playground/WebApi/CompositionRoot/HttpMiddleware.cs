using Microsoft.AspNetCore.Builder;
using Serilog;
using WebApi.Contacts;
using WebApi.Orders;

namespace WebApi.CompositionRoot;

public static class HttpMiddleware
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.MapContactsEndpoints();
        app.MapCompleteOrder();
        app.MapHealthChecks("/");
        return app;
    }
}