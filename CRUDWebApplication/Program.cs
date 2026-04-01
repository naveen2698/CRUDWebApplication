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

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
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

            app.MapPost("/books", async (BookInput input, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var book = new Book { Title = input.Title, Price = input.Price };
                dbContext.Books.Add(book);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.CreatedAtRoute("GetBookById", new { id = book.Id }, book);
            });

            app.MapGet("/books/{id}", async (Guid id, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return await dbContext.Books.FindAsync([id], cancellationToken) is Book book ? Results.Ok(book) : Results.NotFound();
            }).WithName("GetBookById");

            app.MapGet("/books", async (int? skip, int? take, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var skipValue = Math.Max(0, skip ?? 0);
                var takeValue = Math.Clamp(take ?? 50, 1, 100);
                var books = await dbContext.Books
                    .AsNoTracking()
                    .OrderBy(b => b.Id)
                    .Skip(skipValue)
                    .Take(takeValue)
                    .ToListAsync(cancellationToken);
                return Results.Ok(books);
            });

            app.MapPut("/books/{id}", async (Guid id, BookInput input, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var book = await dbContext.Books.FindAsync([id], cancellationToken);
                if (book is null)
                {
                    return Results.NotFound();
                }
                book.Title = input.Title;
                book.Price = input.Price;
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Results.Conflict();
                }
                return Results.NoContent();
            });

            app.MapDelete("/books/{id}", async (Guid id, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var book = await dbContext.Books.FindAsync([id], cancellationToken);
                if (book is null)
                {
                    return Results.NotFound();
                }
                dbContext.Books.Remove(book);
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
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
