// File: ProductManagement.Tests/CreateProductHandlerIntegrationTests.cs
using AutoMapper;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProductManagement.Common.Logging;
using ProductManagement.Common.Mapping;
using ProductManagement.Features.Products;
using ProductManagement.Persistence;
using ProductManagement.Validators;
using Xunit;

namespace ProductManagement.Tests;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ProductManagementContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateProductHandler> _logger;
    private readonly ILogger<CreateProductProfileValidator> _validatorLogger;
    private readonly IMapper _mapper;
    private readonly CreateProductHandler _handler;
    private readonly CreateProductProfileValidator _validator;

    public CreateProductHandlerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ProductManagementContext>()
            .UseInMemoryDatabase(databaseName: $"ProductManagementTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ProductManagementContext(options);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedProductMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        _cache = new MemoryCache(new MemoryCacheOptions());

        _logger = NullLogger<CreateProductHandler>.Instance;
        _validatorLogger = NullLogger<CreateProductProfileValidator>.Instance;

        _validator = new CreateProductProfileValidator(_context, _validatorLogger);
        _handler = new CreateProductHandler(_context, _cache, _logger, _validator, _mapper);
    }

    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        {
            Name = "Smart Phone XPro Technology",
            Brand = "Tech Innovations Inc",
            SKU = "TECH-001",
            Category = ProductCategory.Electronics,
            Price = 899.99m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-2),
            ImageUrl = "https://example.com/phone.jpg",
            StockQuantity = 10
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>>(result);
        var dto = createdResult.Value;

        Assert.NotNull(dto);
        Assert.Equal("Smart Phone XPro Technology", dto.Name);
        Assert.Equal("Electronics & Technology", dto.CategoryDisplayName);
        Assert.Equal("TI", dto.BrandInitials);
        Assert.Contains("month", dto.ProductAge.ToLower());
        Assert.StartsWith("$", dto.FormattedPrice);
        Assert.Equal("In Stock", dto.AvailabilityStatus);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ReturnsBadRequest()
    {
        // Arrange - Create existing product
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Product",
            Brand = "Existing Brand",
            SKU = "DUP-SKU-001",
            Category = ProductCategory.Electronics,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1),
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(existingProduct);
        await _context.SaveChangesAsync();

        // Arrange - Create request with same SKU
        var request = new CreateProductProfileRequest
        {
            Name = "New Product",
            Brand = "New Brand",
            SKU = "DUP-SKU-001",
            Category = ProductCategory.Electronics,
            Price = 200m,
            ReleaseDate = DateTime.UtcNow,
            StockQuantity = 5
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<List<ValidationFailure>>>(result);

        Assert.NotNull(badRequestResult.Value);
        // Verify one of the errors mentions unique/duplicate issues
        Assert.Contains(badRequestResult.Value, e => e.ErrorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        {
            Name = "Elegant Vase",
            Brand = "HomeDecor",
            SKU = "HOME-VASE-001",
            Category = ProductCategory.Home,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1),
            ImageUrl = "https://example.com/vase.jpg",
            StockQuantity = 20
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>>(result);
        var dto = createdResult.Value;

        Assert.NotNull(dto);
        Assert.Equal("Home & Garden", dto.CategoryDisplayName);
        Assert.Equal(90m, dto.Price);
        Assert.Null(dto.ImageUrl);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _cache?.Dispose();
    }
}
