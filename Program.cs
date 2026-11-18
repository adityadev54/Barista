using System.Threading.RateLimiting;
using Barista.Data;
using Barista.Domain;
using Barista.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
// The catalog is the home page. Its Razor file also keeps the /Menu URL.
builder.Services.AddRazorPages(options =>
    options.Conventions.AddPageRoute("/Ordering/Menu", ""));
// Staff and clients have separate cookies. Switching roles requires signing out.
builder.Services.AddAuthentication("BaristaSession")
    .AddPolicyScheme("BaristaSession", "Barista session", options =>
    {
        options.ForwardDefaultSelector = context =>
            IsStaffPath(context.Request.Path) ? StaffAccounts.Scheme : ClientAccounts.Scheme;
    })
    .AddCookie(StaffAccounts.Scheme, options =>
    {
        options.LoginPath = "/Staff";
        options.AccessDeniedPath = "/Staff";
        options.Cookie.Name = StaffSessions.CookieName;
        options.ExpireTimeSpan = StaffSessions.Lifetime;
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = context =>
        {
            var sessions = context.HttpContext.RequestServices.GetRequiredService<StaffSessions>();
            if (!sessions.IsActive(context.Principal))
                context.RejectPrincipal();

            return Task.CompletedTask;
        };
    })
    .AddCookie(ClientAccounts.Scheme, options =>
    {
        options.LoginPath = "/Client";
        options.Cookie.Name = "Barista.Client";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<StaffSessions>();
// Limit repeated sign-in attempts from the same address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AccountSignIn", context =>
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            context.Request.Query["handler"] == "SignOut")
        {
            return RateLimitPartition.GetNoLimiter("account-read-or-sign-out");
        }

        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partition = IsStaffPath(context.Request.Path) ? $"staff:{address}" : $"client:{address}";
        return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new()
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});
// Discounts run in this order. Happy Hour uses the total after the reusable cup discount.
builder.Services.AddSingleton<IDiscountRule, ReusableCupRule>();
builder.Services.AddSingleton<IDiscountRule, HappyHourRule>();
builder.Services.AddSingleton<OrderPricingService>();
// Use the local SQLite file unless a different path is configured.
var databasePath = builder.Configuration["Database:Path"] ?? "App_Data/barista.sqlite";
var database = new BaristaDatabase(
    Path.GetFullPath(databasePath, builder.Environment.ContentRootPath));
builder.Services.AddSingleton(database);
builder.Services.AddSingleton<StaffAccounts>();
builder.Services.AddSingleton<ClientAccounts>();
builder.Services.AddSingleton<OrderWorkspace>();

var app = builder.Build();
app.UseStaticFiles();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var hasStaffCookie = context.Request.Cookies.ContainsKey(StaffSessions.CookieName);
    var sessions = context.RequestServices.GetRequiredService<StaffSessions>();

    // A client must sign out before opening a staff page or sending a staff action.
    if (IsStaffPath(path) && (await context.AuthenticateAsync(ClientAccounts.Scheme)).Succeeded)
    {
        if (hasStaffCookie)
            await sessions.EndAsync(context);

        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            context.Response.Redirect(context.Request.PathBase.Add("/").Value!);
        else
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

        return;
    }

    // A bookmark or another tab can also take staff back to the client workspace.
    if (hasStaffCookie && IsClientPage(path))
        await sessions.EndAsync(context);

    await next(context);
});
app.UseRateLimiter();
app.UseAuthorization();
app.MapRazorPages();
app.Run();

static bool IsStaffPath(PathString path) =>
    path.StartsWithSegments("/Admin") || path.StartsWithSegments("/Staff");

static bool IsClientPage(PathString path) =>
    path.Value?.TrimEnd('/').ToUpperInvariant() is "" or "/MENU" or "/ORDER" or "/MYORDERS" or "/CLIENT";
