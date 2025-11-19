using System.Diagnostics;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductManagement.Common.Logging;
using ProductManagement.Persistence;
using ProductManagement.Validators;

namespace ProductManagement.Features.Products;

public class CreateProductHandler(
    ProductManagementContext context,
    IMemoryCache cache,
    ILogger<CreateProductHandler> logger,
    CreateProductProfileValidator validator,
    IMapper mapper)
{
    private const string CacheKeyAllProducts = "all_products";

    public async Task<IResult> Handle(CreateProductProfileRequest request)
    {
        var operationStartTime = DateTime.UtcNow;
        var operationId = GenerateOperationId();
        
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["ProductName"] = request.Name ?? string.Empty,
            ["ProductSKU"] = request.SKU ?? string.Empty,
            ["ProductCategory"] = request.Category.ToString()
        }))
        {
            logger.LogInformation(LoggingModels.LogEvents.ProductCreationStarted,
                "Product creation started - Operation: {OperationId}, Name: {Name}, Brand: {Brand}, SKU: {SKU}, Category: {Category}",
                operationId, request.Name, request.Brand, request.SKU, request.Category);

            var validationStopwatch = Stopwatch.StartNew();
            var databaseStopwatch = new Stopwatch();
            var totalStopwatch = Stopwatch.StartNew();

            try
            {
                // Validation phase
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    validationStopwatch.Stop();
                    
                    logger.LogWarning(LoggingModels.LogEvents.ProductValidationFailed,
                        "Product validation failed - Operation: {OperationId}, SKU: {SKU}, Errors: {ValidationErrors}",
                        operationId, request.SKU, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                    var failedMetrics = new LoggingModels.ProductCreationMetrics(
                        operationId,
                        request.Name ?? string.Empty,
                        request.SKU ?? string.Empty,
                        request.Category,
                        validationStopwatch.Elapsed,
                        TimeSpan.Zero,
                        totalStopwatch.Elapsed,
                        false,
                        "Validation failed: " + string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                    logger.LogProductCreationMetrics(failedMetrics);
                    return Results.BadRequest(validationResult.Errors);
                }

                // SKU validation logging
                logger.LogInformation(LoggingModels.LogEvents.SKUValidationPerformed,
                    "SKU validation performed - Operation: {OperationId}, SKU: {SKU}, Valid: true",
                    operationId, request.SKU);

                // SKU uniqueness check
                var skuExists = await context.Products.AnyAsync(p => p.SKU == request.SKU);
                if (skuExists)
                {
                    validationStopwatch.Stop();
                    
                    logger.LogWarning(LoggingModels.LogEvents.StockValidationPerformed,
                        "SKU uniqueness validation failed - Operation: {OperationId}, SKU: {SKU}, Exists: true",
                        operationId, request.SKU);

                    var duplicateMetrics = new LoggingModels.ProductCreationMetrics(
                        operationId,
                        request.Name ?? string.Empty,
                        request.SKU ?? string.Empty,
                        request.Category,
                        validationStopwatch.Elapsed,
                        TimeSpan.Zero,
                        totalStopwatch.Elapsed,
                        false,
                        "Duplicate SKU");

                    logger.LogProductCreationMetrics(duplicateMetrics);
                    return Results.Conflict(new { Message = "A product with the same SKU already exists." });
                }

                logger.LogInformation(LoggingModels.LogEvents.StockValidationPerformed,
                    "SKU uniqueness validation passed - Operation: {OperationId}, SKU: {SKU}, Exists: false",
                    operationId, request.SKU);

                validationStopwatch.Stop();

                // Create product entity using AutoMapper
                var product = mapper.Map<Product>(request);

                // Database operations
                databaseStopwatch.Start();
                
                logger.LogInformation(LoggingModels.LogEvents.DatabaseOperationStarted,
                    "Database operation started - Operation: {OperationId}, ProductId: {ProductId}",
                    operationId, product.Id);

                context.Products.Add(product);
                await context.SaveChangesAsync();
                
                databaseStopwatch.Stop();

                logger.LogInformation(LoggingModels.LogEvents.DatabaseOperationCompleted,
                    "Database operation completed - Operation: {OperationId}, ProductId: {ProductId}, Duration: {Duration}ms",
                    operationId, product.Id, databaseStopwatch.ElapsedMilliseconds);

                // Cache operations
                cache.Remove(CacheKeyAllProducts);
                
                logger.LogInformation(LoggingModels.LogEvents.CacheOperationPerformed,
                    "Cache operation performed - Operation: {OperationId}, CacheKey: {CacheKey}, Action: Remove",
                    operationId, CacheKeyAllProducts);

                totalStopwatch.Stop();

                // Log successful metrics
                var successMetrics = new LoggingModels.ProductCreationMetrics(
                    operationId,
                    request.Name ?? string.Empty,
                    request.SKU ?? string.Empty,
                    request.Category,
                    validationStopwatch.Elapsed,
                    databaseStopwatch.Elapsed,
                    totalStopwatch.Elapsed,
                    true);

                logger.LogProductCreationMetrics(successMetrics);

                var dto = mapper.Map<ProductProfileDto>(product);
                return Results.Created($"/products/{product.Id}", dto);
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                
                // Log error metrics
                var errorMetrics = new LoggingModels.ProductCreationMetrics(
                    operationId,
                    request.Name ?? string.Empty,
                    request.SKU ?? string.Empty,
                    request.Category,
                    validationStopwatch.Elapsed,
                    databaseStopwatch.Elapsed,
                    totalStopwatch.Elapsed,
                    false,
                    ex.Message);

                logger.LogProductCreationMetrics(errorMetrics);
                
                logger.LogError(ex, "Product creation failed - Operation: {OperationId}, Name: {Name}, SKU: {SKU}",
                    operationId, request?.Name, request?.SKU);
                
                // Re-throw for global handler
                throw;
            }
        }
    }

    private static string GenerateOperationId()
    {
        return Guid.NewGuid().ToString("N")[..8].ToUpper();
    }
}
