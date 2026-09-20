using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class TriggerReportServiceImplTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		[Fact]
		public async Task TrySendPendingReportAsync_ReturnsFalse_AndDoesNotCallDiscord_WhenNoReportIsPending()
		{
			var store = new FakePendingReportStore();
			var discordNotifier = new FakeDiscordNotifier();
			var service = new TriggerReportServiceImpl(store, discordNotifier);

			var result = await service.TrySendPendingReportAsync(Ctx, CancellationToken.None);

			Assert.False(result);
		}

		[Fact]
		public async Task TrySendPendingReportAsync_SendsPendingReport_ThenClearsStore_WhenReportIsPending()
		{
			var store = new FakePendingReportStore();
			var report = new ScheduledReport(
				new PortfolioValues(1m, 2m, 3m),
				PreviousDayValue: 4m,
				TransferAmount: null,
				GeminiInsights: null,
				ScheduledAtUtc: DateTime.UtcNow,
				Atm: "No");
			store.Set(report);
			var discordNotifier = new FakeDiscordNotifier();
			var service = new TriggerReportServiceImpl(store, discordNotifier);

			var result = await service.TrySendPendingReportAsync(Ctx, CancellationToken.None);

			Assert.True(result);
			Assert.Equal(1, discordNotifier.MorningReportSentCount);
			Assert.Equal(report.Values, discordNotifier.LastMorningReportValues);
			Assert.Equal(1, store.ClearCallCount);
			Assert.Null(store.Get());
		}
	}
}
