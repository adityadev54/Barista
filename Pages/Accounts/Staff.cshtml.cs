using System.Net;
using System.Security.Claims;
using Barista.Data;
using Barista.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace Barista.Pages;

// Handles staff sign-in and the first local account setup for Staff.cshtml.
[EnableRateLimiting("AccountSignIn")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StaffModel(StaffAccounts accounts, StaffSessions sessions) : PageModel
{
    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public bool SetupRequired { get; private set; }
    public bool CanSetUp => SetupRequired && IsLocalRequest();

    public void OnGet() => SetupRequired = !accounts.Exists();

    public async Task<IActionResult> OnPostAsync()
    {
        SetupRequired = !accounts.Exists();
        if (SetupRequired)
            return Page();

        if (Username?.Trim() is { Length: >= 3 and <= 64 } username &&
            Password is { Length: >= 12 and <= 256 } password &&
            accounts.Authenticate(username, password) is { } account)
        {
            return await SignInStaffAsync(account);
        }

        return ShowError("Username or password is incorrect.");
    }

    // First-run setup is allowed only from localhost before the staff account exists.
    public async Task<IActionResult> OnPostSetupAsync()
    {
        SetupRequired = !accounts.Exists();
        if (!CanSetUp)
            return RedirectToPage("/Accounts/Staff");

        if (Username?.Trim() is not { Length: >= 3 and <= 64 } username ||
            Password is not { Length: >= 12 and <= 256 } password)
        {
            return ShowError("Use a 3–64 character username and a 12–256 character password.");
        }

        if (password != ConfirmPassword)
            return ShowError("Passwords do not match.");

        var account = new StaffAccount(username);
        if (!accounts.TryCreate(account, password))
            return RedirectToPage("/Accounts/Staff");

        return await SignInStaffAsync(account);
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await sessions.EndAsync(HttpContext);
        return RedirectToPage("/Accounts/Client");
    }

    // The staff role gives access to the admin page and its post handlers.
    private async Task<IActionResult> SignInStaffAsync(StaffAccount account)
    {
        await sessions.EndAsync(HttpContext);
        var identity = new ClaimsIdentity(
            [
                new(ClaimTypes.Name, account.Username),
                new(ClaimTypes.Role, "Staff"),
                new(StaffSessions.ClaimType, sessions.Start())
            ],
            StaffAccounts.Scheme);
        await HttpContext.SignInAsync(StaffAccounts.Scheme, new ClaimsPrincipal(identity));
        return RedirectToPage("/Administration/Admin");
    }

    private IActionResult ShowError(string message)
    {
        Password = null;
        ConfirmPassword = null;
        ModelState.Remove(nameof(Password));
        ModelState.Remove(nameof(ConfirmPassword));
        ModelState.AddModelError(string.Empty, message);
        return Page();
    }

    private bool IsLocalRequest()
    {
        var remote = HttpContext.Connection.RemoteIpAddress;
        if (remote is null || !IPAddress.IsLoopback(remote))
            return false;

        var host = Request.Host.Host.Trim('[', ']');
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
    }
}
