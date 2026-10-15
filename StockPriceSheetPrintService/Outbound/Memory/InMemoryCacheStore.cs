using Microsoft.Extensions.Caching.Memory;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Outbound.Memory
{
	public class InMemoryCacheStore(IMemoryCache memoryCache) : ICacheStore
	{
		public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class =>
			Task.FromResult(memoryCache.TryGetValue(key, out T? value) ? value : null);

		public Task SetAsync<T>(string key, T value, DateTimeOffset expiresAt, CancellationToken ct) where T : class
		{
			memoryCache.Set(key, value, expiresAt);
			return Task.CompletedTask;
		}

		public Task RemoveAsync(string key, CancellationToken ct)
		{
			memoryCache.Remove(key);
			return Task.CompletedTask;
		}
	}
}
