using System.Security.Claims;
using Barista.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Barista.Services;

// Keep staff sessions revocable when someone switches back to the client workspace.
public sealed class StaffSessions(IMemoryCache cache)
{
    public const string CookieName = "Barista.StaffSession";
    public const string ClaimType = "Barista.StaffSessionId";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public string Start()
    {
        var id = Guid.NewGuid().ToString("N");
        cache.Set(CacheKey(id), true, Lifetime);
        return id;
    }

    public bool IsActive(ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimType) is { } id && cache.TryGetValue(CacheKey(id), out _);

    public async Task EndAsync(HttpContext context)
    {
        var session = await context.AuthenticateAsync(StaffAccounts.Scheme);
        if (session.Principal?.FindFirstValue(ClaimType) is { } id)
            cache.Remove(CacheKey(id));

        await context.SignOutAsync(StaffAccounts.Scheme);
    }

    private static string CacheKey(string id) => $"{ClaimType}:{id}";
}
