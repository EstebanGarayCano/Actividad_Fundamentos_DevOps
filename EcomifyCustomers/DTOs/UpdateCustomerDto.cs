namespace EcomifyCustomers.DTOs;

public record UpdateCustomerDto(
    string? CustomerUniqueId,
    CustomerAddressDto? CustomerAddress
);