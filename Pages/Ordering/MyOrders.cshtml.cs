using Barista.Data;
using Barista.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Barista.Pages;

// Loads only the tickets linked to the signed-in client for MyOrders.cshtml.
[Authorize(AuthenticationSchemes = ClientAccounts.Scheme)]
public sealed class MyOrdersModel(OrderWorkspace workspace) : PageModel
{
    public IReadOnlyList<OrderTicket> Tickets =>
        ClientAccounts.GetId(User) is { } clientId ? workspace.GetTickets(clientId) : [];
}
