using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCacheManager(x => x.WithDictionaryHandle());

var app = builder.Build();

// Logging & Monitoring — loguje svaki zahtev koji prolazi kroz gateway
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Gateway: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
    logger.LogInformation("Gateway: odgovor {StatusCode} za {Path}", context.Response.StatusCode, context.Request.Path);
});

await app.UseOcelot();

app.Run();