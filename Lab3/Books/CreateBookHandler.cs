using Lab3.Validators;
using Lab3.Persistence;

namespace Lab3.Books;

public class CreateBookHandler(BookManagementContext context)
{ 
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(CreateBookRequest request)
    {
        // TODO - create a middleware for validation
        var book = new Book { Title = request.Title, Author = request.Author, Year = request.Year };
            
        var validator = new BookValidator(); 
        var validationResult = await validator.ValidateAsync(request); 
        if (!validationResult.IsValid) 
        { 
            return Results.BadRequest(validationResult.Errors);
        }
        
        _context.Books.Add(book); 
        await _context.SaveChangesAsync();
        return Results.Created($"/books/{book.Id}", book);
    }
}