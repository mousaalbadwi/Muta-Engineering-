using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using System.Globalization;
using AspNet.Security.OAuth.GitHub;

var builder = WebApplication.CreateBuilder(args);

// -------- Services --------
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// -------- Authentication (Cookies + OAuth *conditionally*) --------
var auth = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});
auth.AddCookie(); // "Cookies"

// اقرأ المفاتيح من الإعدادات
string? gId = builder.Configuration["Authentication:Google:ClientId"];
string? gSec = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(gId) && !string.IsNullOrWhiteSpace(gSec))
{
    auth.AddGoogle(opt =>
    {
        opt.ClientId = gId!;
        opt.ClientSecret = gSec!;
        opt.SaveTokens = true;
    });
}

string? fbId = builder.Configuration["Authentication:Facebook:AppId"];
string? fbSec = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(fbId) && !string.IsNullOrWhiteSpace(fbSec))
{
    auth.AddFacebook(opt =>
    {
        opt.AppId = fbId!;
        opt.AppSecret = fbSec!;
        opt.SaveTokens = true;
    });
}

string? ghId = builder.Configuration["Authentication:GitHub:ClientId"];
string? ghSec = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(ghId) && !string.IsNullOrWhiteSpace(ghSec))
{
    auth.AddGitHub(opt =>
    {
        opt.ClientId = ghId!;
        opt.ClientSecret = ghSec!;
        opt.Scope.Add("user:email");
        opt.SaveTokens = true;
    });
}

var app = builder.Build();

// -------- Pipeline --------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// اللغات: عربي افتراضي + إنجليزي
var supportedCultures = new[] { new CultureInfo("ar-JO"), new CultureInfo("en-US") };
var locOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("ar-JO"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
// تبديل اللغة عبر QueryString: ?culture=ar-JO&ui-culture=ar-JO
locOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
app.UseRequestLocalization(locOptions);

app.UseRouting();
app.UseSession();
app.UseAuthentication();   // مهم
app.UseAuthorization();

// Static Web Assets (قالبك يدعمها)
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
   pattern: "{controller=Welcome}/{action=Index}/{id?}")
    .WithStaticAssets();

await DbSeeder.SeedAsync(app.Services);

app.Run();
