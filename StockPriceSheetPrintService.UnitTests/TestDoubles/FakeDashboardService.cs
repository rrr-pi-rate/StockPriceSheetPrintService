using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Inbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeDashboardService : IDashboardService
	{
		public List<(DateOnly Date, decimal Value)> Entries { get; set; } = [];

		public Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, ClientContext ctx, CancellationToken ct)
		{
			throw new NotImplementedException();
		}

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(ClientContext ctx, CancellationToken ct) =>
			Task.FromResult(Entries);
	}
}
