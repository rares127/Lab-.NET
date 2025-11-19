using System.ComponentModel.DataAnnotations;
using ProductManagement.Features.Products;

namespace ProductManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowedCategories;

    public ProductCategoryAttribute(params ProductCategory[] allowedCategories)
    {
        _allowedCategories = allowedCategories ?? Array.Empty<ProductCategory>();
        
        if (_allowedCategories.Length > 0)
        {
            var categoryNames = string.Join(", ", _allowedCategories.Select(c => c.ToString()));
            ErrorMessage = $"Category must be one of: {categoryNames}";
        }
        else
        {
            ErrorMessage = "Invalid product category.";
        }
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (value is not ProductCategory category)
            return new ValidationResult("Invalid category type.");

        if (_allowedCategories.Length == 0)
            return ValidationResult.Success;

        if (!_allowedCategories.Contains(category))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
