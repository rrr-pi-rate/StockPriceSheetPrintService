namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IExchangeRateStore
	{
		Task<Dictionary<DateOnly, decimal>> GetCachedRatesAsync(string currency, DateOnly from, DateOnly to, CancellationToken ct);
		Task InsertRangeAsync(string currency, IReadOnlyDictionary<DateOnly, decimal> rates, CancellationToken ct);
	}
}
