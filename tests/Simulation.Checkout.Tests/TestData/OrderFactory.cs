using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public static class OrderFactory
{
    public static OrderItem Item(string productName, decimal unitPrice, int quantity) => new()
    {
        ProductId = Guid.NewGuid(),
        ProductName = productName,
        UnitPrice = unitPrice,
        Quantity = quantity
    };

    public static Order Create(params OrderItem[] items) =>
        Create("Test Customer", "test.customer@example.com", items);

    public static Order Create(string customerName, string customerEmail, params OrderItem[] items) => new()
    {
        CustomerName = customerName,
        CustomerEmail = customerEmail,
        Items = items
    };
}
