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

            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                    if (exceptionFeature?.Error is not null)
                    {
                        logger.LogError(exceptionFeature.Error, "An unhandled exception occurred.");
                    }
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"An unexpected error occurred.\"}");
                });
            });

            app.MapGet("/", () => "Hello World!");

            app.MapPost("/books", async (BookInput input, AppDbContext dbContext) =>
            {
                var book = new Book { Title = input.Title, Price = input.Price };
                dbContext.Books.Add(book);
                await dbContext.SaveChangesAsync();
                return Results.CreatedAtRoute("GetBookById", new { id = book.Id }, book);
            });

            app.MapGet("/books/{id}", async (Guid id, AppDbContext dbContext) =>
            {
                return await dbContext.Books.FindAsync(id) is Book book ? Results.Ok(book) : Results.NotFound();
            }).WithName("GetBookById");

            app.MapGet("/books", async (int? skip, int? take, AppDbContext dbContext) =>
            {
                var skipValue = Math.Max(0, skip ?? 0);
                var takeValue = Math.Clamp(take ?? 50, 1, 100);
                var books = await dbContext.Books
                    .OrderBy(b => b.Id)
                    .Skip(skipValue)
                    .Take(takeValue)
                    .ToListAsync();
                return Results.Ok(books);
            });

            app.MapPut("/books/{id}", async (Guid id, BookInput input, AppDbContext dbContext) =>
            {
                var book = await dbContext.Books.FindAsync(id);
                if (book is null)
                {
                    return Results.NotFound();
                }
                book.Title = input.Title;
                book.Price = input.Price;
                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Results.Conflict();
                }
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
                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Results.Conflict();
                }
                return Results.NoContent();
            });

            app.Run();
        }
    }
}
