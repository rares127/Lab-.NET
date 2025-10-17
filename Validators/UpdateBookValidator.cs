using System;
using FluentValidation;
using Lab3.Books;

namespace Lab3.Validators;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x).NotNull();

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than zero.");

        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title cannot be empty when provided.");
        });

        When(x => x.Author != null, () =>
        {
            RuleFor(x => x.Author)
                .NotEmpty().WithMessage("Author cannot be empty when provided.")
                .MinimumLength(3).WithMessage("Author must be at least 3 characters long when provided.");
        });

        var currentYear = DateTime.UtcNow.Year;
        When(x => x.Year.HasValue, () =>
        {
            RuleFor(x => x.Year.Value)
                .InclusiveBetween(1450, currentYear)
                .WithMessage($"Year must be between 1450 and {currentYear} when provided.");
        });

        RuleFor(x => x)
            .Must(x => x.Title != null || x.Author != null || x.Year.HasValue)
            .WithMessage("At least one updatable field (Title, Author, Year) must be provided.");
    }
}