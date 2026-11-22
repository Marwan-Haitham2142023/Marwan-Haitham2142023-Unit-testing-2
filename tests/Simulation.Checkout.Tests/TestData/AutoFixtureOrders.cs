using AutoFixture;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public static class AutoFixtureOrders
{
    public static Order CreateValidOrder()
    {
        var fixture = new Fixture();

        fixture.Customize<OrderItem>(composer => composer
            .With(item => item.Quantity, () => (fixture.Create<byte>() % 9) + 1)
            .With(item => item.UnitPrice, () => (decimal)fixture.Create<ushort>()));

        var items = fixture.CreateMany<OrderItem>(3).ToList();

        return fixture.Build<Order>()
            .With(order => order.Items, items)
            .Create();
    }
}
