using StockPriceSheetPrintService.Outbound.Persistence;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DbInflationStoreTests
	{
		[Fact]
		public async Task GetLastFetchedAtAsync_ReturnsNull_WhenStoreIsEmpty()
		{
			var store = new DbInflationStore(new InMemoryDbContextFactory());

			Assert.Null(await store.GetLastFetchedAtAsync(CancellationToken.None));
		}

		[Fact]
		public async Task GetCachedDataAsync_ReturnsEmpty_WhenStoreIsEmpty()
		{
			var store = new DbInflationStore(new InMemoryDbContextFactory());

			Assert.Empty(await store.GetCachedDataAsync(CancellationToken.None));
		}

		[Fact]
		public async Task UpsertAsync_WithNoPoints_DoesNothing()
		{
			var store = new DbInflationStore(new InMemoryDbContextFactory());

			await store.UpsertAsync([], CancellationToken.None);

			Assert.Null(await store.GetLastFetchedAtAsync(CancellationToken.None));
		}

		[Fact]
		public async Task UpsertAsync_ThenGetCachedDataAsync_ReturnsPointsOrderedByMonth_AndSetsFetchedAt()
		{
			var store = new DbInflationStore(new InMemoryDbContextFactory());

			await store.UpsertAsync(
			[
				new BenchmarkDataPoint(new DateTime(2026, 9, 1), 102.6),
				new BenchmarkDataPoint(new DateTime(2025, 9, 1), 100.41),
			], CancellationToken.None);

			var data = await store.GetCachedDataAsync(CancellationToken.None);
			Assert.Equal(2, data.Count);
			Assert.Equal(new DateTime(2025, 9, 1), data[0].Date);
			Assert.Equal(100.41, data[0].Value);
			Assert.Equal(new DateTime(2026, 9, 1), data[1].Date);
			Assert.NotNull(await store.GetLastFetchedAtAsync(CancellationToken.None));
		}

		[Fact]
		public async Task UpsertAsync_OverwritesExistingMonth_AndAddsNewOnes()
		{
			var store = new DbInflationStore(new InMemoryDbContextFactory());
			await store.UpsertAsync([new BenchmarkDataPoint(new DateTime(2026, 8, 1), 102.0)], CancellationToken.None);

			await store.UpsertAsync(
			[
				new BenchmarkDataPoint(new DateTime(2026, 8, 1), 102.3),
				new BenchmarkDataPoint(new DateTime(2026, 9, 1), 102.6),
			], CancellationToken.None);

			var data = await store.GetCachedDataAsync(CancellationToken.None);
			Assert.Equal(2, data.Count);
			Assert.Equal(102.3, data[0].Value);
			Assert.Equal(102.6, data[1].Value);
		}
	}
}
