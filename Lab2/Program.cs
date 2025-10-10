namespace Lab2;

public class Program
{
    public static void Main()
    {
        var books = new List<Book>
        {
            new Book("Book1", "Author1", 2020),
            new Book("Book2", "Author2", 2015),
            new Book("Book3", "Author3", 2005),
        };
        var borrower = new Borrower(1, "Alice", books);

        var newBook = new Book("Book4", "Author4", 2024);
        var updatedBorrowedBooks = new List<Book>(borrower.BorrowedBooks) { newBook };
        var clonedBorrower = borrower with { BorrowedBooks = updatedBorrowedBooks };
        
        Console.WriteLine("Books borrowed by Alice:");
        foreach (var book in borrower.BorrowedBooks)
        {
            Console.WriteLine($"- {book.Title} by {book.Author} ({book.YearPublished})");
        }
        
        Console.WriteLine("Books borrowed by the cloned borrower:");
        foreach (var book in clonedBorrower.BorrowedBooks)
        {
            Console.WriteLine($"- {book.Title} by {book.Author} ({book.YearPublished})");
        }

        Console.Write("Enter a book title: ");
        string title = Console.ReadLine();
        Console.Write("Enter author name: ");
        string author = Console.ReadLine();
        Console.Write("Enter year published: ");
        int year = int.Parse(Console.ReadLine());

        books.Add(new Book(title, author, year));
        Console.WriteLine("Updated list of books:");
        foreach (var book in books)
        {
            Console.WriteLine($"- {book.Title} by {book.Author} ({book.YearPublished})");
        }

        ObjectDisplayer.DisplayObjectInfo(new Book("Sample Book", "Sample Author", 2021));
        ObjectDisplayer.DisplayObjectInfo(borrower);
        ObjectDisplayer.DisplayObjectInfo("Just a string");

        var booksAfter2010 = books.Where(static book => book.YearPublished > 2010);
        Console.WriteLine("Books published after 2010:");
        foreach (var book in booksAfter2010)
        {
            Console.WriteLine($"- {book.Title} ({book.YearPublished})");
        }
    }
}