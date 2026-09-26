namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IHistoricalExchangeRateProvider
	{
		Task<IReadOnlyDictionary<DateOnly, decimal>> GetRatesToDkkAsync(string currency, DateOnly from, DateOnly to, ClientContext ctx, CancellationToken ct);
	}
}
