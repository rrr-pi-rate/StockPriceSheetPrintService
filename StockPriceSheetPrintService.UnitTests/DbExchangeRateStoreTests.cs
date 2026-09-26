using Microsoft.EntityFrameworkCore;
using StockPriceSheetPrintService.Outbound.Persistence;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DbExchangeRateStoreTests
	{
		private static IDbContextFactory<StockDbContext> CreateInMemoryFactory() => new InMemoryDbContextFactory();

		[Fact]
		public async Task GetCachedRateAsync_ReturnsNull_WhenNoRateExistsForCurrencyAndDate()
		{
			var store = new DbExchangeRateStore(CreateInMemoryFactory());

			var rate = await store.GetCachedRateAsync("USD", new DateOnly(2026, 1, 2), CancellationToken.None);

			Assert.Null(rate);
		}

		[Fact]
		public async Task InsertAsync_ThenGetCachedRateAsync_ReturnsTheInsertedRate()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbExchangeRateStore(factory);

			await store.InsertAsync("USD", new DateOnly(2026, 1, 2), 6.83m, CancellationToken.None);

			var rate = await store.GetCachedRateAsync("USD", new DateOnly(2026, 1, 2), CancellationToken.None);

			Assert.Equal(6.83m, rate);
		}

		[Fact]
		public async Task GetCachedRateAsync_DistinguishesBetweenCurrenciesAndDates()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbExchangeRateStore(factory);

			await store.InsertAsync("USD", new DateOnly(2026, 1, 2), 6.83m, CancellationToken.None);
			await store.InsertAsync("EUR", new DateOnly(2026, 1, 2), 7.46m, CancellationToken.None);
			await store.InsertAsync("USD", new DateOnly(2026, 1, 3), 6.90m, CancellationToken.None);

			Assert.Equal(6.83m, await store.GetCachedRateAsync("USD", new DateOnly(2026, 1, 2), CancellationToken.None));
			Assert.Equal(7.46m, await store.GetCachedRateAsync("EUR", new DateOnly(2026, 1, 2), CancellationToken.None));
			Assert.Equal(6.90m, await store.GetCachedRateAsync("USD", new DateOnly(2026, 1, 3), CancellationToken.None));
			Assert.Null(await store.GetCachedRateAsync("EUR", new DateOnly(2026, 1, 3), CancellationToken.None));
		}
	}
}
