using System;
using FluentValidation;
using Lab3.Books;

namespace Lab3.Validators;

public class BookValidator : AbstractValidator<CreateBookRequest>
{
    public BookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(1);

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MinimumLength(3).WithMessage("Author must be at least 3 characters long");

        var currentYear = DateTime.UtcNow.Year;
        RuleFor(x => x.Year)
            .InclusiveBetween(1450, currentYear)
            .WithMessage($"Year must be between 1450 and {currentYear}.");
    }
}