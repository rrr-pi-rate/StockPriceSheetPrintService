using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class PortfolioDataFetcherTransferTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private static DateOnly DaysAgo(int days) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));

		private sealed record Fixture(
			PortfolioDataFetcher Fetcher,
			FakeSaxoTokenService Token,
			FakeSaxoAccountService Saxo,
			FakeGoogleSheetsClient Sheets,
			FakeSeenTransferStore Seen);

		private static Fixture CreateFixture(bool withSheetsKey = true)
		{
			var token = new FakeSaxoTokenService();
			var saxo = new FakeSaxoAccountService();
			var sheets = new FakeGoogleSheetsClient();
			var seen = new FakeSeenTransferStore();
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(withSheetsKey ? new Dictionary<string, string?> { ["SheetsApi:SheetsKey"] = "sheet-id" } : [])
				.Build();

			var fetcher = new PortfolioDataFetcher(
				new TestLogger<PortfolioDataFetcher>(),
				config,
				token,
				saxo,
				sheets,
				new FakePortfolioCalculator(),
				seen,
				new FakeNordnetStore());

			return new Fixture(fetcher, token, saxo, sheets, seen);
		}

		[Fact]
		public async Task GetNewTransfersAsync_ReturnsTransfersAfterPreviousRowUpToYesterday_AndSavesThem()
		{
			var f = CreateFixture();
			f.Sheets.HistoricalData = [(DaysAgo(3), 1000m)];
			f.Saxo.Transfers =
			[
				new Transfer("before-previous-row", 100m, DaysAgo(4)),
				new Transfer("a", 200m, DaysAgo(2)),
				new Transfer("b", 300m, DaysAgo(1)),
				new Transfer("later", 5000m, DaysAgo(0)),
			];

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Equal(["a", "b"], report.NewTransfers.Select(t => t.Id));
			Assert.Equal(5000m, report.LaterTransfersSum);
			Assert.Equal(["a", "b"], f.Seen.Saved.Select(t => t.Id));
		}

		[Fact]
		public async Task GetNewTransfersAsync_SkipsAlreadySeenTransfers()
		{
			var f = CreateFixture();
			f.Sheets.HistoricalData = [(DaysAgo(3), 1000m)];
			f.Seen.Seen = ["a"];
			f.Saxo.Transfers = [new Transfer("a", 200m, DaysAgo(2)), new Transfer("b", 300m, DaysAgo(1))];

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Equal("b", Assert.Single(report.NewTransfers).Id);
		}

		[Fact]
		public async Task GetNewTransfersAsync_DoesNotSaveAnything_WhenNothingIsNew()
		{
			var f = CreateFixture();
			f.Saxo.Transfers = [new Transfer("later", 5000m, DaysAgo(0))];

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Empty(report.NewTransfers);
			Assert.Equal(5000m, report.LaterTransfersSum);
			Assert.Empty(f.Seen.Saved);
		}

		[Fact]
		public async Task GetNewTransfersAsync_FallsBackToDayBeforeYesterdayAsPreviousRow_WhenSheetHasNoEarlierRows()
		{
			var f = CreateFixture();
			f.Saxo.Transfers = [new Transfer("old", 100m, DaysAgo(2)), new Transfer("yesterday", 200m, DaysAgo(1))];

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Equal("yesterday", Assert.Single(report.NewTransfers).Id);
		}

		[Fact]
		public async Task GetNewTransfersAsync_FallsBackToDayBeforeYesterdayAsPreviousRow_WhenSheetsKeyIsMissing()
		{
			var f = CreateFixture(withSheetsKey: false);
			f.Saxo.Transfers = [new Transfer("old", 100m, DaysAgo(2)), new Transfer("yesterday", 200m, DaysAgo(1))];

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Equal("yesterday", Assert.Single(report.NewTransfers).Id);
		}

		[Fact]
		public async Task GetNewTransfersAsync_ReturnsEmptyReport_WhenSaxoTokenIsUnavailable()
		{
			var f = CreateFixture();
			f.Token.TokenToReturn = null;

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Empty(report.NewTransfers);
			Assert.Equal(0m, report.LaterTransfersSum);
		}

		[Fact]
		public async Task GetNewTransfersAsync_ReturnsEmptyReport_WhenFetchingTransactionsFails()
		{
			var f = CreateFixture(); // Saxo.Transfers er null, så fake kaster

			var report = await f.Fetcher.GetNewTransfersAsync(Ctx, CancellationToken.None);

			Assert.Empty(report.NewTransfers);
			Assert.Equal(0m, report.LaterTransfersSum);
		}
	}
}
