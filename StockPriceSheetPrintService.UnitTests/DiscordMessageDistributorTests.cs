using Microsoft.Extensions.DependencyInjection;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Inbound;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DiscordMessageDistributorTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private static (DiscordMessageDistributor Distributor, FakePendingReportStore Store, FakeDiscordNotifier Notifier) CreateDistributor()
		{
			var pendingReportStore = new FakePendingReportStore();
			var discordNotifier = new FakeDiscordNotifier();

			var services = new ServiceCollection();
			services.AddSingleton<IPendingReportStore>(pendingReportStore);
			services.AddSingleton<IDiscordNotifier>(discordNotifier);
			services.AddScoped<ITriggerReportService, TriggerReportServiceImpl>();
			var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

			var distributor = new DiscordMessageDistributor(
				new FakeNordnetStore(),
				new FakeJuneStore(),
				scopeFactory,
				new FakeNordnetSymbolStore(),
				new FakeSchedulerStatus(),
				new FakeGeminiToggle());

			return (distributor, pendingReportStore, discordNotifier);
		}

		[Fact]
		public async Task HandleComponentAsync_BtnSendReport_ReturnsNoPendingMessage_WhenNoReportIsPending()
		{
			var (distributor, _, discordNotifier) = CreateDistributor();

			var response = await distributor.HandleComponentAsync(new BotComponentCommand("btn_send_report"), Ctx, CancellationToken.None);

			var textResponse = Assert.IsType<TextBotResponse>(response);
			Assert.Contains("No pending report found", textResponse.Text);
			Assert.Equal(0, discordNotifier.MorningReportSentCount);
		}

		[Fact]
		public async Task HandleComponentAsync_BtnSendReport_SendsPendingReport_WhenReportIsPending()
		{
			var (distributor, store, discordNotifier) = CreateDistributor();
			store.Set(new ScheduledReport(
				new PortfolioValues(1m, 2m, 3m),
				PreviousDayValue: 4m,
				TransferAmount: null,
				GeminiInsights: null,
				ScheduledAtUtc: DateTime.UtcNow,
				Atm: "No"));

			var response = await distributor.HandleComponentAsync(new BotComponentCommand("btn_send_report"), Ctx, CancellationToken.None);

			var textResponse = Assert.IsType<TextBotResponse>(response);
			Assert.Contains("Morning report sendt", textResponse.Text);
			Assert.Equal(1, discordNotifier.MorningReportSentCount);
			Assert.Equal(1, store.ClearCallCount);
		}
	}
}
