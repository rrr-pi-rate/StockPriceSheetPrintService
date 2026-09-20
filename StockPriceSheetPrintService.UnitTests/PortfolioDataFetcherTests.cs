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
	}
}
