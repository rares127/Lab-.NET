using System;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Lab3.Books;
using Lab3.Persistence;
using Lab3.Middleware;
using Lab3.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<BookManagementContext>(options => options.UseSqlite("Data Source = bookmanagement.db"));

builder.Services.AddScoped<ListBooksHandler>();
builder.Services.AddScoped<GetAllBooksHandler>();
builder.Services.AddScoped<GetBookByIdHandler>();
builder.Services.AddScoped<CreateBookHandler>();
builder.Services.AddScoped<DeleteBookHandler>();
builder.Services.AddScoped<UpdateBookHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<BookValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateBookValidator>();


var app = builder.Build();

// ensure middleware is registered early so it catches unhandled exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookManagementContext>();
    context.Database.EnsureCreated();
}

app.MapGet("/books/all", async (GetAllBooksHandler handler) =>
    await handler.Handle(new GetAllBooksRequest()));

app.MapGet("/books/{id:int}", async (int id, GetBookByIdHandler handler) =>
    await handler.Handle(new GetBookByIdRequest(id)));

app.MapPost("/books", async (CreateBookRequest request, CreateBookHandler handler) =>
    await handler.Handle(request));

app.MapDelete("/books/{id:int}", async (int id, DeleteBookHandler handler) =>
    await handler.Handle(new DeleteBookRequest(id)));

app.MapPut("/books/{id:int}", async (int id, UpdateBookRequest request, UpdateBookHandler handler) =>
{
    request.Id = id;
    return await handler.Handle(request);
});

app.MapGet("/books", async (ListBooksRequest request, ListBooksHandler handler) =>
    await handler.Handle(request));

app.Run();
