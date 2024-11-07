using Microsoft.EntityFrameworkCore;
using AreEyeP.Models;

namespace AreEyeP.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<RegistrationViewModel> Clients { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ClientPayment> ClientPayments { get; set; }
        public DbSet<BurialApplication> BurialApplications { get; set; }
        public DbSet<Catacomb> Catacombs { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ActivityLog> ActivityLog { get; set; }
        public DbSet<Renewal> Renewal { get; set; }
        public DbSet<Deceased> Deceased { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Specify precision for the Amount column in ClientPayment
            modelBuilder.Entity<ClientPayment>()
                        .Property(p => p.Amount)
                        .HasPrecision(18, 2); // 18 is the precision, and 2 is the scale
        }
    }
}
