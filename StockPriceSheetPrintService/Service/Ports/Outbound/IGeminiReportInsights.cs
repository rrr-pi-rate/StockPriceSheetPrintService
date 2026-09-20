using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IGeminiReportInsights
	{
		Task<string?> GetInsightsAsync(
			PortfolioValues values, decimal previousDayValue,
			List<Transfer> newTransfers,
			List<string> nordnetTickers,
			List<Instrument> saxoPositions,
			ClientContext ctx,
			CancellationToken ct);
	}
}
