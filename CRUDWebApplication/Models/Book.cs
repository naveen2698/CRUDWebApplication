using System.ComponentModel.DataAnnotations;

namespace CRUDWebApplication.Models
{
    public class Book
    {
        public Guid Id { get; private init; } = Guid.NewGuid();

        [Required]
        public required string Title { get; set; }

        [Range(0, (double)decimal.MaxValue)]
        public decimal Price { get; set; }
    }
}
