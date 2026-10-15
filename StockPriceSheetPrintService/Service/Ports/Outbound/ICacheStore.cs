namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface ICacheStore
	{
		Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;
		Task SetAsync<T>(string key, T value, DateTimeOffset expiresAt, CancellationToken ct) where T : class;
		Task RemoveAsync(string key, CancellationToken ct);
	}
}
