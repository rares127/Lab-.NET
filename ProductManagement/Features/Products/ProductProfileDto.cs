using System.Globalization;
using System.Linq;

namespace ProductManagement.Features.Products;

public class ProductProfileDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public string CategoryDisplayName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string FormattedPrice { get; init; } = string.Empty;
    public DateTime ReleaseDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsAvailable { get; init; }
    public int StockQuantity { get; init; }
    public string ProductAge { get; init; } = string.Empty;
    public string BrandInitials { get; init; } = string.Empty;
    public string AvailabilityStatus { get; init; } = string.Empty;

    // Parameterless constructor for AutoMapper
    public ProductProfileDto()
    {
    }

    // Constructor for manual mapping
    public ProductProfileDto(Product product)
    {
        Id = product.Id;
        Name = product.Name;
        Brand = product.Brand;
        SKU = product.SKU;
        CategoryDisplayName = product.Category.ToString();
        Price = product.Price;
        FormattedPrice = product.Price.ToString("C", CultureInfo.CurrentCulture);
        ReleaseDate = product.ReleaseDate;
        CreatedAt = product.CreatedAt;
        ImageUrl = product.ImageUrl;
        IsAvailable = product.IsAvailable;
        StockQuantity = product.StockQuantity;
        AvailabilityStatus = IsAvailable ? "Available" : "Out of stock";
        BrandInitials = string.Join("", product.Brand
            .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => char.ToUpperInvariant(s[0])));
        ProductAge = ComputeProductAge(product.ReleaseDate);
    }

    private static string ComputeProductAge(DateTime release)
    {
        var now = DateTime.UtcNow;
        if (release > now) return "Not released yet";
        var totalMonths = (now.Year - release.Year) * 12 + now.Month - release.Month;
        if (totalMonths < 1) return "Less than a month";
        var years = totalMonths / 12;
        var months = totalMonths % 12;
        if (years > 0 && months > 0) return $"{years} yr {months} mo";
        if (years > 0) return $"{years} yr";
        return $"{months} mo";
    }
}