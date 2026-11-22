using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using VerifyXunit;

namespace Simulation.Checkout.Tests;

public class ReceiptSnapshotTests
{
    private static readonly Guid FixedOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FixedProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CheckoutAsync_WhenExampleCOrder_ShouldMatchApprovedReceiptSnapshot()
    {
        var inventory = new Mock<IInventoryService>();
        inventory
            .Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = new CheckoutService(
            new Mock<IOrderRepository>().Object,
            inventory.Object,
            new Mock<INotificationService>().Object);
        var order = new Order
        {
            Id = FixedOrderId,
            CustomerName = "Sara Mostafa",
            CustomerEmail = "sara.mostafa@example.com",
            Items =
            [
                new OrderItem
                {
                    ProductId = FixedProductId,
                    ProductName = "Mechanical Keyboard",
                    UnitPrice = 500m,
                    Quantity = 3
                }
            ]
        };

        var receipt = await sut.CheckoutAsync(order);

       
        await Verifier.Verify(new
        {
            receipt.OrderId,
            receipt.CustomerName,
            receipt.CustomerEmail,
            Items = receipt.Items.Select(line => new
            {
                line.ProductId,
                line.ProductName,
                UnitPrice = Money(line.UnitPrice),
                line.Quantity,
                LineTotal = Money(line.LineTotal)
            }),
            Subtotal = Money(receipt.Subtotal),
            Discount = Money(receipt.Discount),
            Total = Money(receipt.Total),
            receipt.CreatedAt
        }).ScrubMember("CreatedAt");
    }

    private static string Money(decimal value) =>
        $"{value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} EGP";
}
