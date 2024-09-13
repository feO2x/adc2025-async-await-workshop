using Microsoft.EntityFrameworkCore;
using WebApi.DatabaseAccess.Model;

namespace WebApi.DatabaseAccess;

public sealed class WebApiDbContext : DbContext
{
    public WebApiDbContext(DbContextOptions<WebApiDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxItem> OutboxItems => Set<OutboxItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(options =>
            {
                options.Property(x => x.Id).ValueGeneratedNever();
                options.Property(x => x.FirstName).IsRequired().HasMaxLength(250);
                options.Property(x => x.LastName).IsRequired().HasMaxLength(250);
                options.Property(x => x.Email).IsRequired(false).HasMaxLength(250);
                options.Property(x => x.PhoneNumber).IsRequired(false).HasMaxLength(20);
            }
        );

        modelBuilder.Entity<Address>(options =>
            {
                options.Property(x => x.Id).ValueGeneratedNever();
                options.Property(x => x.Street).IsRequired().HasMaxLength(250);
                options.Property(x => x.ZipCode).IsRequired().HasMaxLength(20);
                options.Property(x => x.City).IsRequired().HasMaxLength(250);
            }
        );

        modelBuilder.Entity<Order>(options => { options.Property(x => x.Id).ValueGeneratedNever(); }
        );

        modelBuilder.Entity<OutboxItem>(options =>
            {
                options.Property(x => x.MessageType).IsRequired().HasMaxLength(250);
                options.Property(x => x.SerializedMessage).IsRequired().HasMaxLength(4000);
            }
        );
    }
}