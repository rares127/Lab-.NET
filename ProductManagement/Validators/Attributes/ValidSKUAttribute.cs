using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProductManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    private static readonly Regex SkuRegex = new Regex(@"^[A-Za-z0-9\-]{5,20}$", RegexOptions.Compiled);

    public ValidSKUAttribute() : base("SKU must be alphanumeric with optional hyphens and be 5-20 characters.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        var sku = value.ToString();
        if (string.IsNullOrWhiteSpace(sku))
            return ValidationResult.Success;

        // Remove spaces before validation
        sku = sku.Replace(" ", "");

        if (!SkuRegex.IsMatch(sku))
        {
            return new ValidationResult(ErrorMessage ?? "Invalid SKU format.");
        }

        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-validsku", ErrorMessage ?? "Invalid SKU format.");
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
