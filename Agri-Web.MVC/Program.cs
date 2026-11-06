using Agri_Web.MVC.Services;
using Agri_Web.MVC.Services.Interfaces;
using ClassLibrary.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DATABASE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// PASSWORD HASHING
builder.Services.AddScoped<IPasswordHasher<Farmer>,
    PasswordHasher<Farmer>>();

builder.Services.AddScoped<IPasswordHasher<Agronomist>,
    PasswordHasher<Agronomist>>();

builder.Services.AddScoped<IPasswordHasher<Admin>,
    PasswordHasher<Admin>>();

// AUTHENTICATION
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// AZURE BLOB STORAGE
builder.Services.AddScoped<
    IBlobStorageService,
    BlobStorageService>();

// AZURE FUNCTIONS
builder.Services.AddHttpClient<
    ITelemetryService,
    TelemetryService>();

builder.Services.AddHttpClient<
    IIrrigationService,
    IrrigationService>();

builder.Services.AddHttpClient<
    IProcessIrrigationService,
    ProcessIrrigationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();