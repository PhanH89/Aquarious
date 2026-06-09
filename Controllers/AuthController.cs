using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoCRUD_LOGIN.Data;
using DemoCRUD_LOGIN.Models;
using DemoCRUD_LOGIN.Dtos; // Dùng trực tiếp bộ DTO từ folder Dtos của bạn
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BCrypt.Net; // Kích hoạt thư viện mã hóa chuẩn BCrypt

namespace DemoCRUD_LOGIN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        // Chuỗi khóa bí mật dùng để ký mã nhận diện Token (Độ dài tối thiểu 32 ký tự)
        private readonly string _jwtSecret = "Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_An_Toan_123456789";

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 💡 ĐÃ XÓA CÁC CLASS DTO TRÙNG LẶP Ở ĐÂY ĐỂ TRÁNH XUNG ĐỘT HỆ THỐNG

        // ==========================================
        // 1. API ĐĂNG KÝ TÀI KHOẢN
        // ==========================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
                return BadRequest(new { message = "Tên đăng nhập này đã tồn tại trên hệ thống!" });

            // Sử dụng BCrypt chính chủ để băm mật khẩu bảo mật
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = dto.Role // Hoạt động mượt mà nhờ dùng DTO chuẩn ngoài folder
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký tài khoản thành công!" });
        }

        // ==========================================
        // 2. API ĐĂNG NHẬP VÀ CẤP PHÁT TOKEN
        // ==========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            // Tìm user theo tên đăng nhập trước
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

            // Dùng BCrypt.Verify để đối chiếu mật khẩu thô gửi lên với chuỗi băm dưới DB
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            // Sinh cặp Token xịn khi xác thực thành công
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Lưu Refresh Token vào Database để đối chiếu sau này
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7); // Hạn dùng 7 ngày
            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = accessToken,
                refreshToken = refreshToken,
                username = user.Username,
                role = user.Role // Trả thêm role về cho Frontend làm badge hiển thị
            });
        }

        // ==========================================
        // 3. API REFRESH TOKEN (GIA HẠN TỰ ĐỘNG)
        // ==========================================
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == dto.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest(new { message = "Refresh Token không hợp lệ hoặc đã hết hạn!" });

            // Sinh cặp token mới cứng thay thế
            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        // --- CÁC HÀM BỔ TRỢ SINH TOKEN TỰ ĐỘNG ---
        private string GenerateAccessToken(User user)
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role) // Đóng gói quyền của User vào trong Token!
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(5), // Thời gian sống token
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}