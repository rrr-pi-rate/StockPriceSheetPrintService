using Microsoft.EntityFrameworkCore;
using StockPriceSheetPrintService.Outbound.Persistence;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DbExchangeRateStoreTests
	{
		private static IDbContextFactory<StockDbContext> CreateInMemoryFactory() => new InMemoryDbContextFactory();

		[Fact]
		public async Task GetCachedRatesAsync_ReturnsEmpty_WhenNoRatesExistForCurrency()
		{
			var store = new DbExchangeRateStore(CreateInMemoryFactory());

			var rates = await store.GetCachedRatesAsync("USD", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CancellationToken.None);

			Assert.Empty(rates);
		}

		[Fact]
		public async Task InsertRangeAsync_ThenGetCachedRatesAsync_ReturnsAllInsertedRates()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbExchangeRateStore(factory);

			await store.InsertRangeAsync("USD", new Dictionary<DateOnly, decimal>
			{
				[new DateOnly(2026, 1, 2)] = 6.83m,
				[new DateOnly(2026, 1, 5)] = 6.90m,
			}, CancellationToken.None);

			var rates = await store.GetCachedRatesAsync("USD", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CancellationToken.None);

			Assert.Equal(2, rates.Count);
			Assert.Equal(6.83m, rates[new DateOnly(2026, 1, 2)]);
			Assert.Equal(6.90m, rates[new DateOnly(2026, 1, 5)]);
		}

		[Fact]
		public async Task GetCachedRatesAsync_OnlyReturnsRatesWithinRequestedRangeAndCurrency()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbExchangeRateStore(factory);

			await store.InsertRangeAsync("USD", new Dictionary<DateOnly, decimal>
			{
				[new DateOnly(2026, 1, 2)] = 6.83m,
				[new DateOnly(2026, 2, 1)] = 6.95m,
			}, CancellationToken.None);
			await store.InsertRangeAsync("EUR", new Dictionary<DateOnly, decimal>
			{
				[new DateOnly(2026, 1, 2)] = 7.46m,
			}, CancellationToken.None);

			var rates = await store.GetCachedRatesAsync("USD", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CancellationToken.None);

			var only = Assert.Single(rates);
			Assert.Equal(new DateOnly(2026, 1, 2), only.Key);
			Assert.Equal(6.83m, only.Value);
		}

		[Fact]
		public async Task InsertRangeAsync_WithEmptyDictionary_DoesNotThrow_AndInsertsNothing()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbExchangeRateStore(factory);

			await store.InsertRangeAsync("USD", new Dictionary<DateOnly, decimal>(), CancellationToken.None);

			var rates = await store.GetCachedRatesAsync("USD", DateOnly.MinValue, DateOnly.MaxValue, CancellationToken.None);
			Assert.Empty(rates);
		}
	}
}
