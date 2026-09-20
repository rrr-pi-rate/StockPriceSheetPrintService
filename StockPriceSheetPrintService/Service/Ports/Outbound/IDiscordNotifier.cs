using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IDiscordNotifier
	{
		Task SendMorningReportAsync(PortfolioValues values, decimal dayBeforeValue, decimal? lastTransferAmount, string? geminiInsights, string atm, CancellationToken stoppingToken);
		Task SendLoginUrlAsync(string loginUrl, CancellationToken stoppingToken);
	}
}
