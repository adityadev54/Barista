namespace Barista.Domain;

// The builder collects selections. The finished order keeps a validated snapshot for the pricing service.
public sealed class Order
{
    internal Order(
        MenuItem item,
        IEnumerable<IMenuItemAddOn> addons,
        bool reusableCup,
        bool happyHour)
    {
        Item = item;
        // Copy the list so later changes in the builder cannot change this order.
        Addons = Array.AsReadOnly(addons.ToArray());
        Item.Validate(Addons);
        ReusableCup = reusableCup;
        HappyHour = happyHour;
    }

    public MenuItem Item { get; }
    public IReadOnlyList<IMenuItemAddOn> Addons { get; }
    public bool ReusableCup { get; }
    public bool HappyHour { get; }
}

public sealed class OrderBuilder(MenuItem item)
{
    private readonly List<IMenuItemAddOn> addons = [];
    private bool reusableCup;
    private bool happyHour;

    public OrderBuilder Add(IMenuItemAddOn addon)
    {
        addons.Add(addon);
        return this;
    }

    public OrderBuilder WithPromotions(bool broughtCup, bool duringHappyHour)
    {
        reusableCup = broughtCup;
        happyHour = duringHappyHour;
        return this;
    }

    // Build runs the selected item's validation before handing the order to pricing.
    public Order Build() => new(item, addons, reusableCup, happyHour);
}
