using System.ComponentModel.DataAnnotations;

namespace CRUDWebApplication.Models
{
    public class Book
    {
        public Guid Id { get; private init; } = Guid.NewGuid();

        [Required]
        public required string Title { get; set; }

        [Required]
        public decimal Price { get; set; }
    }
}
