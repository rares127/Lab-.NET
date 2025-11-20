using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManagement.Persistence;
using ProductManagement.Features.Products;

namespace ProductManagement.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    // simple inappropriate/restricted lists
    private static readonly string[] InappropriateWords = new[] { "inappropriate1", "bannedword", "restricted" };
    private static readonly string[] HomeRestrictedWords = new[] { "adult", "weapon", "danger" };
    private static readonly string[] ImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public CreateProductProfileValidator(ProductManagementContext context, ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        // Name
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(1).MaximumLength(200)
            .Must(BeValidName).WithMessage("Name contains inappropriate content.")
            .MustAsync(BeUniqueName).WithMessage("A product with the same name already exists for this brand.");

        // Brand
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MinimumLength(2).MaximumLength(100)
            .Must(BeValidBrandName).WithMessage("Brand contains invalid characters (allowed: letters, spaces, hyphens, apostrophes, dots, numbers).");

        // SKU
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .Matches(@"^[A-Za-z0-9\-]{5,20}$").WithMessage("SKU must be alphanumeric with optional hyphens and be 5-20 characters.")
            .MustAsync(BeUniqueSKU).WithMessage("SKU must be unique in the system.");

        // Category (enum)
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid category value.");

        // Price
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThan(10000).WithMessage("Price must be less than 10,000.");

        // ReleaseDate
        RuleFor(x => x.ReleaseDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Release date cannot be in the future.")
            .Must(d => d.Year >= 1900).WithMessage("Release date cannot be before year 1900.");

        // StockQuantity
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000.");

        // ImageUrl
        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl)
                .Must(BeValidImageUrl).WithMessage("ImageUrl must be a valid http/https image URL ending with .jpg/.jpeg/.png/.gif/.webp");
        });

        // Business rules (async)
        RuleFor(x => x).MustAsync(PassBusinessRules).WithMessage("One or more business rules failed.");

        // Conditional validation for Electronics
        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(50m).WithMessage("Electronics products must have a minimum price of $50.00.");
            
            RuleFor(x => x.Name)
                .Must(ContainTechnologyKeywords).WithMessage("Electronics product name must contain technology-related keywords.");
            
            RuleFor(x => x.ReleaseDate)
                .Must(d => d >= DateTime.UtcNow.AddYears(-5)).WithMessage("Electronics products must be released within the last 5 years.");
        });

        // Conditional validation for Home
        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(200m).WithMessage("Home products must have a maximum price of $200.00.");
            
            RuleFor(x => x.Name)
                .Must(BeAppropriateForHome).WithMessage("Home product name contains restricted words.");
        });

        // Conditional validation for Clothing
        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3).WithMessage("Clothing products require a brand name of at least 3 characters.");
        });

        // Cross-field validation
        RuleFor(x => x)
            .Must(x => x.Price <= 100m || x.StockQuantity <= 20)
            .WithMessage("Expensive products (>$100) must have limited stock (≤20 units).");
    }

    private bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true; // handled by NotEmpty
        var lower = name.ToLowerInvariant();
        foreach (var bad in InappropriateWords)
        {
            if (lower.Contains(bad))
            {
                _logger.LogWarning("Name contains inappropriate word '{Word}'", bad);
                return false;
            }
        }
        return true;
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest request, string name, CancellationToken ct)
    {
        var exists = await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Name == name && p.Brand == request.Brand, ct);

        if (exists)
            _logger.LogWarning("Duplicate product name detected for Name='{Name}', Brand='{Brand}'", name, request.Brand);

        return !exists;
    }


    private bool BeValidBrandName(string brand)
    {
        if (string.IsNullOrWhiteSpace(brand)) return true; 
        // we allow letters, numbers, spaces, hyphens, apostrophes, dots
        var regex = new Regex(@"^[A-Za-z0-9\s\-\.'']+$");
        var ok = regex.IsMatch(brand);
        if (!ok) _logger.LogWarning("Brand name validation failed for '{Brand}'", brand);
        return ok;
    }

    private bool BeValidSKU(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return true; // NotEmpty handles
        var regex = new Regex(@"^[A-Za-z0-9\-]{5,20}$");
        var ok = regex.IsMatch(sku);
        if (!ok) _logger.LogWarning("SKU format invalid: '{SKU}'", sku);
        return ok;
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sku)) return true;
        var exists = await _context.Products.AsNoTracking().AnyAsync(p => p.SKU == sku, ct);
        if (exists) _logger.LogWarning("Duplicate SKU detected: {SKU}", sku);
        return !exists;
    }

    private bool BeValidImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        var path = uri.AbsolutePath?.ToLowerInvariant() ?? string.Empty;
        var ok = ImageExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        if (!ok) _logger.LogWarning("Image URL validation failed for '{Url}'", url);
        return ok;
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken ct)
    {
        // Rule 1: Daily product addition limit (max 500 per UTC day)
        var todayStart = DateTime.UtcNow.Date;
        var addedToday = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.CreatedAt >= todayStart, ct);

        if (addedToday >= 500)
        {
            _logger.LogWarning("Daily product addition limit reached: {Count}", addedToday);
            return false;
        }

        // Rule 2: Electronics minimum price ($50.00)
        if (request.Category == ProductCategory.Electronics && request.Price < 50m)
        {
            _logger.LogWarning("Electronics product below minimum price: {Price}", request.Price);
            return false;
        }

        // Rule 3: Home product content restrictions (name must not contain restricted words)
        if (request.Category == ProductCategory.Home)
        {
            var lower = (request.Name ?? string.Empty).ToLowerInvariant();
            foreach (var w in HomeRestrictedWords)
            {
                if (lower.Contains(w))
                {
                    _logger.LogWarning("Home category name contains restricted word '{Word}'", w);
                    return false;
                }
            }
        }

        // Rule 4: High-value product stock limit (>$500 => max 10 stock)
        if (request.Price > 500m && request.StockQuantity > 10)
        {
            _logger.LogWarning("High-value product stock limit exceeded: Price={Price}, Stock={Stock}", request.Price, request.StockQuantity);
            return false;
        }

        // All rules passed
        _logger.LogInformation("Business rules passed for product Name='{Name}', SKU='{SKU}'", request.Name, request.SKU);
        return true;
    }

    private bool ContainTechnologyKeywords(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var techKeywords = new[] 
        { 
            "phone", "laptop", "computer", "tablet", "monitor", "keyboard", "mouse", 
            "headphones", "speaker", "camera", "tv", "smart", "wireless", "bluetooth",
            "usb", "gaming", "processor", "cpu", "gpu", "ssd", "hdd", "ram", "memory",
            "router", "modem", "wifi", "network", "tech", "digital", "electronic"
        };

        var lowerName = name.ToLowerInvariant();
        return techKeywords.Any(keyword => lowerName.Contains(keyword));
    }

    private bool BeAppropriateForHome(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        var lowerName = name.ToLowerInvariant();
        foreach (var restrictedWord in HomeRestrictedWords)
        {
            if (lowerName.Contains(restrictedWord))
            {
                _logger.LogWarning("Home product name contains restricted word: '{Word}'", restrictedWord);
                return false;
            }
        }
        return true;
    }
}