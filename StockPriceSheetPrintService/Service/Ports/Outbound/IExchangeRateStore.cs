namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IExchangeRateStore
	{
		Task<decimal?> GetCachedRateAsync(string currency, DateOnly date, CancellationToken ct);
		Task InsertAsync(string currency, DateOnly date, decimal rateToDkk, CancellationToken ct);
	}
}
