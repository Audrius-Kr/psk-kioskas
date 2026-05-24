using Microsoft.EntityFrameworkCore;
using TicketPlatform.Data.Entities;

namespace TicketPlatform.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        b.Entity<Event>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Venue).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);

            // PostgreSQL xmin -> EF Core optimistic concurrency token.
            // When UPDATE WHERE xmin = ? affects 0 rows, EF throws DbUpdateConcurrencyException.
            e.Property(x => x.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        b.Entity<TicketCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.BasePrice).HasPrecision(10, 2);

            e.HasOne(x => x.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        b.Entity<Order>(e =>
        {
            e.Property(x => x.TotalAmount).HasPrecision(10, 2);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(10, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId);
            e.HasOne(x => x.TicketCategory).WithMany().HasForeignKey(x => x.TicketCategoryId);
        });
    }
}
