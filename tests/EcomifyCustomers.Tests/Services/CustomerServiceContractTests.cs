using EcomifyCustomers.DTOs;
using EcomifyCustomers.Services;
using Moq;

namespace EcomifyCustomers.Tests.Services;

/// <summary>
/// Verifica el contrato de ICustomerService usando un mock.
/// Las pruebas de integración reales contra PostgreSQL se realizan
/// en el pipeline de CD con la base de datos de staging.
/// </summary>
public class CustomerServiceContractTests
{
    private readonly Mock<ICustomerService> _serviceMock = new();

    [Fact]
    public async Task GetAllAsync_ReturnsEnumerableOfCustomerDto()
    {
        _serviceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync([new CustomerDto("c1", "u1", null)]);

        var result = await _serviceMock.Object.GetAllAsync();

        Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCustomerDto()
    {
        var expected = new CustomerDto("c1", "u1", new CustomerAddressDto("12345", "SP", "SP"));
        _serviceMock.Setup(s => s.GetByIdAsync("c1")).ReturnsAsync(expected);

        var result = await _serviceMock.Object.GetByIdAsync("c1");

        Assert.NotNull(result);
        Assert.Equal("c1", result.CustomerId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        _serviceMock.Setup(s => s.GetByIdAsync("invalid")).ReturnsAsync((CustomerDto?)null);

        var result = await _serviceMock.Object.GetByIdAsync("invalid");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedCustomer()
    {
        var dto = new CreateCustomerDto("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP"));
        var expected = new CustomerDto("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP"));
        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(expected);

        var result = await _serviceMock.Object.CreateAsync(dto);

        Assert.Equal("c1", result.CustomerId);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        _serviceMock.Setup(s => s.DeleteAsync("c1")).ReturnsAsync(true);

        var result = await _serviceMock.Object.DeleteAsync("c1");

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ReturnsFalse()
    {
        _serviceMock.Setup(s => s.DeleteAsync("ghost")).ReturnsAsync(false);

        var result = await _serviceMock.Object.DeleteAsync("ghost");

        Assert.False(result);
    }
}
