namespace Barista.Domain;

// Base type shared by drinks and food. The order builder calls its validation before pricing.
public abstract class MenuItem
{
    public abstract string Name { get; }
    public abstract int PrepMinutes { get; }
    public abstract decimal GetBasePrice();

    // Food uses this default rule. Beverages override it to allow drink extras.
    public virtual void Validate(IReadOnlyList<IMenuItemAddOn> addons)
    {
        if (addons.Count > 0)
            throw new OrderValidationException("Drink extras require a beverage.");
    }
}
