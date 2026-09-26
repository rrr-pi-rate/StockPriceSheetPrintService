namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IHistoricalExchangeRateProvider
	{
		Task<decimal?> GetRateToDkkAsync(string currency, DateOnly date, ClientContext ctx, CancellationToken ct);
	}
}
