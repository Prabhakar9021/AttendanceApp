using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AttendanceApp.Models;
using Microsoft.EntityFrameworkCore;
namespace AttendanceApp.Data
{
    public class AppDbContext:IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<AttendanceRequest> AttendanceRequests { get; set; }


    }
}
