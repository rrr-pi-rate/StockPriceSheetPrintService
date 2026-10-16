using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IInflationStore
	{
		Task<DateTimeOffset?> GetLastFetchedAtAsync(CancellationToken ct);
		Task UpsertAsync(IReadOnlyList<BenchmarkDataPoint> points, CancellationToken ct);
		Task<IReadOnlyList<BenchmarkDataPoint>> GetCachedDataAsync(CancellationToken ct);
	}
}
