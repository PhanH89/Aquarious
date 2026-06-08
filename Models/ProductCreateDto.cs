using Microsoft.AspNetCore.Http;

namespace DemoCRUD_LOGIN.Models
{
    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }

        // Đảm bảo viết chuẩn từng chữ cái hoa-thường: IFormFile và ImageFile
        public IFormFile? ImageFile { get; set; }
    }
}