namespace EcomifyCustomers.DTOs;

public record CustomerDto(
    string CustomerId,
    string? CustomerUniqueId,
    CustomerAddressDto? CustomerAddress
);

public record CustomerAddressDto(
    string? ZipCode,
    string? City,
    string? State
);