using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoCRUD_LOGIN.Data;
using DemoCRUD_LOGIN.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace DemoCRUD_LOGIN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; // Dùng để lấy đường dẫn thư mục trên ổ cứng

        // Tiêm cả DbContext và Environment vào Controller
        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // 1. READ ALL (Giữ nguyên)
        [HttpGet]
        [Authorize(Roles = "Admin,Employee,Customer")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // 2. READ BY ID (Giữ nguyên)
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });
            return Ok(product);
        }

        // 3. NÂNG CẤP CREATE: Thêm sản phẩm kèm Upload Ảnh thực chiến
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<Product>> CreateProduct([FromForm] ProductCreateDto dto)
        {
            // Khởi tạo đối tượng Product gốc để chuẩn bị lưu database
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                CreatedAt = DateTime.Now
            };

            // LOGIC XỬ LÝ FILE ẢNH TẢI LÊN:
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                // Thư mục lưu ảnh: wwwroot/uploads
                string wwwRootPath = _environment.WebRootPath;
                string uploadsFolder = Path.Combine(wwwRootPath, "uploads");

                // Nếu thư mục uploads chưa tồn tại trên ổ cứng thì tự động tạo mới
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Đổi tên file ảnh thành một chuỗi duy nhất bằng Guid để tránh trùng tên file trên server
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                // Copy file ảnh vật lý từ trình duyệt và lưu xuống ổ cứng server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }

                // Lưu đường dẫn URL ảo vào database (Ví dụ: /uploads/abc-123.jpg)
                product.ImageUrl = "/uploads/" + fileName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // 4. DELETE (Nâng cấp thêm tính năng: Xóa sản phẩm thì xóa luôn file ảnh vật lý trên ổ cứng)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Sản phẩm không tồn tại" });

            // Nếu sản phẩm có ảnh, tiến hành xóa file ảnh trên ổ cứng để tránh rác server
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                string filePath = Path.Combine(_environment.WebRootPath, product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa sản phẩm và ảnh thành công!" });
        }
        // ==========================================
        // API CẬP NHẬT/SỬA SẢN PHẨM (ĐÃ TỐI ƯU)
        // ==========================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] ProductCreateDto dto)
        {
            // 1. Tìm sản phẩm gốc trong Database xem có tồn tại không
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm cần sửa!" });
            }

            // 2. Cập nhật các thông tin dạng chữ từ Frontend gửi qua
            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;

            // 3. XỬ LÝ ẢNH: Nếu người dùng có chọn file ảnh mới thì tiến hành lưu đè
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _environment.WebRootPath;
                string uploadsFolder = Path.Combine(wwwRootPath, "uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 🔥 ĐOẠN BỔ SUNG: Xóa file ảnh cũ trên ổ cứng (nếu có) trước khi lưu ảnh mới
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    string oldFilePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Tạo tên file duy nhất bằng Guid để tránh trùng lặp
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(stream);
                }

                // Cập nhật lại đường dẫn ảnh mới cứng cho sản phẩm
                product.ImageUrl = "/uploads/" + fileName;
            }

            // 4. Lưu toàn bộ thay đổi chỉnh sửa xuống SQL Server
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật sản phẩm thành công!", product });
        }
    }
}