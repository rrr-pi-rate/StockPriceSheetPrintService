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

		public Task<decimal> UpdateGoogleSheetsCellAsync(string spreadsheetId, string sheetName, PortfolioValues values, ClientContext ctx, CancellationToken ct)
		{
			UpdateCallCount++;
			LastValues = values;
			return Task.FromResult(DayBeforeValueToReturn);
		}

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(string spreadsheetId, string sheetName, ClientContext ctx, CancellationToken ct) =>
			Task.FromResult(new List<(DateOnly Date, decimal Value)>());

		public Task<string> GetAtmValue(string spreadsheetId, string sheetName, CancellationToken ct) =>
			Task.FromResult(string.Empty);
	}
}
