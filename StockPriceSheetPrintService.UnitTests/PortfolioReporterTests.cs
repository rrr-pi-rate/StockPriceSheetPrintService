using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class PortfolioReporterTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private sealed record Fixture(
			PortfolioReporter Reporter,
			FakeDiscordNotifier DiscordNotifier,
			FakeGoogleSheetsClient GoogleSheetsClient,
			FakePendingReportStore PendingReportStore);

		private static Fixture CreateReporter(bool withSheetsKey = true)
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(withSheetsKey
					? new Dictionary<string, string?> { ["SheetsApi:SheetsKey"] = "sheet-123" }
					: new Dictionary<string, string?>())
				.Build();

			var discordNotifier = new FakeDiscordNotifier();
			var googleSheetsClient = new FakeGoogleSheetsClient();
			var pendingReportStore = new FakePendingReportStore();

			var reporter = new PortfolioReporter(
				new TestLogger<PortfolioReporter>(),
				discordNotifier,
				googleSheetsClient,
				configuration,
				pendingReportStore);

			return new Fixture(reporter, discordNotifier, googleSheetsClient, pendingReportStore);
		}

		[Fact]
		public async Task UpdateGoogleSheetsAsync_WritesTheGivenValues_WhenSheetsKeyIsConfigured()
		{
			var fixture = CreateReporter();
			var values = new PortfolioValues(1m, 2m, 3m);

			await fixture.Reporter.UpdateGoogleSheetsAsync(values, Ctx, CancellationToken.None);

			Assert.Equal(1, fixture.GoogleSheetsClient.UpdateCallCount);
			Assert.Equal(values, fixture.GoogleSheetsClient.LastValues);
		}

		[Fact]
		public async Task UpdateGoogleSheetsAsync_SkipsWrite_WhenSheetsKeyIsNotConfigured()
		{
			var fixture = CreateReporter(withSheetsKey: false);

			await fixture.Reporter.UpdateGoogleSheetsAsync(new PortfolioValues(1m, 2m, 3m), Ctx, CancellationToken.None);

			Assert.Equal(0, fixture.GoogleSheetsClient.UpdateCallCount);
		}

		[Fact]
		public async Task ReportMorningAsync_SendsToDiscordImmediately_WhenRequested()
		{
			var fixture = CreateReporter();
			var values = new PortfolioValues(10m, 20m, 30m);

			await fixture.Reporter.ReportMorningAsync(
				values, previousDayValue: 5m, newTransfers: [], sendDiscordImmediately: true,
				geminiInsights: null, atm: "No", Ctx, CancellationToken.None);

			Assert.Equal(1, fixture.DiscordNotifier.MorningReportSentCount);
			Assert.Equal(values, fixture.DiscordNotifier.LastMorningReportValues);
			Assert.Null(fixture.PendingReportStore.Get());
		}

		[Fact]
		public async Task ReportMorningAsync_SchedulesReportForLater_WhenNotSendingImmediately()
		{
			var fixture = CreateReporter();
			var values = new PortfolioValues(10m, 20m, 30m);

			await fixture.Reporter.ReportMorningAsync(
				values, previousDayValue: 5m, newTransfers: [], sendDiscordImmediately: false,
				geminiInsights: "insight", atm: "No", Ctx, CancellationToken.None);

			var pending = fixture.PendingReportStore.Get();
			Assert.NotNull(pending);
			Assert.Equal(values, pending.Values);
			Assert.Equal(5m, pending.PreviousDayValue);
			Assert.Equal(0, fixture.DiscordNotifier.MorningReportSentCount);
		}
	}
}
