using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceRangeAttribute(double minPrice, double maxPrice)
    {
        _minPrice = (decimal)minPrice;
        _maxPrice = (decimal)maxPrice;
        ErrorMessage = $"Price must be between {_minPrice:C2} and {_maxPrice:C2}.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (value is not decimal price)
        {
            // Try to convert to decimal
            if (!decimal.TryParse(value.ToString(), out price))
            {
                return new ValidationResult("Invalid price format.");
            }
        }

        if (price < _minPrice || price > _maxPrice)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
