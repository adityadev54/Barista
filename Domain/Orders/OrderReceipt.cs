namespace Barista.Domain;

// The order summary uses these lines and trace entries. The same receipt is saved with the order ticket.
public sealed record ReceiptLine(string Name, decimal Amount, int PrepMinutes);
public sealed record TraceStep(string Contract, string ClassName, string Detail);

public sealed record OrderReceipt(
    IReadOnlyList<ReceiptLine> Lines,
    IReadOnlyList<TraceStep> Trace)
{
    public decimal Total => Lines.Sum(line => line.Amount);
    public int PrepMinutes => Lines.Sum(line => line.PrepMinutes);
}
