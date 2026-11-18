namespace Barista.Domain;

// Each discount decides its own eligibility. The pricing service runs these rules in registration order.
public interface IDiscountRule
{
    string Name { get; }
    decimal GetDiscount(Order order, decimal runningTotal);
}

public sealed class ReusableCupRule : IDiscountRule
{
    public string Name => "Reusable cup";
    public decimal GetDiscount(Order order, decimal runningTotal)
        => order.ReusableCup && order.Item is Beverage ? 0.50m : 0;
}

// Happy Hour uses the running total after any earlier discount.
public sealed class HappyHourRule : IDiscountRule
{
    public string Name => "Happy hour";
    public decimal GetDiscount(Order order, decimal runningTotal)
        => order.HappyHour ? runningTotal * 0.15m : 0;
}
