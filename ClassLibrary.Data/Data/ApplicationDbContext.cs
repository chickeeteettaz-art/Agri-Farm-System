using Microsoft.EntityFrameworkCore;

namespace ClassLibrary.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }

        public DbSet<Farmer> Farmers { get; set; }

        public DbSet<Agronomist> Agronomists { get; set; }

        public DbSet<Device> Devices { get; set; }
    }
}