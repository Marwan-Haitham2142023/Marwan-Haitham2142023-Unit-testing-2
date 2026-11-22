using Bogus;

namespace Simulation.Checkout.Tests.TestData;

public sealed record TestCustomer(string Name, string Email);

public static class BogusCustomers
{
    private const int Seed = 20261122;

    public static TestCustomer Create() =>
        new Faker<TestCustomer>()
            .UseSeed(Seed)
            .CustomInstantiator(f => new TestCustomer(
                f.Name.FullName(),
                f.Internet.Email()))
            .Generate();
}
