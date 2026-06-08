using System;

namespace DemoCRUD_LOGIN.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Lưu mật khẩu đã mã hóa

        // Các trường phục vụ luồng Refresh Token thực chiến
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}