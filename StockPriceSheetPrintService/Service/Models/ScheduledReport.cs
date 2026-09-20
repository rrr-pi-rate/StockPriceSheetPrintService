namespace StockPriceSheetPrintService.Service.Models
{
	public record ScheduledReport(
		PortfolioValues Values,
		decimal PreviousDayValue,
		decimal? TransferAmount,
		string? GeminiInsights,
		DateTime ScheduledAtUtc,
		string Atm
	);
}
