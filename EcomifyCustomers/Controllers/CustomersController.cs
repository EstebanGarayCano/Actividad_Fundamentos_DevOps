using EcomifyCustomers.DTOs;
using EcomifyCustomers.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomifyCustomers.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var customers = await service.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(string id)
    {
        var customer = await service.GetByIdAsync(id);
        if (customer is null) return NotFound();
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerDto dto)
    {
        var exists = await service.GetByIdAsync(dto.CustomerId);
        if (exists is not null)
            return Conflict(new { message = $"Customer '{dto.CustomerId}' already exists." });

        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.CustomerId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> Update(string id, UpdateCustomerDto dto)
    {
        var updated = await service.UpdateAsync(id, dto);
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
