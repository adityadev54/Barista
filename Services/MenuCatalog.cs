using Barista.Domain;

namespace Barista.Services;

// The client and staff menus share these item names, photos, and starting prices.
// SQLite uses these names for availability. Keep existing names when adding items.
public enum MenuKey
{
    Espresso, ColdBrew, MatchaLatte, Cappuccino, IcedLatte,
    Croissant, Cookie, ZaatarScone, BananaBread, MisoToastie
}

public sealed record MenuEntry(
    MenuKey Key,
    Func<CupSize, MenuItem> CreateItem,
    string Description,
    string Image,
    CupSize? MinimumSize = null)
{
    // A drink starts at its smallest available size. Food uses a fixed price.
    public MenuItem DefaultItem => CreateItem(MinimumSize ?? CupSize.Medium);
    public string Name => DefaultItem.Name;
    public decimal Price => DefaultItem.GetBasePrice();
    public bool IsDrink => MinimumSize.HasValue;
}

public static class MenuCatalog
{
    public static IReadOnlyList<MenuEntry> Items { get; } =
    [
        new(
            MenuKey.Espresso, size => new Espresso(size),
            "Rich crema with a concentrated finish.", "/images/espresso.png",
            CupSize.Small),
        new(
            MenuKey.ColdBrew, size => new ColdBrew(size),
            "Slow-steeped coffee over ice.", "/images/cold-brew.png",
            CupSize.Medium),
        new(
            MenuKey.MatchaLatte, size => new MatchaLatte(size),
            "Earthy green tea with milk.", "/images/matcha-latte.png",
            CupSize.Small),
        new(
            MenuKey.Cappuccino, size => new Cappuccino(size),
            "Espresso, steamed milk, and thick foam.", "/images/cappuccino.png",
            CupSize.Small),
        new(
            MenuKey.IcedLatte, size => new IcedLatte(size),
            "Espresso and cold milk over ice.", "/images/iced-latte.png",
            CupSize.Small),
        new(
            MenuKey.Croissant, _ => new Croissant(),
            "Buttery, flaky pastry.", "/images/croissant.png"),
        new(
            MenuKey.Cookie, _ => new ChocolateCookie(),
            "Soft-baked with dark chocolate chunks.", "/images/cookie.png"),
        new(
            MenuKey.ZaatarScone, _ => new ZaatarScone(),
            "Sharp cheddar and herby za’atar.", "/images/zaatar-scone.png"),
        new(
            MenuKey.BananaBread, _ => new BananaBread(),
            "A thick slice with warm cardamom.", "/images/banana-bread.png"),
        new(
            MenuKey.MisoToastie, _ => new MisoToastie(),
            "Miso mushrooms and melted cheese on sourdough.", "/images/miso-toastie.png")
    ];

    public static MenuEntry? Find(MenuKey key) => Items.FirstOrDefault(item => item.Key == key);
}
