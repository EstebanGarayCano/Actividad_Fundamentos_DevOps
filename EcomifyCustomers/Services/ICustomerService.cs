using EcomifyCustomers.DTOs;

namespace EcomifyCustomers.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(string id);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerDto?> UpdateAsync(string id, UpdateCustomerDto dto);
    Task<bool> DeleteAsync(string id);
}
