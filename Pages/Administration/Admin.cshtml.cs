using Barista.Data;
using Barista.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Barista.Pages;

// Provides the staff views in Admin.cshtml and handles changes to the queue and menu.
[Authorize(AuthenticationSchemes = StaffAccounts.Scheme, Roles = "Staff")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminModel(OrderWorkspace workspace) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Section { get; set; }

    public IReadOnlyList<OrderTicket> Tickets => workspace.GetTickets(activeOnly: true);

    // Use local midnight boundaries for today's figures.
    public IReadOnlyList<OrderTicket> DailyTickets()
    {
        var today = DateTime.Today;
        return workspace.GetTickets(
            since: new DateTimeOffset(today), until: new DateTimeOffset(today.AddDays(1)));
    }

    public bool IsAvailable(MenuKey key) => workspace.IsAvailable(key);

    // Save the visibility change and return to the staff menu.
    public IActionResult OnPostAvailability(MenuKey item, bool available)
    {
        if (!ModelState.IsValid || MenuCatalog.Find(item) is null)
            return BadRequest();

        workspace.SetAvailability(item, available);
        return RedirectToPage("/Administration/Admin", new { section = "menu" });
    }

    // Mark the ticket ready for pickup and refresh the active queue.
    public IActionResult OnPostReady(int number)
    {
        if (!ModelState.IsValid || number < 1)
            return BadRequest();

        workspace.MarkReady(number);
        return RedirectToPage("/Administration/Admin");
    }

    // Record the handoff. Completed tickets leave the active queue.
    public IActionResult OnPostComplete(int number)
    {
        if (!ModelState.IsValid || number < 1)
            return BadRequest();

        workspace.Complete(number);
        return RedirectToPage("/Administration/Admin");
    }
}
