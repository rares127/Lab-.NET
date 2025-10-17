using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Books;

public class GetAllBooksHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(GetAllBooksRequest request)
    {
        var items = await _context.Books.ToListAsync();
        return Results.Ok(items);
    }
}