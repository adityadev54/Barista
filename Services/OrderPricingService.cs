using Barista.Domain;

namespace Barista.Services;

// Turns a validated order into the receipt shown to the client and saved with the ticket.
public sealed class OrderPricingService(IEnumerable<IDiscountRule> rules)
{
    public OrderReceipt Price(Order order)
    {
        var item = order.Item;
        var total = item.GetBasePrice();
        List<ReceiptLine> lines =
        [
            new(item.Name, total, item.PrepMinutes)
        ];
        List<TraceStep> trace =
        [
            new(
                item is Beverage ? nameof(Beverage) : nameof(MenuItem),
                item.GetType().Name,
                "GetBasePrice()")
        ];

        // Combine repeated extras into one line, while charging and timing every unit.
        var addons = order.Addons.GroupBy(addon => (Type: addon.GetType(), addon.Name));
        foreach (var group in addons)
        {
            var amount = group.Sum(addon => addon.ApplyPrice());
            var minutes = group.Sum(addon => addon.ApplyPrepMinutes());
            var count = group.Count();
            var quantity = count > 1 ? $" ×{count}" : "";
            lines.Add(new(group.Key.Name + quantity, amount, minutes));
            trace.Add(new(
                nameof(IMenuItemAddOn),
                group.Key.Type.Name,
                "ApplyPrice() · ApplyPrepMinutes()" + quantity));
            total += amount;
        }

        foreach (var rule in rules)
        {
            // Cap discounts at the current total and round each amount to cents.
            var discount = Math.Clamp(rule.GetDiscount(order, total), 0, total);
            discount = decimal.Round(discount, 2, MidpointRounding.AwayFromZero);
            trace.Add(new(
                nameof(IDiscountRule),
                rule.GetType().Name,
                discount > 0 ? "GetDiscount() · applied" : "GetDiscount() · skipped"));

            if (discount == 0)
                continue;

            lines.Add(new(rule.Name, -discount, 0));
            total -= discount;
        }

        return new(lines.AsReadOnly(), trace.AsReadOnly());
    }
}
