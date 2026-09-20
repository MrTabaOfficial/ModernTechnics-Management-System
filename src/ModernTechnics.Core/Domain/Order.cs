namespace ModernTechnics.Core.Domain;

/// <summary>A completed sale of one product. The price is captured at the time of sale.</summary>
public class Order
{
    public int Id { get; set; }

    public DateTime PlacedAtUtc { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    /// <summary>Optional: walk-in sales have no registered customer.</summary>
    public string? CustomerPersonalId { get; set; }

    public Customer? Customer { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total => UnitPrice * Quantity;
}
