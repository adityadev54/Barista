namespace Barista.Domain;

// Drink prices and prep times live here. Drinks share size and shot rules, and Cold Brew adds its own size restriction.
public enum CupSize { Small, Medium, Large }

public sealed class OrderValidationException(string message) : Exception(message);

public abstract class Beverage(CupSize size) : MenuItem
{
    public const int MaxExtraShots = 40;
    public CupSize Size { get; } = size;

    public override void Validate(IReadOnlyList<IMenuItemAddOn> addons)
    {
        if (!Enum.IsDefined(Size))
            throw new OrderValidationException("Choose a valid cup size.");

        if (addons.OfType<ExtraShot>().Count() > MaxExtraShots)
            throw new OrderValidationException($"Drinks allow up to {MaxExtraShots} extra shots.");
    }

    // A drink passes its own three prices and shares the same size lookup.
    protected decimal PriceBySize(decimal small, decimal medium, decimal large)
        => Size switch
        {
            CupSize.Small => small,
            CupSize.Medium => medium,
            CupSize.Large => large,
            _ => throw new OrderValidationException("Choose a valid cup size.")
        };
}

public sealed class Espresso(CupSize size) : Beverage(size)
{
    public override string Name => "Espresso";
    public override int PrepMinutes => 2;
    public override decimal GetBasePrice() => PriceBySize(3, 3.75m, 4.50m);

}

public sealed class ColdBrew(CupSize size) : Beverage(size)
{
    public override string Name => "Cold Brew";
    public override int PrepMinutes => 2;
    public override decimal GetBasePrice() => PriceBySize(0, 4.50m, 5.25m);

    public override void Validate(IReadOnlyList<IMenuItemAddOn> addons)
    {
        base.Validate(addons);
        // Cold Brew starts at Medium in the catalog and enforces that rule here too.
        if (Size == CupSize.Small)
            throw new OrderValidationException("Cold Brew is available in Medium or Large.");
    }
}

public sealed class MatchaLatte(CupSize size) : Beverage(size)
{
    public override string Name => "Matcha Latte";
    public override int PrepMinutes => 4;
    public override decimal GetBasePrice() => PriceBySize(4.50m, 5.25m, 6);
}

public sealed class Cappuccino(CupSize size) : Beverage(size)
{
    public override string Name => "Cappuccino";
    public override int PrepMinutes => 3;
    public override decimal GetBasePrice() => PriceBySize(4, 4.75m, 5.50m);
}

public sealed class IcedLatte(CupSize size) : Beverage(size)
{
    public override string Name => "Iced Latte";
    public override int PrepMinutes => 3;
    public override decimal GetBasePrice() => PriceBySize(4.25m, 5, 5.75m);
}
