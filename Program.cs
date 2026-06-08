using DemoCRUD_LOGIN.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// 🔐 ĐOẠN KHAI BÁO BẢO MẬT JWT TOKEN:
var jwtSecret = "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_An_Toan_123456789"; // Trùng khớp với Secret bên AuthController
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero // Triệt tiêu thời gian trễ mặc định để Token hết hạn chính xác từng giây
    };
});
// ==========================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (SERVICES CONTAINER)
// ==========================================

// Đăng ký dịch vụ Controller để xử lý các API (như ProductsController)
builder.Services.AddControllers();

// Đăng ký dịch vụ OpenAPI/Swagger để tạo giao diện thử nghiệm API
builder.Services.AddOpenApi();

// Đăng ký ApplicationDbContext và cấu hình kết nối tới Microsoft SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthorization();
var app = builder.Build();

// ==========================================
// 2. CẤU HÌNH ĐƯỜNG ỐNG XỬ LÝ (MIDDLEWARE PIPELINE)
// ==========================================

// Kích hoạt giao diện hiển thị Swagger khi chạy ứng dụng trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Đoạn này giúp tự động chuyển cấu hình để bạn gõ đường dẫn /swagger là ra giao diện web
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "DemoCRUD_LOGIN API v1");
        options.RoutePrefix = "swagger";
    });
}

// Cấu hình bắt buộc chuyển hướng sang HTTPS bảo mật
app.UseHttpsRedirection();
app.UseAuthentication(); // Đọc và nhận diện Token
app.UseAuthorization();  // Kiểm tra quyền truy cập
app.UseDefaultFiles();
app.UseStaticFiles();
// Định tuyến để hệ thống tự nhận diện các Route trong Controller
app.MapControllers();

// ==========================================================
// 3. ĐOẠN CODE THỰC CHIẾN: TỰ ĐỘNG KHỞI TẠO DATABASE & BẢNG
// ==========================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Quét hệ thống: Nếu chưa có Database hoặc Bảng trong SQL Server, nó sẽ tự động dựng lên
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình tự động tạo cơ sở dữ liệu.");
    }
}

// Lệnh cuối cùng kích hoạt chạy toàn bộ ứng dụng Web API
app.Run();