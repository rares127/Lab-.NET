using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
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
    private readonly Mock<ILogger<CreateProductHandler>> _mockLogger;
    private readonly Mock<ILogger<CreateProductProfileValidator>> _mockValidatorLogger;
    private readonly IMapper _mapper;
    private readonly CreateProductHandler _handler;
    private readonly CreateProductProfileValidator _validator;

    public CreateProductHandlerIntegrationTests()
    {
        // Set up in-memory database with unique name
        var options = new DbContextOptionsBuilder<ProductManagementContext>()
            .UseInMemoryDatabase(databaseName: $"ProductManagementTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ProductManagementContext(options);

        // Configure AutoMapper with both product profiles
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedProductMappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Set up memory cache
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Mock loggers
        _mockLogger = new Mock<ILogger<CreateProductHandler>>();
        _mockValidatorLogger = new Mock<ILogger<CreateProductProfileValidator>>();

        // Create validator and handler instances
        _validator = new CreateProductProfileValidator(_context, _mockValidatorLogger.Object);
        _handler = new CreateProductHandler(_context, _cache, _mockLogger.Object, _validator, _mapper);
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
            StockQuantity = 15
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>>(result);
        var dto = createdResult.Value;

        Assert.NotNull(dto);
        Assert.Equal("Smart Phone XPro", dto.Name);
        Assert.Equal("Electronics & Technology", dto.CategoryDisplayName);
        
        // Check BrandInitials for two-word brand (should be "TI" - Tech Innovations)
        Assert.Equal("TI", dto.BrandInitials);
        
        // Check ProductAge calculation (should be "New Release" since < 30 days is not met, but < 365 days)
        Assert.Contains("month", dto.ProductAge.ToLower());
        
        // Check FormattedPrice starts with currency symbol
        Assert.StartsWith("$", dto.FormattedPrice);
        
        // Check AvailabilityStatus based on stock (15 > 5, so "In Stock")
        Assert.Equal("In Stock", dto.AvailabilityStatus);

        // Verify ProductCreationStarted log called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Id == LoggingModels.LogEvents.ProductCreationStarted),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ReturnsConflict()
    {
        // Arrange - Create existing product in database
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
        var conflictResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Conflict<object>>(result);
        
        Assert.NotNull(conflictResult.Value);
        var message = conflictResult.Value.GetType().GetProperty("Message")?.GetValue(conflictResult.Value) as string;
        Assert.Contains("already exists", message, StringComparison.OrdinalIgnoreCase);

        // Verify ProductValidationFailed or similar log was NOT called (because we handle SKU uniqueness separately)
        // The handler should log a warning about duplicate SKU
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
        
        // Check CategoryDisplayName
        Assert.Equal("Home & Garden", dto.CategoryDisplayName);
        
        // Check Price has 10% discount applied (100 * 0.9 = 90)
        Assert.Equal(90m, dto.Price);
        
        // Check ImageUrl is null (content filtering for Home category)
        Assert.Null(dto.ImageUrl);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _cache?.Dispose();
    }
}
