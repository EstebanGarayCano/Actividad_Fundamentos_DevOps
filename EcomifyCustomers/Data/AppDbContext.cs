using EcomifyCustomers.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomifyCustomers.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerFlat> CustomersFlat => Set<CustomerFlat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.Ignore(e => e.CustomerAddress);
        });

        modelBuilder.Entity<CustomerFlat>(entity =>
        {
            entity.HasNoKey();
            entity.ToSqlQuery(@"
                SELECT customer_id,
                       customer_unique_id,
                       (customer_address).zip_code AS zip_code,
                       (customer_address).city     AS city,
                       (customer_address).state    AS state
                FROM ecommify_customers");

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerUniqueId).HasColumnName("customer_unique_id");
            entity.Property(e => e.ZipCode).HasColumnName("zip_code");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.State).HasColumnName("state");
        });
    }
}

public class CustomerFlat
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerUniqueId { get; set; }
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}