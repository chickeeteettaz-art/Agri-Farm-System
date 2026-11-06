using Agri_Web.MVC.Services;
using Agri_Web.MVC.Services.Interfaces;
using ClassLibrary.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Azure Functions HTTP services
builder.Services.AddHttpClient<ITelemetryService, TelemetryService>();

builder.Services.AddHttpClient<IIrrigationService, IrrigationService>();
var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();