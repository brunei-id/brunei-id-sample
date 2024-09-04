using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Security.Policy;

namespace PSC.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<User>(entity => { entity.Property(e => e.Id).IsRequired(); });

        modelBuilder.Entity<User>().HasData(new User { Id = 1, IcNo = "01-072141", Email = "farhan.norazman@itpss.com", Name = "Iman Izzat Farhan Bin Mohd Norazman" }, new User { Id = 2, IcNo = "01-034560", Email = "fahmi.osman@itpss.com", Name = "Isyrah Fahmi Osman" }, new User {Id = 3, IcNo = "01-075963", Email = "safwan.ahman@itpss.com", Name = "Safwan Ahman" });

    }
}
