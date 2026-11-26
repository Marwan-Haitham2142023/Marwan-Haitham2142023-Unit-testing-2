# TESTING_NOTES

1. **Total tests:** 16 test methods (20 test cases with `[Theory]` rows) - passing: 20, failing: 0
2. **Line coverage %:** 90.5% overall (Application 95.8%, Domain 100%) - from coverage-report/Summary.txt
3. **Where I used the libraries:**
   - **Moq:** mocks `IInventoryService`, `IOrderRepository` and `INotificationService` in `CheckoutServiceTests` to verify save/send were called `Times.Once` on success and `Times.Never` on failure.
   - **AutoFixture:** `AutoFixtureOrders` builds a random order, customized so quantity is 1-9 and price is 0 or more.
   - **Bogus:** `BogusCustomers` generates a realistic name and email with a fixed seed, so the data is the same every run.
4. **Is it fair that 999.99 pays more than 1,000?** No - at 1,000 the customer pays 900 while at 999.99 they pay 999.99, so spending 0.01 more makes the bill about 100 cheaper; a threshold cliff like this is surprising and a tiered or marginal discount would avoid it.
5. **Bugs found:** No wrong calculations found. Observations (not fixed, `src/` unchanged):
   - The order is saved *before* the confirmation is sent, so if sending fails the order stays saved without a confirmation (BR-09 says nothing is saved on "any failure").
   - Stock is checked per line, so the same product on two lines is never checked for the combined quantity.
   - `CreatedAt` uses `DateTimeOffset.UtcNow` directly, so it cannot be controlled in tests (the snapshot scrubs it).