using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IGoogleSheetsClient
	{
		Task<decimal> UpdateGoogleSheetsCellAsync(string spreadsheetId, string sheetName, PortfolioValues values, ClientContext ctx, CancellationToken ct);
		Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(string spreadsheetId, string sheetName, ClientContext ctx, CancellationToken ct);
		Task<string> GetAtmValue(string spreadsheetId, string sheetName, CancellationToken ct);
	}
}
