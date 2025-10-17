namespace Lab3.Books;

public class UpdateBookRequest
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int? Year { get; set; }
}