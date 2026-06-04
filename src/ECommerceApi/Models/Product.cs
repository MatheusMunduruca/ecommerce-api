using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    /// <summary>Desconto atual em % (0, 5, 10 ou 25). Re-sorteado a cada 6h.</summary>
    public int DiscountPercent { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Preço final já com o desconto aplicado.</summary>
    [NotMapped]
    public decimal FinalPrice => Math.Round(Price * (100 - DiscountPercent) / 100m, 2);
}
