using Microsoft.EntityFrameworkCore;
using StockPriceSheetPrintService.Outbound.Persistence;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DbBenchmarkStoreTests
	{
		private static IDbContextFactory<StockDbContext> CreateInMemoryFactory() => new InMemoryDbContextFactory();

		[Fact]
		public async Task GetLatestDateAsync_ReturnsNull_WhenNoDataExistsForSymbol()
		{
			var store = new DbBenchmarkStore(CreateInMemoryFactory());

			var latest = await store.GetLatestDateAsync("^GSPC", CancellationToken.None);

			Assert.Null(latest);
		}

		[Fact]
		public async Task InsertAsync_ThenGetLatestDateAsync_ReturnsMaxDate_ForThatSymbolOnly()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbBenchmarkStore(factory);

			await store.InsertAsync("^GSPC", [
				new BenchmarkDataPoint(new DateTime(2026, 1, 2), 100.0),
				new BenchmarkDataPoint(new DateTime(2026, 1, 5), 105.0),
			], CancellationToken.None);
			await store.InsertAsync("OMXC25", [
				new BenchmarkDataPoint(new DateTime(2026, 1, 10), 200.0),
			], CancellationToken.None);

			var latest = await store.GetLatestDateAsync("^GSPC", CancellationToken.None);

			Assert.Equal(new DateOnly(2026, 1, 5), latest);
		}

		[Fact]
		public async Task InsertAsync_ThenGetCachedDataAsync_ReturnsPointsOrderedByDate_ForThatSymbolOnly()
		{
			var factory = CreateInMemoryFactory();
			var store = new DbBenchmarkStore(factory);

			await store.InsertAsync("^GSPC", [
				new BenchmarkDataPoint(new DateTime(2026, 1, 5), 105.0),
				new BenchmarkDataPoint(new DateTime(2026, 1, 2), 100.0),
			], CancellationToken.None);
			await store.InsertAsync("OMXC25", [
				new BenchmarkDataPoint(new DateTime(2026, 1, 1), 999.0),
			], CancellationToken.None);

			var points = await store.GetCachedDataAsync("^GSPC", CancellationToken.None);

			Assert.Equal(2, points.Count);
			Assert.Equal(new DateTime(2026, 1, 2), points[0].Date);
			Assert.Equal(100.0, points[0].Value);
			Assert.Equal(new DateTime(2026, 1, 5), points[1].Date);
			Assert.Equal(105.0, points[1].Value);
		}

		[Fact]
		public async Task GetCachedDataAsync_ReturnsEmpty_WhenNoDataExistsForSymbol()
		{
			var store = new DbBenchmarkStore(CreateInMemoryFactory());

			var points = await store.GetCachedDataAsync("^GSPC", CancellationToken.None);

			Assert.Empty(points);
		}
	}
}
