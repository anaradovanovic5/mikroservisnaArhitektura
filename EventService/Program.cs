using EventService;
using EventService.Data;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServer<EventDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<SagaOrchestrator>();
builder.Services.AddSingleton<IRabbitMqRequestReplyClient, RabbitMqRequestReplyClient>();
builder.Services.AddHostedService<EventService.HostedServices.OutboxMessagePublisher>();

// HTTP klijent za pozivanje LocationService SA POLLY mehanizmima
builder.Services.AddHttpClient("LocationService", client =>
{
    client.BaseAddress = new Uri("https://localhost:7002");
    client.Timeout = TimeSpan.FromSeconds(10); // TIMEOUT
})
.AddResilienceHandler("location-pipeline", pipeline =>
{
    // RETRY — pokušaj 3 puta, cekaj 2s izmeðu pokušaja
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        OnRetry = args =>
        {
            Console.WriteLine($"Retry #{args.AttemptNumber} - poziv ka LocationService nije uspeo.");
            return ValueTask.CompletedTask;
        }
    });

    // CIRCUIT BREAKER — prekini ako 5 od 10 poziva ne uspe, cekaj 30s
    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(30),
        OnOpened = args =>
        {
            Console.WriteLine("Circuit Breaker OTVOREN - LocationService nedostupan!");
            return ValueTask.CompletedTask;
        },
        OnClosed = args =>
        {
            Console.WriteLine("Circuit Breaker ZATVOREN - LocationService dostupan.");
            return ValueTask.CompletedTask;
        }
    });
});

builder.Services.AddHttpClient("RegistrationService", client =>
{
    client.BaseAddress = new Uri("https://localhost:7003");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();