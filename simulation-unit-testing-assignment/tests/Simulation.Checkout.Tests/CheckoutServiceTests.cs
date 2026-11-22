using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Simulation.Checkout.Tests.TestData;

namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<INotificationService> _notifications = new();

    private CheckoutService CreateSut(bool inStock = true)
    {
        _inventory
            .Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inStock);

        return new CheckoutService(_repository.Object, _inventory.Object, _notifications.Object);
    }

 

    [Fact]
    public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
    {
        var sut = CreateSut();
        var order = OrderFactory.Create();

        var act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<EmptyOrderException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckoutAsync_WhenQuantityIsZeroOrNegative_ShouldThrowInvalidQuantityExceptionWithProductName(int quantity)
    {
        var sut = CreateSut();
        var order = OrderFactory.Create(OrderFactory.Item("Gaming Mouse", 200m, quantity));

        var act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>()
            .WithMessage("*Gaming Mouse*");
    }

    [Fact]
    public async Task CheckoutAsync_WhenPriceIsNegative_ShouldThrowInvalidPriceException()
    {
        var sut = CreateSut();
        var order = OrderFactory.Create(OrderFactory.Item("Gaming Mouse", -0.01m, 1));

        var act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidPriceException>();
    }

    [Fact]
    public async Task CheckoutAsync_WhenPriceIsZero_ShouldSucceedWithZeroTotal()
    {
        var sut = CreateSut();
        var order = OrderFactory.Create(OrderFactory.Item("Free Sticker", 0m, 2));

        var receipt = await sut.CheckoutAsync(order);

        receipt.Total.Should().Be(0m);
    }

    [Theory]
    [InlineData(999.99, 0)]
    [InlineData(1000, 100)]
    [InlineData(1500, 100)]
    public async Task CheckoutAsync_WhenSubtotalChanges_ShouldApplyDiscountRule(double subtotal, double expectedDiscount)
    {
        var sut = CreateSut();
        var order = OrderFactory.Create(OrderFactory.Item("Keyboard", Convert.ToDecimal(subtotal), 1));

        var receipt = await sut.CheckoutAsync(order);

        receipt.Discount.Should().Be(Convert.ToDecimal(expectedDiscount));
    }

    public static TheoryData<OrderItem[], decimal, decimal, decimal> ExampleOrders => new()
    {
        {
            new[]
            {
                OrderFactory.Item("Cable", 150m, 2),
                OrderFactory.Item("Headphones", 400m, 1)
            },
            700m, 0m, 700m
        },
        {
            new[] { OrderFactory.Item("Keyboard", 500m, 3) },
            1500m, 100m, 1400m
        }
    };

    [Theory]
    [MemberData(nameof(ExampleOrders))]
    public async Task CheckoutAsync_WhenGivenExampleOrder_ShouldCalculateSubtotalDiscountAndTotal(
        OrderItem[] items, decimal expectedSubtotal, decimal expectedDiscount, decimal expectedTotal)
    {
        var sut = CreateSut();
        var order = OrderFactory.Create(items);

        var receipt = await sut.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(expectedSubtotal);
        receipt.Discount.Should().Be(expectedDiscount);
        receipt.Total.Should().Be(expectedTotal);
    }

   

    [Fact]
    public async Task CheckoutAsync_WhenAllItemsInStock_ShouldSaveOrderOnceAndSendConfirmationOnce()
    {
        var sut = CreateSut(inStock: true);
        var order = OrderFactory.Create(
            OrderFactory.Item("Cable", 150m, 2),
            OrderFactory.Item("Headphones", 400m, 1));

        var receipt = await sut.CheckoutAsync(order);

        receipt.OrderId.Should().Be(order.Id);
        _repository.Verify(
            r => r.SaveAsync(order, It.IsAny<CancellationToken>()),
            Times.Once);
        _notifications.Verify(
            n => n.SendOrderConfirmationAsync(receipt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_WhenOneItemIsOutOfStock_ShouldThrowAndNeverSaveOrNotify()
    {
        var inStockItem = OrderFactory.Item("Cable", 150m, 2);
        var outOfStockItem = OrderFactory.Item("Headphones", 400m, 1);
        var order = OrderFactory.Create(inStockItem, outOfStockItem);
        var sut = CreateSut(inStock: true);
        _inventory
            .Setup(i => i.IsAvailableAsync(outOfStockItem.ProductId, outOfStockItem.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InsufficientInventoryException>()
            .WithMessage("*Headphones*");
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifications.Verify(
            n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_WhenValidationFails_ShouldNeverCheckInventorySaveOrNotify()
    {
        var sut = CreateSut();
        var order = OrderFactory.Create(OrderFactory.Item("Gaming Mouse", 200m, 0));

        var act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>();
        _inventory.Verify(
            i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifications.Verify(
            n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

   

    [Fact]
    public async Task CheckoutAsync_WhenOrderCreatedByAutoFixture_ShouldSucceed()
    {
        var sut = CreateSut();
        var order = AutoFixtureOrders.CreateValidOrder();

        var receipt = await sut.CheckoutAsync(order);

        receipt.OrderId.Should().Be(order.Id);
        receipt.Items.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task CheckoutAsync_WhenCustomerCreatedByBogus_ShouldCopyNameAndEmailToReceipt()
    {
        var sut = CreateSut();
        var customer = BogusCustomers.Create();
        var order = OrderFactory.Create(
            customer.Name,
            customer.Email,
            OrderFactory.Item("Keyboard", 500m, 1));

        var receipt = await sut.CheckoutAsync(order);

        receipt.CustomerName.Should().Be(customer.Name);
        receipt.CustomerEmail.Should().Be(customer.Email);
    }
}
