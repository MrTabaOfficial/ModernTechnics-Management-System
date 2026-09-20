namespace ModernTechnics.Core.Domain;

/// <summary>
/// A catalogue item. Stock is tracked in two places: the warehouse receives deliveries,
/// the store floor sells to customers, and units move from the first to the second.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public int ReleaseYear { get; set; }

    public decimal Price { get; set; }

    public int WarehouseStock { get; set; }

    public int StoreStock { get; set; }

    public int TotalStock => WarehouseStock + StoreStock;
}
