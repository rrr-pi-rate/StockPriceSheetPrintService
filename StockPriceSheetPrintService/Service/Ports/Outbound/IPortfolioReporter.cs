using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IPortfolioReporter
	{
		Task ReportMorningAsync(PortfolioValues values, decimal previousDayValue, List<Transfer> newTransfers, bool sendDiscordImmediately, string? geminiInsights, string atm, ClientContext ctx, CancellationToken ct);
		Task UpdateGoogleSheetsAsync(PortfolioValues values, ClientContext ctx, CancellationToken ct);
	}
}
