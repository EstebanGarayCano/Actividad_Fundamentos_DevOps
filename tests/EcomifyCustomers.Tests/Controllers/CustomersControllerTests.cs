using EcomifyCustomers.Controllers;
using EcomifyCustomers.DTOs;
using EcomifyCustomers.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EcomifyCustomers.Tests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _serviceMock;
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _serviceMock = new Mock<ICustomerService>();
        _controller = new CustomersController(_serviceMock.Object);
    }

    // ── GET ALL ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithListOfCustomers()
    {
        var customers = new List<CustomerDto>
        {
            new("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP")),
            new("c2", "u2", new CustomerAddressDto("54321", "Rio", "RJ"))
        };
        _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(customers);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(ok.Value);
        Assert.Equal(2, data.Count());
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(ok.Value);
        Assert.Empty(data);
    }

    // ── GET BY ID ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithCustomer()
    {
        var customer = new CustomerDto("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP"));
        _serviceMock.Setup(s => s.GetByIdAsync("c1")).ReturnsAsync(customer);

        var result = await _controller.GetById("c1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<CustomerDto>(ok.Value);
        Assert.Equal("c1", data.CustomerId);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync("unknown")).ReturnsAsync((CustomerDto?)null);

        var result = await _controller.GetById("unknown");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── POST CREATE ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_NewCustomer_ReturnsCreated()
    {
        var dto = new CreateCustomerDto("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP"));
        var created = new CustomerDto("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP"));

        _serviceMock.Setup(s => s.GetByIdAsync("c1")).ReturnsAsync((CustomerDto?)null);
        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdAt.StatusCode);
        var data = Assert.IsType<CustomerDto>(createdAt.Value);
        Assert.Equal("c1", data.CustomerId);
    }

    [Fact]
    public async Task Create_DuplicateId_ReturnsConflict()
    {
        var dto = new CreateCustomerDto("c1", "u1", new CustomerAddressDto("12345", "São Paulo", "SP"));
        var existing = new CustomerDto("c1", "u1", null);

        _serviceMock.Setup(s => s.GetByIdAsync("c1")).ReturnsAsync(existing);

        var result = await _controller.Create(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ── PUT UPDATE ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingCustomer_ReturnsOkWithUpdatedData()
    {
        var dto = new UpdateCustomerDto("u1-updated", new CustomerAddressDto("00000", "Brasilia", "DF"));
        var updated = new CustomerDto("c1", "u1-updated", new CustomerAddressDto("00000", "Brasilia", "DF"));

        _serviceMock.Setup(s => s.UpdateAsync("c1", dto)).ReturnsAsync(updated);

        var result = await _controller.Update("c1", dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<CustomerDto>(ok.Value);
        Assert.Equal("u1-updated", data.CustomerUniqueId);
        Assert.Equal("Brasilia", data.CustomerAddress!.City);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var dto = new UpdateCustomerDto("u1", new CustomerAddressDto("12345", "São Paulo", "SP"));
        _serviceMock.Setup(s => s.UpdateAsync("unknown", dto)).ReturnsAsync((CustomerDto?)null);

        var result = await _controller.Update("unknown", dto);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── DELETE ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingCustomer_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.DeleteAsync("c1")).ReturnsAsync(true);

        var result = await _controller.Delete("c1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync("unknown")).ReturnsAsync(false);

        var result = await _controller.Delete("unknown");

        Assert.IsType<NotFoundResult>(result);
    }
}
