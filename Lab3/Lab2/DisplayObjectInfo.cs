namespace Lab2;
public static class ObjectDisplayer
{
    public static void DisplayObjectInfo(object obj)
    {
        switch (obj)
        {
            case Book book:
                Console.WriteLine($"Book: {book.Title}, Year: {book.YearPublished}");
                break;
            case Borrower borrower:
                Console.WriteLine($"Borrower: {borrower.Name}, Number of borrowed books: {borrower.BorrowedBooks.Count}");
                break;
            default:
                Console.WriteLine("Unknown object type");
                break;
        }
    }
}
