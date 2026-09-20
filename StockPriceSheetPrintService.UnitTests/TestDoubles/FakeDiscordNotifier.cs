using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeDiscordNotifier : IDiscordNotifier
	{
		public int LoginUrlSentCount { get; private set; }
		public string? LastLoginUrl { get; private set; }
		public int MorningReportSentCount { get; private set; }
		public PortfolioValues? LastMorningReportValues { get; private set; }

		public Task SendLoginUrlAsync(string loginUrl, CancellationToken stoppingToken)
		{
			LoginUrlSentCount++;
			LastLoginUrl = loginUrl;
			return Task.CompletedTask;
		}

		public Task SendMorningReportAsync(PortfolioValues values, decimal dayBeforeValue, decimal? lastTransferAmount, string? geminiInsights, string atm, CancellationToken stoppingToken)
		{
			MorningReportSentCount++;
			LastMorningReportValues = values;
			return Task.CompletedTask;
		}
	}
}
