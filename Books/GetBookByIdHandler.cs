using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Books;

public class GetBookByIdHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(GetBookByIdRequest request)
    {
        if (request is null || request.Id <= 0)
            return Results.BadRequest(new { Error = "Invalid or missing Id." });

        var book = await _context.Books.FindAsync(request.Id);
        if (book is null)
            return Results.NotFound(new { Message = $"Book with id {request.Id} not found." });

        return Results.Ok(book);
    }
}