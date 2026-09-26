using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Inbound
{
	public interface IDashboardService
	{
		Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(ClientContext ctx, CancellationToken ct);
		Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, ClientContext ctx, CancellationToken ct);
	}
}
