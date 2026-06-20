using RegistrationService;
using RegistrationService.Data;
using RegistrationService.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServer<RegistrationDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHostedService<RegistrationService.HostedServices.RabbitMqConsumerHostedService>();
builder.Services.AddHostedService<RequestReplyHostedService>();
builder.Services.AddHostedService<EmailQueueHostedService>();

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