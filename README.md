# Barista

A C# and ASP.NET Core Razor Pages order portal with separate client and staff
workspaces. The client menu includes five drinks and five food items.

## Run locally

Use the .NET 10 SDK.

```sh
dotnet run
```

Open [the client menu](http://localhost:5045). Clients can register or
sign in and track purchases in **My orders**. Guest checkout is available.

To change roles, use **Sign out**. The signed-out [login page](http://localhost:5045/Client)
offers **Staff sign-in**. On first launch, create the staff username and password
from localhost. The client portal does not show staff navigation, and a signed-in
client must sign out before opening staff pages.

## Quick smoke test

With the app running, use `./scripts/smoke-test.sh` from a second terminal.
It checks that the menu and receipt render and that Admin asks visitors to
sign in. If Barista is running on a different port, pass its URL as an argument.

## SQLite database

The app creates `App_Data/barista.sqlite` on startup. It stores the staff
and client accounts with hashed passwords, order tickets, and menu availability. Those records
survive restarts. Set `Database__Path` to use a different database file.

## Order flow

- Choose one menu item. Drink orders show cup sizes and optional extras.
- The receipt refreshes as selections change. The browser sends the form
  to a Razor Pages handler that renders the shared receipt partial.
- **Place order** creates a numbered ticket. Staff can mark it **Ready** and
  then **Complete handoff** to clear it from the active queue.
- Staff can hide or restore items from the visual menu and see today's order
  counts and completed order value in **Daily overview**.

## Pricing and validation

- Cold Brew comes in Medium and Large. Drinks support up to 40 extra shots.
- Drink add-ons contribute their own price and prep time.
- The reusable cup rule takes $0.50 off a drink first. Happy Hour takes 15% off
  the remaining amount, rounded to the nearest cent.
- The Happy Hour switch simulates an order placed between 2 and 4 PM.
- The receipt trace shows every evaluated discount rule and whether it applied.

## Class diagram

```text
Espresso, ColdBrew, MatchaLatte, Cappuccino, IcedLatte → Beverage → MenuItem
Bakery items and toasties ──────────────────────────────────→ MenuItem

OrderBuilder ── builds ──> Order
Order ── holds ──────────> MenuItem and IMenuItemAddOn entries
OrderPricingService ── prices ─> Order using injected IDiscountRule entries
OrderWorkspace ── tracks ──────> OrderTicket and menu availability
```

## Project structure

- `Domain/Menu` contains menu items, drinks, food, and add-ons.
- `Domain/Orders` contains the order, builder, promotions, and receipt.
- `Pages/Shared/Receipts` contains the receipt markup used by the order page
  and the live pricing handler.

- `Pages/Ordering` holds the menu, checkout, and client order history.
- `Pages/Accounts` holds client and staff sign-in.
- `Pages/Administration` holds the staff queue, availability, and daily overview.
- `Data` handles SQLite and accounts. `Services` connects the catalog, pricing,
  and order workflow.
- `wwwroot/css/barista.css` holds the brand styling on top of Bootstrap.
  `wwwroot/barista.svg` is the logo and browser favicon.

### How Razor page pairs work

| What you want to change | File to open |
| --- | --- |
| Text, fields, labels, or layout on the order screen | `Pages/Ordering/Order.cshtml` |
| What happens when the page opens, quotes a price, or places an order | `Pages/Ordering/Order.cshtml.cs` |
| How a receipt looks on page load and after a quote | `Pages/Shared/Receipts/_OrderReceipt.cshtml` |

The shared filename ties a page to its C# page model. The `@model` line names
the class that supplies data and handles submissions. Keep both files together
when moving a paired page. The catalog page is a standalone Razor view because
it only reads the menu. The `@page` route keeps URLs such as `/Order` and
`/Staff` stable even though the files live in feature folders.

## Add a drink or add-on

A new drink needs a `Beverage` subclass with a name, prep time, and size prices.

```csharp
public sealed class Mocha(CupSize size) : Beverage(size)
{
    public override string Name => "Mocha";
    public override int PrepMinutes => 4;
    public override decimal GetBasePrice() => PriceBySize(4, 5, 6);
}
```

Add a key and entry to `MenuCatalog`. A food item instead extends `MenuItem`
and leaves `MinimumSize` unset.

A new add-on provides its name and the two adjustments through `IMenuItemAddOn`.

```csharp
public sealed class Cinnamon : IMenuItemAddOn
{
    public string Name => "Cinnamon";
    public decimal ApplyPrice() => 0.25m;
    public int ApplyPrepMinutes() => 1;
}
```

Add a checkbox and include the add-on in `OrderModel.AddExtras` when selected.
The pricing service includes the add-on in the receipt and trace.

## Menu photography

The transparent cut-outs in `wwwroot/images` were generated with built-in
ImageGen. The drink prompts used warm studio light, full silhouettes, and
unbranded drinkware. The new asset sheet called for a charcoal cup of
cappuccino with cocoa-dusted foam, a tall iced latte with milk swirls,
a flaky crescent croissant, and a thick chocolate chunk cookie.

The added food prompts use the same warm studio lighting and transparent
silhouettes for a za’atar cheddar scone, a thick slice of cardamom banana
bread, and a miso mushroom sourdough toastie.
