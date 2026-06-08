using System.ComponentModel.DataAnnotations.Schema;
namespace DemoCRUD_LOGIN.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
