using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductManagement.Common.Middleware;
using ProductManagement.Common.Logging;
using ProductManagement.Common.Mapping;
using ProductManagement.Persistence;
using ProductManagement.Features.Products;
using ProductManagement.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ProductManagementContext>(options =>
    options.UseSqlite("Data Source=productmanagement.db"));

builder.Services.AddMemoryCache();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile));

builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<CorrelationMiddleware>();

app.MapPost("/products", async (CreateProductProfileRequest request, CreateProductHandler handler) =>
    await handler.Handle(request));

app.MapGet("/products", async (ProductManagementContext context, IMemoryCache cache, AutoMapper.IMapper mapper) =>
{
    const string cacheKey = "all_products";

    if (cache.TryGetValue(cacheKey, out List<ProductProfileDto>? cachedProducts))
    {
        return Results.Ok(cachedProducts);
    }

    var products = await context.Products
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    var productDtos = mapper.Map<List<ProductProfileDto>>(products);

    cache.Set(cacheKey, productDtos, TimeSpan.FromMinutes(5));

    return Results.Ok(productDtos);
});

app.MapGet("/products/{id:guid}", async (Guid id, ProductManagementContext context, AutoMapper.IMapper mapper) =>
{
    var product = await context.Products.FindAsync(id);
    return product is not null
        ? Results.Ok(mapper.Map<ProductProfileDto>(product))
        : Results.NotFound();
});

app.Run();
