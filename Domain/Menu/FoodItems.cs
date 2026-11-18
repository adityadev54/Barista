namespace Barista.Domain;

// Food entries have a fixed price and prep time. The menu catalog supplies their photos and descriptions.
public sealed class Croissant : MenuItem
{
    public override string Name => "Butter Croissant";
    public override int PrepMinutes => 1;
    public override decimal GetBasePrice() => 3.25m;
}

public sealed class ChocolateCookie : MenuItem
{
    public override string Name => "Chocolate Chunk Cookie";
    public override int PrepMinutes => 1;
    public override decimal GetBasePrice() => 2.75m;
}

public sealed class ZaatarScone : MenuItem
{
    public override string Name => "Za’atar Cheddar Scone";
    public override int PrepMinutes => 1;
    public override decimal GetBasePrice() => 3.75m;
}

public sealed class BananaBread : MenuItem
{
    public override string Name => "Cardamom Banana Bread";
    public override int PrepMinutes => 1;
    public override decimal GetBasePrice() => 4m;
}

public sealed class MisoToastie : MenuItem
{
    public override string Name => "Miso Mushroom Toastie";
    public override int PrepMinutes => 5;
    public override decimal GetBasePrice() => 7.50m;
}
