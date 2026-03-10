using CRUDWebApplication.Data;
using CRUDWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace CRUDWebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddValidation();

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.MapPost("/books", async (Book book, AppDbContext dbContext) => {
                dbContext.Books.Add(book);
                await dbContext.SaveChangesAsync();
                return Results.Created($"/books/{book.Id}", book);
            });

            app.MapGet("/books/{id}", async (Guid id, AppDbContext dbContext) =>
            {
                return await dbContext.Books.FindAsync(id) is Book book ? Results.Ok(book): Results.NotFound();
            });

            app.MapGet("/books", async (AppDbContext dbContext) =>
            {
                var books = await dbContext.Books.ToListAsync();
                return Results.Ok(books);
            });

            app.MapPut("/books/{id}", async (Guid id, Book updatedBook, AppDbContext dbContext) =>
            {
                var book = await dbContext.Books.FindAsync(id);
                if (book is null)
                {
                    return Results.NotFound();
                }
                book.Title = updatedBook.Title;
                book.Price = updatedBook.Price;
                await dbContext.SaveChangesAsync();
                return Results.NoContent();
            });

            app.MapDelete("/books/{id}", async (Guid id, AppDbContext dbContext) =>
            {
                var book = await dbContext.Books.FindAsync(id);
                if (book is null)
                {
                    return Results.NotFound();
                }
                dbContext.Books.Remove(book);
                await dbContext.SaveChangesAsync();
                return Results.NoContent();
            });

            app.Run();
        }
    }
}
