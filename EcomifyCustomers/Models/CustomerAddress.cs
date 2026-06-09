namespace EcomifyCustomers.Models;

// Composite type PostgreSQL "address_type": zip_code varchar(20), city varchar(100), state varchar(10)
// Npgsql convierte nombres a snake_case: ZipCode -> zip_code, City -> city, State -> state
public record CustomerAddress(
    string? ZipCode,
    string? City,
    string? State
);
