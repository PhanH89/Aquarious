using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DemoCRUD_LOGIN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        // Tạo một cấu trúc dữ liệu Danh mục (Category) nhanh ngay trong file
        public class CategoryMock
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        // API Xuất file CSV chứa danh sách danh mục sản phẩm
        [HttpGet("export-csv")]
        public IActionResult ExportCategoriesToCsv()
        {
            // 1. Khởi tạo danh sách dữ liệu mẫu (Mock Data)
            var categories = new List<CategoryMock>
            {
                new CategoryMock { Id = 1, Name = "Bể Cá Thủy Sinh", Code = "BE-CA" },
                new CategoryMock { Id = 2, Name = "Cá Cảnh Thuần Chủng", Code = "CA-CANH" },
                new CategoryMock { Id = 3, Name = "Thức Ăn & Dinh Dưỡng", Code = "THUC-AN" },
                new CategoryMock { Id = 4, Name = "Hệ Thống Lọc & Bơm", Code = "LOC-BOM" }
            };

            // 2. Sử dụng StringBuilder để dựng cấu trúc file CSV
            var csvBuilder = new StringBuilder();

            // Định nghĩa dòng tiêu đề (Header) của file Excel/CSV
            csvBuilder.AppendLine("Id,Mã Danh Mục,Tên Danh Mục");

            // Duyệt qua từng danh mục để ghi dữ liệu vào dòng tiếp theo
            foreach (var item in categories)
            {
                csvBuilder.AppendLine($"{item.Id},{item.Code},{item.Name}");
            }

            // 3. Chuyển đổi chuỗi văn bản thành mảng Byte kèm bảng mã UTF-8 để hiển thị được tiếng Việt
            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

            // Thêm ký tự BOM (Byte Order Mark) để Excel nhận diện đúng font tiếng Việt có dấu khi mở file lên
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var finalBytes = bom.Concat(csvBytes).ToArray();

            // 4. Trả file về trình duyệt của người dùng với định dạng văn bản CSV
            string fileName = $"Danh_Muc_San_Pham_{DateTime.Now:yyyyMMdd}.csv";
            return File(finalBytes, "text/csv", fileName);
        }
    }
}