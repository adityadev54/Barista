using System.ComponentModel.DataAnnotations;
using Barista.Data;
using Barista.Domain;
using Barista.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Barista.Pages;

// Order.cshtml shows the form. This page model handles initial pricing, live quotes, and checkout.
public sealed class OrderInput
{
    public MenuKey Item { get; set; }
    public CupSize Size { get; set; } = CupSize.Medium;
    public bool OatMilk { get; set; }
    public bool ColdFoam { get; set; }
    public bool Caramel { get; set; }
    public bool ReusableCup { get; set; }
    public bool HappyHour { get; set; }

    [Range(0, Beverage.MaxExtraShots, ErrorMessage = "Choose between {1} and {2} extra shots.")]
    public int Shots { get; set; }
}

public sealed class OrderModel(
    OrderPricingService pricing,
    OrderWorkspace workspace) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public OrderInput Input { get; set; } = new();

    // The hidden request ID lets the database detect a repeated checkout.
    [BindProperty]
    public Guid RequestId { get; set; } = Guid.NewGuid();

    public MenuEntry? Entry => MenuCatalog.Find(Input.Item);
    public bool Available => Entry is { } entry && workspace.IsAvailable(entry.Key);
    public OrderReceipt? Receipt { get; private set; }
    public OrderTicket? Ticket { get; private set; }
    public string? ReceiptError { get; private set; }

    // Open the selected item and show its starting receipt.
    public IActionResult OnGet()
    {
        if (Entry is null)
            return RedirectToPage("/Ordering/Menu");

        CalculateReceipt();
        return Page();
    }

    // Return just the receipt markup when a client changes a selection.
    public IActionResult OnPostQuote()
    {
        CalculateReceipt();
        return Partial("/Pages/Shared/Receipts/_OrderReceipt.cshtml", this);
    }

    // Refresh the whole page when JavaScript is unavailable.
    public void OnPostRefresh() => CalculateReceipt();

    // Reprice before saving. The browser's preview is only for display.
    public void OnPost()
    {
        CalculateReceipt();
        if (Entry is not { } entry || Receipt is not { } receipt)
            return;

        if (RequestId == Guid.Empty)
        {
            ReceiptError = "Reload the order before placing it.";
            return;
        }

        try
        {
            var description = entry.IsDrink ? $"{entry.Name} · {Input.Size}" : entry.Name;
            var clientId = ClientAccounts.GetId(User);
            Ticket = workspace.Place(RequestId, entry.Key, description, receipt, clientId);
            Receipt = Ticket.Receipt;
        }
        catch (OrderValidationException error)
        {
            Receipt = null;
            ReceiptError = error.Message;
        }
    }

    // Turn the posted form values into a domain order, then price it.
    private void CalculateReceipt()
    {
        if (!ModelState.IsValid)
        {
            ReceiptError = ModelState.Values.SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Check your selections and try again.";
            return;
        }

        if (Entry is not { } entry || !Available)
        {
            ReceiptError = "This item is currently unavailable.";
            return;
        }

        try
        {
            var order = new OrderBuilder(entry.CreateItem(Input.Size))
                .WithPromotions(entry.IsDrink && Input.ReusableCup, Input.HappyHour);

            if (entry.IsDrink)
                AddExtras(order);

            Receipt = pricing.Price(order.Build());
        }
        catch (OrderValidationException error)
        {
            ReceiptError = error.Message;
        }
    }

    private void AddExtras(OrderBuilder order)
    {
        if (Input.OatMilk)
            order.Add(new OatMilkSwap());

        for (var shot = 0; shot < Input.Shots; shot++)
            order.Add(new ExtraShot());

        if (Input.ColdFoam)
            order.Add(new VanillaColdFoam());

        if (Input.Caramel)
            order.Add(new CaramelDrizzle());
    }
}
