using System.ComponentModel.DataAnnotations;

namespace EcomifyCustomers.DTOs;

public record CreateCustomerDto(
    [Required] string CustomerId,
    string? CustomerUniqueId,
    CustomerAddressDto? CustomerAddress
);