using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IYahooFinanceClient
	{
		Task<FundNav?> GetFromYahooApiAsync(string ticker, ClientContext ctx, CancellationToken token);
		Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, DateTimeOffset from, DateTimeOffset to, ClientContext ctx, CancellationToken ct);
	}
}
