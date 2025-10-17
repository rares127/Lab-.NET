using System;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Books;

public class UpdateBookHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(UpdateBookRequest request)
    {
        if (request is null)
            return Results.BadRequest(new { Error = "Invalid request." });

        var book = await _context.Books.FindAsync(request.Id);
        if (book is null)
            return Results.NotFound(new { Message = $"Book with id {request.Id} not found." });

        // apply only provided fields
        if (request.Title != null && !string.Equals(request.Title, book.Title, StringComparison.Ordinal))
            book.Title = request.Title;

        if (request.Author != null && !string.Equals(request.Author, book.Author, StringComparison.Ordinal))
            book.Author = request.Author;

        if (request.Year.HasValue && request.Year.Value != book.Year)
            book.Year = request.Year.Value;

        await _context.SaveChangesAsync();

        return Results.Ok(book);
    }
}