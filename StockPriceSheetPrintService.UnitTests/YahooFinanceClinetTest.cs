using StockPriceSheetPrintService.Outbound.HtmlScraping;
using StockPriceSheetPrintService.Outbound.YahooFinance;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class YahooFinanceClinetTest
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private static YahooFinanceClient CreateScraper(string html, out TestLogger<YahooFinanceClient> logger)
		{
			logger = new TestLogger<YahooFinanceClient>();
			var client = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, html));
			return new YahooFinanceClient(client, logger);
		}

		private const string YahooJsonWithCurrency = """
			{"chart":{"result":[{"meta":{"regularMarketPrice":18.34,"regularMarketTime":1789745766,"currency":"EUR"}}]}}
			""";

		private const string YahooJsonWithoutCurrency = """
			{"chart":{"result":[{"meta":{"regularMarketPrice":18.34,"regularMarketTime":1789745766}}]}}
			""";

		[Fact]
		public async Task GetFromYahooApiAsync_ReturnsPriceDateAndCurrency_FromChartResponse()
		{
			var scraper = CreateScraper(YahooJsonWithCurrency, out _);

			var result = await scraper.GetFromYahooApiAsync("2B76.DE", Ctx, CancellationToken.None);

			Assert.NotNull(result);
			Assert.Equal(18.34m, result!.Nav);
			Assert.Equal("EUR", result.Currency);
			Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1789745766).LocalDateTime, result.Date);
		}

		[Fact]
		public async Task GetFromYahooApiAsync_ReturnsNullCurrency_WhenNotPresentInResponse()
		{
			var scraper = CreateScraper(YahooJsonWithoutCurrency, out _);

			var result = await scraper.GetFromYahooApiAsync("2B76.DE", Ctx, CancellationToken.None);

			Assert.NotNull(result);
			Assert.Null(result!.Currency);
		}
	}
}
