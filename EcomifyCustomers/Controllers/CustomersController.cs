using EcomifyCustomers.Data;
using EcomifyCustomers.DTOs;
using EcomifyCustomers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomifyCustomers.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    // GET /api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var rows = await db.CustomersFlat.AsNoTracking().ToListAsync();
        return Ok(rows.Select(ToDto));
    }

    // GET /api/customers/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(string id)
    {
        var row = await db.CustomersFlat
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        if (row is null) return NotFound();
        return Ok(ToDto(row));
    }

    // POST /api/customers
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerDto dto)
    {
        if (await db.Customers.AnyAsync(c => c.CustomerId == dto.CustomerId))
            return Conflict(new { message = $"Customer '{dto.CustomerId}' already exists." });

        // Insertamos con ROW()::address_type para construir el composite type
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

        var created = await db.CustomersFlat.FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId);
        return CreatedAtAction(nameof(GetById), new { id = dto.CustomerId }, ToDto(created!));
    }

    // PUT /api/customers/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> Update(string id, UpdateCustomerDto dto)
    {
        if (!await db.Customers.AnyAsync(c => c.CustomerId == id))
            return NotFound();

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
        return Ok(ToDto(updated!));
    }

    // DELETE /api/customers/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var rows = await db.Database.ExecuteSqlAsync(
            $"DELETE FROM ecommify_customers WHERE customer_id = {id}");
        if (rows == 0) return NotFound();
        return NoContent();
    }

    private static CustomerDto ToDto(CustomerFlat r) => new(
        r.CustomerId,
        r.CustomerUniqueId,
        r.ZipCode is null && r.City is null && r.State is null ? null
            : new CustomerAddressDto(r.ZipCode, r.City, r.State)
    );
}