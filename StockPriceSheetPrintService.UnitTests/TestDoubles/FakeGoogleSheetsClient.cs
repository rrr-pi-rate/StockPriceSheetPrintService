using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeGoogleSheetsClient : IGoogleSheetsClient
	{
		public int UpdateCallCount { get; private set; }
		public PortfolioValues? LastValues { get; private set; }
		public decimal DayBeforeValueToReturn { get; set; }
		public List<(DateOnly Date, decimal Value)> HistoricalData { get; set; } = [];
		public Exception? HistoricalDataException { get; set; }

		public Task<decimal> UpdateGoogleSheetsCellAsync(string spreadsheetId, string sheetName, PortfolioValues values, ClientContext ctx, CancellationToken ct)
		{
			UpdateCallCount++;
			LastValues = values;
			return Task.FromResult(DayBeforeValueToReturn);
		}

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(string spreadsheetId, string sheetName, ClientContext ctx, CancellationToken ct) =>
			HistoricalDataException is not null
				? Task.FromException<List<(DateOnly Date, decimal Value)>>(HistoricalDataException)
				: Task.FromResult(HistoricalData);
	}
}
