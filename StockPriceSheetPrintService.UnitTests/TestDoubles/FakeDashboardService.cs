using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Inbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeDashboardService : IDashboardService
	{
		public List<(DateOnly Date, decimal Value)> Entries { get; set; } = [];

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(ClientContext ctx, CancellationToken ct) =>
			Task.FromResult(Entries);
	}
}
