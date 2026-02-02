using Leave_system.Models;
using Microsoft.EntityFrameworkCore;

namespace Leave_system.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<Attendance> Attendance { get; set; }

    }
}
