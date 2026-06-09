using EcomifyCustomers.Models;

namespace EcomifyCustomers.Tests.Models;

public class CustomerAddressTests
{
    [Fact]
    public void CustomerAddress_Constructor_SetsAllProperties()
    {
        var address = new CustomerAddress("12345", "São Paulo", "SP");

        Assert.Equal("12345", address.ZipCode);
        Assert.Equal("São Paulo", address.City);
        Assert.Equal("SP", address.State);
    }

    [Fact]
    public void CustomerAddress_AllNullValues_CreatesValidRecord()
    {
        var address = new CustomerAddress(null, null, null);

        Assert.Null(address.ZipCode);
        Assert.Null(address.City);
        Assert.Null(address.State);
    }

    [Fact]
    public void CustomerAddress_Equality_SameValuesMeansEqual()
    {
        var a1 = new CustomerAddress("12345", "São Paulo", "SP");
        var a2 = new CustomerAddress("12345", "São Paulo", "SP");

        Assert.Equal(a1, a2);
    }

    [Fact]
    public void CustomerAddress_Equality_DifferentValuesMeansNotEqual()
    {
        var a1 = new CustomerAddress("12345", "São Paulo", "SP");
        var a2 = new CustomerAddress("99999", "Rio", "RJ");

        Assert.NotEqual(a1, a2);
    }
}
