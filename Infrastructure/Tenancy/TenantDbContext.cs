using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenancy
{
    public class TenantDbContext(DbContextOptions<TenantDbContext> options) : EFCoreStoreDbContext<AppTenantInfo>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppTenantInfo>()
                .ToTable("Tenants", "Multitenant");
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.Id)
                .HasMaxLength(64)
                .IsRequired();
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.Name)
                .HasMaxLength(60)
                .IsRequired();
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.Email)
                .HasMaxLength(100)
                .IsRequired();
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.FirstName)
                .HasMaxLength(60)
                .IsRequired();
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.LastName)
                .HasMaxLength(60)
                .IsRequired();
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.IsActive)
                .IsRequired();
            modelBuilder.Entity<AppTenantInfo>()
                .Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        }
    }
}
