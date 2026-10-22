using Microsoft.EntityFrameworkCore;

namespace TEO_Airline_Project.Models.EF
{
    public class AirlineDbContext : DbContext
    {
        // Hàm khởi tạo nhận cấu hình kết nối từ Program.cs
        public AirlineDbContext(DbContextOptions<AirlineDbContext> options) : base(options)
        {
        }

        // Khai báo các DbSet đại diện cho các bảng trong cơ sở dữ liệu sau này
        // Ví dụ minh họa bảng Chuyến bay (Flight) và Vé (Ticket):
        // public DbSet<Flight> Flights { get; set; }
        // public DbSet<Ticket> Tickets { get; set; }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bỏ qua các thuộc tính điều hướng gây lỗi cấu hình quan hệ phức tạp
            modelBuilder.Entity<Booking>().Ignore(b => b.User);
            modelBuilder.Entity<Booking>().Ignore(b => b.CreatedByStaff);

            // ... (các đoạn cấu hình khác nếu có sẵn trong file của bạn)
        }
    }
}
