using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Lab3.Persistence;

namespace Lab3.Books;

public class ListBooksHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(ListBooksRequest request)
    {
        /// inputs for pagination
        var page = Math.Max(1, request?.Page ?? 1);
        var pageSize = Math.Clamp(request?.PageSize ?? 10, 1, 100);

        // filtering and sorting query
        IQueryable<Book> query = _context.Books.AsQueryable();

        // filtering
        // exact author filter takes priority
        if (!string.IsNullOrWhiteSpace(request?.Author))
        {
            var author = request.Author!.ToLowerInvariant();
            // only include books where Author is not null and equals the provided value
            query = query.Where(b => b.Author != null && b.Author.ToLower().Equals(author));
        }
        // if exact author not provided, check for substring match
        else if (!string.IsNullOrWhiteSpace(request?.AuthorContains))
        {
            var ac = request.AuthorContains!.ToLowerInvariant();
            query = query.Where(b => b.Author != null && b.Author.ToLower().Contains(ac));
        }

        // sorting
        // if SortBy provided, choose the corresponding property (year or title)
        if (!string.IsNullOrWhiteSpace(request?.SortBy))
        {
            var sort = request.SortBy!.ToLowerInvariant();
            query = sort switch
            {
                "title" => request.Desc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
                "year"  => request.Desc ? query.OrderByDescending(b => b.Year)  : query.OrderBy(b => b.Year),
                // sort by id if unrecognized SortBy value
                _       => request.Desc ? query.OrderByDescending(b => b.Id)    : query.OrderBy(b => b.Id)
            };
        }
        else
        {
            query = request.Desc ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id);
        }
        
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var result = new PagedResult<Book>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Results.Ok(result);
    }
}
