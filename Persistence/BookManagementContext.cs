namespace Lab3.Persistence;
using Microsoft.EntityFrameworkCore;
using Lab3.Books;

public class BookManagementContext(DbContextOptions<BookManagementContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
}
    