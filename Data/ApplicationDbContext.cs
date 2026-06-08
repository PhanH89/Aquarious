using Microsoft.EntityFrameworkCore;
using DemoCRUD_LOGIN.Models;

namespace DemoCRUD_LOGIN.Data
{
    // Chú ý dấu hai chấm (:) và chữ DbContext phía sau. 
    // Đây là bắt buộc để .NET biết lớp này là cầu nối dữ liệu.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
