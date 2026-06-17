using LocationService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServer<LocationDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();