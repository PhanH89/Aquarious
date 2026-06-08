using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoCRUD_LOGIN.Data;
using DemoCRUD_LOGIN.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.ComponentModel.DataAnnotations;

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

        // --- LỚP HỨNG DỮ LIỆU ĐĂNG KÝ (DTO) KÈM VALIDATE ---
        public class UserRegisterDto
        {
            [Required(ErrorMessage = "Username không được trống")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email không được trống")]
            [EmailAddress(ErrorMessage = "Định dạng Email không đúng")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu không được trống")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
            public string Password { get; set; } = string.Empty;
        }

        public class UserLoginDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class TokenRequestDto
        {
            public string RefreshToken { get; set; } = string.Empty;
        }

        // ==========================================
        // 1. API ĐĂNG KÝ TÀI KHOẢN
        // ==========================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
                return BadRequest(new { message = "Tên đăng nhập này đã tồn tại trên hệ thống!" });

            // Thực tế nên dùng BCrypt để hash mật khẩu, ở đây demo hash bằng SHA256 cho nhanh gọn
            var passwordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Password)));

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash
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
            var passwordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Password)));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.PasswordHash == passwordHash);
            if (user == null)
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });

            // Sinh cặp Token xịn
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
                username = user.Username
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
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Để thời gian hết hạn cực ngắn (1 phút) để bạn dễ test tính năng tự động Refresh ngầm dưới Frontend
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(1),
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