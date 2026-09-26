using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IBenchmarkStore
	{
		Task<DateOnly?> GetLatestDateAsync(string symbol, CancellationToken ct);
		Task InsertAsync(string symbol, IReadOnlyList<BenchmarkDataPoint> points, CancellationToken ct);
		Task<IReadOnlyList<BenchmarkDataPoint>> GetCachedDataAsync(string symbol, CancellationToken ct);
	}
}
