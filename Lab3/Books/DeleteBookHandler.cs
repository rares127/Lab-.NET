using Lab3.Persistence;

namespace Lab3.Books;

public class DeleteBookHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(DeleteBookRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return Results.BadRequest(new { Error = "Invalid or missing Id." });
        }

        var book = await _context.Books.FindAsync(request.Id);
        if (book is null)
        {
            return Results.NotFound(new { Message = $"Book with id {request.Id} not found." });
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return Results.NoContent();
    }
}