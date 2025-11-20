using AutoMapper;
using ProductManagement.Features.Products;

namespace ProductManagement.Common.Mapping;

public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
        // Map CreateProductProfileRequest to Product
        CreateMap<CreateProductProfileRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.Ignore()) // Computed property
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity));

        // Map Product to ProductProfileDto with custom resolvers
        CreateMap<Product, ProductProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.ProductAge, opt => opt.MapFrom<ProductAgeResolver>())
            .ForMember(dest => dest.BrandInitials, opt => opt.MapFrom<BrandInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>())
            // Conditional mapping for ImageUrl (null for Home category)
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => 
                src.Category == ProductCategory.Home ? null : src.ImageUrl))
            // Conditional mapping for Price (10% discount for Home category)
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => 
                src.Category == ProductCategory.Home ? src.Price * 0.9m : src.Price));
    }
}

// Custom Value Resolver for Category Display Name
public class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Clothing => "Clothing & Fashion",
            ProductCategory.Books => "Books & Media",
            ProductCategory.Home => "Home & Garden",
            _ => "Uncategorized"
        };
    }
}

// Custom Value Resolver for Price Formatting
public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        // Apply discount for Home category
        var price = source.Category == ProductCategory.Home ? source.Price * 0.9m : source.Price;
        
        return price.ToString("C2", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
    }
}

// Custom Value Resolver for Product Age
public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var now = DateTime.UtcNow;
        var daysDiff = (now - source.ReleaseDate).Days;

        if (daysDiff < 30)
            return "New Release";

        if (daysDiff < 365)
        {
            var months = daysDiff / 30;
            return $"{months} months old";
        }

        if (daysDiff < 1825) // 5 years
        {
            var years = daysDiff / 365;
            return $"{years} years old";
        }

        return "Classic";
    }
}

// Custom Value Resolver for Brand Initials
public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Brand))
            return "?";

        var words = source.Brand.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length >= 2)
        {
            // Two+ words: First letter of first and last words
            return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[^1][0])}";
        }

        if (words.Length == 1)
        {
            // Single word: First letter
            return char.ToUpper(words[0][0]).ToString();
        }

        return "?";
    }
}

// Custom Value Resolver for Availability Status
public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
            return "Out of Stock";

        if (source.StockQuantity == 0)
            return "Unavailable";

        if (source.StockQuantity == 1)
            return "Last Item";

        if (source.StockQuantity <= 5)
            return "Limited Stock";

        return "In Stock";
    }
}