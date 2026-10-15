using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeCacheStore : ICacheStore
	{
		public Dictionary<string, (object Value, DateTimeOffset ExpiresAt)> Entries { get; } = [];
		public List<string> RemovedKeys { get; } = [];

		public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class =>
			Task.FromResult(Entries.TryGetValue(key, out var entry) ? entry.Value as T : null);

		public Task SetAsync<T>(string key, T value, DateTimeOffset expiresAt, CancellationToken ct) where T : class
		{
			Entries[key] = (value, expiresAt);
			return Task.CompletedTask;
		}

		public Task RemoveAsync(string key, CancellationToken ct)
		{
			RemovedKeys.Add(key);
			Entries.Remove(key);
			return Task.CompletedTask;
		}
	}
}
