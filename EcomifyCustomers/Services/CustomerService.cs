using EcomifyCustomers.Data;
using EcomifyCustomers.DTOs;
using EcomifyCustomers.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomifyCustomers.Services;

public class CustomerService(AppDbContext db) : ICustomerService
{
    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var rows = await db.CustomersFlat.AsNoTracking().ToListAsync();
        return rows.Select(ToDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(string id)
    {
        var row = await db.CustomersFlat.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        return row is null ? null : ToDto(row);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO ecommify_customers (customer_id, customer_unique_id, customer_address)
            VALUES (
                {dto.CustomerId},
                {dto.CustomerUniqueId},
                ROW({dto.CustomerAddress!.ZipCode ?? (object)DBNull.Value},
                    {dto.CustomerAddress.City     ?? (object)DBNull.Value},
                    {dto.CustomerAddress.State    ?? (object)DBNull.Value})::address_type
            )
            """);

        var created = await db.CustomersFlat.FirstAsync(c => c.CustomerId == dto.CustomerId);
        return ToDto(created);
    }

    public async Task<CustomerDto?> UpdateAsync(string id, UpdateCustomerDto dto)
    {
        await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE ecommify_customers
            SET customer_unique_id = {dto.CustomerUniqueId},
                customer_address   = ROW({dto.CustomerAddress!.ZipCode ?? (object)DBNull.Value},
                                        {dto.CustomerAddress.City     ?? (object)DBNull.Value},
                                        {dto.CustomerAddress.State    ?? (object)DBNull.Value})::address_type
            WHERE customer_id = {id}
            """);

        var updated = await db.CustomersFlat.FirstOrDefaultAsync(c => c.CustomerId == id);
        return updated is null ? null : ToDto(updated);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var affected = await db.Database.ExecuteSqlAsync(
            $"DELETE FROM ecommify_customers WHERE customer_id = {id}");
        return affected > 0;
    }

    private static CustomerDto ToDto(CustomerFlat r) => new(
        r.CustomerId,
        r.CustomerUniqueId,
        r.ZipCode is null && r.City is null && r.State is null ? null
            : new CustomerAddressDto(r.ZipCode, r.City, r.State)
    );
}
