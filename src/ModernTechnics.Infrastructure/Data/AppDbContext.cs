using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Domain;

namespace ModernTechnics.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(255);
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.PasswordHash).HasMaxLength(255);
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Position>(position =>
        {
            position.Property(p => p.Name).HasMaxLength(100);
            position.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<Employee>(employee =>
        {
            employee.HasKey(e => e.PersonalId);
            employee.Property(e => e.PersonalId).HasMaxLength(11).IsFixedLength();
            employee.Property(e => e.FirstName).HasMaxLength(100);
            employee.Property(e => e.LastName).HasMaxLength(100);
            employee.Property(e => e.Address).HasMaxLength(255);
            employee.Property(e => e.Phone).HasMaxLength(20);
            employee.Property(e => e.Email).HasMaxLength(255);
            employee.HasIndex(e => e.Email).IsUnique();
            employee.Property(e => e.MaritalStatus).HasConversion<string>().HasMaxLength(16);
            employee.Ignore(e => e.FullName);
            employee.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(customer =>
        {
            customer.HasKey(c => c.PersonalId);
            customer.Property(c => c.PersonalId).HasMaxLength(11).IsFixedLength();
            customer.Property(c => c.FirstName).HasMaxLength(100);
            customer.Property(c => c.LastName).HasMaxLength(100);
            customer.Property(c => c.Address).HasMaxLength(255);
            customer.Property(c => c.Phone).HasMaxLength(20);
            customer.Ignore(c => c.FullName);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Name).HasMaxLength(100);
            product.Property(p => p.Manufacturer).HasMaxLength(100);
            product.Ignore(p => p.TotalStock);
            product.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Products_WarehouseStock", "\"WarehouseStock\" >= 0");
                table.HasCheckConstraint("CK_Products_StoreStock", "\"StoreStock\" >= 0");
            });
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.Ignore(o => o.Total);
            order.HasIndex(o => o.PlacedAtUtc);
            order.HasOne(o => o.Product).WithMany().HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            order.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerPersonalId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<JobApplication>(application =>
        {
            application.Property(a => a.FirstName).HasMaxLength(100);
            application.Property(a => a.LastName).HasMaxLength(100);
            application.Property(a => a.Phone).HasMaxLength(20);
            application.Property(a => a.MotivationLetter).HasMaxLength(4000);
            application.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
            application.Ignore(a => a.FullName);
            application.HasOne(a => a.Position).WithMany().HasForeignKey(a => a.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
