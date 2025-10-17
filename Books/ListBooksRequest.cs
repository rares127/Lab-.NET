namespace Lab3.Books;

public sealed record ListBooksRequest(
    int? Page = 1,
    int? PageSize = 10,
    string? AuthorContains = null,
    string? Author = null,
    string? SortBy = null,
    bool Desc = false
);