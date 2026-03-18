using System.ComponentModel.DataAnnotations;

namespace CRUDWebApplication.Models
{
    public record BookInput(
        [property: Required] string Title,
        [property: Range(0, (double)decimal.MaxValue)] decimal Price);
}
