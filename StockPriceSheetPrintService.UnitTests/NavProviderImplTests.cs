using StockPriceSheetPrintService.Outbound.HtmlScraping;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class NavProviderImplTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private static NavProviderImpl CreateScraper(string html, out TestLogger<NavProviderImpl> logger)
		{
			logger = new TestLogger<NavProviderImpl>();
			var client = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, html));
			return new NavProviderImpl(client, logger);
		}

		private const string ValidHtml = """
			<div class="fund-number">
				<small class="description">Indre værdi pr. 18.09.2026</small>
				<p class="value">123,45</p>
			</div>
			""";

		[Fact]
		public async Task GetJuneNavAsync_ReturnsNav_WhenHtmlIsWellFormed()
		{
			var scraper = CreateScraper(ValidHtml, out var logger);

			var result = await scraper.GetJuneNavAsync("https://example.com/june", Ctx, CancellationToken.None);

			Assert.NotNull(result);
			Assert.Equal(123.45m, result!.Nav);
			Assert.Equal(new DateTime(2026, 9, 18), result.Date);
			Assert.Empty(logger.Messages);
		}

		[Fact]
		public async Task GetJuneNavAsync_ReturnsNull_AndLogs_WhenValueNodeIsMissing()
		{
			const string html = "<div class=\"fund-number\"><small class=\"description\">Indre værdi pr. 18.09.2026</small></div>";
			var scraper = CreateScraper(html, out var logger);

			var result = await scraper.GetJuneNavAsync("https://example.com/june", Ctx, CancellationToken.None);

			Assert.Null(result);
			Assert.Contains(logger.Messages, m => m.Contains("Value node not found"));
		}

		[Fact]
		public async Task GetJuneNavAsync_ReturnsNull_AndLogs_WhenNavTextIsNotANumber()
		{
			const string html = """
				<div class="fund-number">
					<small class="description">Indre værdi pr. 18.09.2026</small>
					<p class="value">ikke-et-tal</p>
				</div>
				""";
			var scraper = CreateScraper(html, out var logger);

			var result = await scraper.GetJuneNavAsync("https://example.com/june", Ctx, CancellationToken.None);

			Assert.Null(result);
			Assert.Contains(logger.Messages, m => m.Contains("Could not parse NAV value") && m.Contains("ikke-et-tal"));
		}

		[Fact]
		public async Task GetJuneNavAsync_ReturnsNull_AndLogs_WhenDescriptionHasNoDate()
		{
			const string html = """
				<div class="fund-number">
					<small class="description">Indre værdi pr. ukendt dato</small>
					<p class="value">123,45</p>
				</div>
				""";
			var scraper = CreateScraper(html, out var logger);

			var result = await scraper.GetJuneNavAsync("https://example.com/june", Ctx, CancellationToken.None);

			Assert.Null(result);
			Assert.Contains(logger.Messages, m => m.Contains("No date found"));
		}

		[Fact]
		public async Task GetJuneNavAsync_ReturnsNull_AndLogs_WhenDateIsNotValid()
		{
			const string html = """
				<div class="fund-number">
					<small class="description">Indre værdi pr. 99.99.2026</small>
					<p class="value">123,45</p>
				</div>
				""";
			var scraper = CreateScraper(html, out var logger);

			var result = await scraper.GetJuneNavAsync("https://example.com/june", Ctx, CancellationToken.None);

			Assert.Null(result);
			Assert.Contains(logger.Messages, m => m.Contains("Could not parse date"));
		}
	}
}
