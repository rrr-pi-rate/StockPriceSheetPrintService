using Microsoft.Extensions.Caching.Memory;
using StockPriceSheetPrintService.Outbound.Memory;

namespace StockPriceSheetPrintService.UnitTests
{
	public class InMemoryCacheStoreTests
	{
		private static InMemoryCacheStore CreateStore() => new(new MemoryCache(new MemoryCacheOptions()));

		[Fact]
		public async Task GetAsync_ReturnsNull_WhenKeyIsMissing()
		{
			var store = CreateStore();

			var result = await store.GetAsync<List<int>>("missing", CancellationToken.None);

			Assert.Null(result);
		}

		[Fact]
		public async Task GetAsync_ReturnsStoredValue_BeforeExpiry()
		{
			var store = CreateStore();
			List<int> value = [1, 2, 3];
			await store.SetAsync("key", value, DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None);

			var result = await store.GetAsync<List<int>>("key", CancellationToken.None);

			Assert.Same(value, result);
		}

		[Fact]
		public async Task GetAsync_ReturnsNull_AfterExpiry()
		{
			var store = CreateStore();
			await store.SetAsync("key", new List<int> { 1 }, DateTimeOffset.UtcNow.AddSeconds(-1), CancellationToken.None);

			var result = await store.GetAsync<List<int>>("key", CancellationToken.None);

			Assert.Null(result);
		}

		[Fact]
		public async Task RemoveAsync_DeletesStoredValue()
		{
			var store = CreateStore();
			await store.SetAsync("key", new List<int> { 1 }, DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None);

			await store.RemoveAsync("key", CancellationToken.None);

			Assert.Null(await store.GetAsync<List<int>>("key", CancellationToken.None));
		}
	}
}
