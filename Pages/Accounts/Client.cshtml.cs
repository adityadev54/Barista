using System.Security.Claims;
using Barista.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace Barista.Pages;

// Handles registration, sign-in, and sign-out for the client form in Client.cshtml.
[EnableRateLimiting("AccountSignIn")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ClientModel(ClientAccounts accounts) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Create { get; set; }

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var session = await HttpContext.AuthenticateAsync(ClientAccounts.Scheme);
        return session.Succeeded ? Continue() : Page();
    }

    // The same form signs in or registers, depending on which mode it is in.
    public async Task<IActionResult> OnPostAsync()
    {
        if (Username?.Trim() is not { Length: >= 3 and <= 64 } username ||
            Password is not { Length: >= 12 and <= 256 } password)
        {
            return ShowError(Create
                ? "Use a 3–64 character username and a 12–256 character password."
                : "Username or password is incorrect.");
        }

        if (Create && password != ConfirmPassword)
            return ShowError("Passwords do not match.");

        var account = Create
            ? accounts.Register(username, password)
            : accounts.Authenticate(username, password);
        if (account is null)
            return ShowError(Create ? "That username is taken." : "Username or password is incorrect.");

        var identity = new ClaimsIdentity(
            [
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Name, account.Username)
            ],
            ClientAccounts.Scheme);
        await HttpContext.SignInAsync(ClientAccounts.Scheme, new ClaimsPrincipal(identity));
        return Continue();
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await HttpContext.SignOutAsync(ClientAccounts.Scheme);
        return RedirectToPage("/Accounts/Client");
    }

    // Return to the requested page only when it belongs to Barista.
    private IActionResult Continue() => Url.IsLocalUrl(ReturnUrl)
        ? LocalRedirect(ReturnUrl)
        : RedirectToPage("/Ordering/Menu");

    private IActionResult ShowError(string message)
    {
        Password = null;
        ConfirmPassword = null;
        ModelState.Remove(nameof(Password));
        ModelState.Remove(nameof(ConfirmPassword));
        ModelState.AddModelError(string.Empty, message);
        return Page();
    }
}
