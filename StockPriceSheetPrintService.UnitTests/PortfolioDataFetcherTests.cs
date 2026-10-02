using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class PortfolioDataFetcherTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		[Fact]
		public async Task GetNordnetValueAsync_ReturnsStockValuePlusCash()
		{
			var portfolioCalculator = new FakePortfolioCalculator { StockValueToReturn = 1000m };
			var nordnetStore = new FakeNordnetStore { Cash = new(250m, DateTime.UtcNow) };

			var fetcher = new PortfolioDataFetcher(
				new TestLogger<PortfolioDataFetcher>(),
				new ConfigurationBuilder().Build(),
				new FakeSaxoTokenService(),
				new FakeSaxoAccountService(),
				new FakeGoogleSheetsClient(),
				portfolioCalculator,
				new FakeSeenTransferStore(),
				nordnetStore);

			var result = await fetcher.GetNordnetValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(1250m, result);
			Assert.Equal(1, portfolioCalculator.CalculateTotalStockValueCallCount);
		}

		private static PortfolioDataFetcher CreateFetcher(FakeGoogleSheetsClient sheets, bool withSheetsKey = true)
		{
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(withSheetsKey ? new Dictionary<string, string?> { ["SheetsApi:SheetsKey"] = "sheet-id" } : [])
				.Build();

			return new PortfolioDataFetcher(
				new TestLogger<PortfolioDataFetcher>(),
				config,
				new FakeSaxoTokenService(),
				new FakeSaxoAccountService(),
				sheets,
				new FakePortfolioCalculator(),
				new FakeSeenTransferStore(),
				new FakeNordnetStore());
		}

		[Fact]
		public async Task GetHighestValueAsync_ReturnsMaxOfHistoricalValues()
		{
			var sheets = new FakeGoogleSheetsClient
			{
				HistoricalData =
				[
					(new DateOnly(2026, 1, 1), 500m),
					(new DateOnly(2026, 1, 2), 900m),
					(new DateOnly(2026, 1, 3), 700m),
				]
			};

			var result = await CreateFetcher(sheets).GetHighestValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(900m, result);
		}

		[Fact]
		public async Task GetHighestValueAsync_ReturnsNull_WhenNoHistory()
		{
			var result = await CreateFetcher(new FakeGoogleSheetsClient()).GetHighestValueAsync(Ctx, CancellationToken.None);

			Assert.Null(result);
		}

		[Fact]
		public async Task GetHighestValueAsync_ReturnsNull_WhenSheetsKeyMissing()
		{
			var result = await CreateFetcher(new FakeGoogleSheetsClient(), withSheetsKey: false).GetHighestValueAsync(Ctx, CancellationToken.None);

			Assert.Null(result);
		}

		[Fact]
		public async Task GetHighestValueAsync_ReturnsNull_WhenSheetsClientThrows()
		{
			var sheets = new FakeGoogleSheetsClient { HistoricalDataException = new InvalidOperationException("boom") };

			var result = await CreateFetcher(sheets).GetHighestValueAsync(Ctx, CancellationToken.None);

			Assert.Null(result);
		}
	}
}
