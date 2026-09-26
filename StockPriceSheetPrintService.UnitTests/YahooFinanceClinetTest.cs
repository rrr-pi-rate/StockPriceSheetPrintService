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
			var client = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, html))
			{
				BaseAddress = new Uri("https://query1.finance.yahoo.com/")
			};
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

		private const string YahooChartJson = """
			{"chart":{"result":[{"meta":{"currency":"USD","symbol":"^GSPC"},"timestamp":[1735776000,1735862400],"indicators":{"quote":[{"close":[4000.5,4010.25]}]}}]}}
			""";

		private const string YahooChartJsonWithNullClose = """
			{"chart":{"result":[{"meta":{"currency":"USD","symbol":"^GSPC"},"timestamp":[1735776000,1735862400],"indicators":{"quote":[{"close":[4000.5,null]}]}}]}}
			""";

		private const string YahooChartErrorJson = """
			{"chart":{"result":null,"error":{"code":"Not Found","description":"No data found"}}}
			""";

		[Fact]
		public async Task GetBenchmarkDataAsync_ReturnsCurrencyAndPoints_FromChartResponse()
		{
			var scraper = CreateScraper(YahooChartJson, out _);

			var quote = await scraper.GetBenchmarkDataAsync("^GSPC", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow, Ctx, CancellationToken.None);

			Assert.Equal("USD", quote.Currency);
			Assert.Equal(2, quote.Points.Count);
			Assert.Equal(4000.5, quote.Points[0].Value);
			Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1735776000).UtcDateTime.Date, quote.Points[0].Date);
			Assert.Equal(4010.25, quote.Points[1].Value);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_SkipsNullCloseValues()
		{
			var scraper = CreateScraper(YahooChartJsonWithNullClose, out _);

			var quote = await scraper.GetBenchmarkDataAsync("^GSPC", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow, Ctx, CancellationToken.None);

			Assert.Single(quote.Points);
			Assert.Equal(4000.5, quote.Points[0].Value);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_ReturnsEmptyQuote_WhenChartHasError()
		{
			var scraper = CreateScraper(YahooChartErrorJson, out _);

			var quote = await scraper.GetBenchmarkDataAsync("^GSPC", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow, Ctx, CancellationToken.None);

			Assert.Null(quote.Currency);
			Assert.Empty(quote.Points);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_ReturnsEmptyQuote_AndLogsError_WhenHttpRequestFails()
		{
			var logger = new TestLogger<YahooFinanceClient>();
			var client = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "boom"))
			{
				BaseAddress = new Uri("https://query1.finance.yahoo.com/")
			};
			var scraper = new YahooFinanceClient(client, logger);

			var quote = await scraper.GetBenchmarkDataAsync("^GSPC", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow, Ctx, CancellationToken.None);

			Assert.Null(quote.Currency);
			Assert.Empty(quote.Points);
			Assert.Contains(logger.Messages, m => m.Contains("^GSPC"));
		}
	}
}
