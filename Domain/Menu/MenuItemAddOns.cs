namespace Barista.Domain;

// Each add-on tells the pricing service how much it adds to the price and prep time.
public interface IMenuItemAddOn
{
    string Name { get; }
    decimal ApplyPrice();
    int ApplyPrepMinutes();
}

// These are the drink extras offered on the order page.
public sealed class OatMilkSwap : IMenuItemAddOn
{
    public string Name => "Oat milk swap";
    public decimal ApplyPrice() => 0.65m;
    public int ApplyPrepMinutes() => 1;
}

public sealed class ExtraShot : IMenuItemAddOn
{
    public string Name => "Extra shot";
    public decimal ApplyPrice() => 0.90m;
    public int ApplyPrepMinutes() => 1;
}

public sealed class VanillaColdFoam : IMenuItemAddOn
{
    public string Name => "Vanilla cold foam";
    public decimal ApplyPrice() => 1.25m;
    public int ApplyPrepMinutes() => 2;
}

public sealed class CaramelDrizzle : IMenuItemAddOn
{
    public string Name => "Caramel drizzle";
    public decimal ApplyPrice() => 0.50m;
    public int ApplyPrepMinutes() => 1;
}
