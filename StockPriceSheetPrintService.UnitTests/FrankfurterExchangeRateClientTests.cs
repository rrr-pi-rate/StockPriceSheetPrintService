using StockPriceSheetPrintService.Outbound.ExchangeRates;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class FrankfurterExchangeRateClientTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private const string FrankfurterJson = """
			{"amount":1,"base":"USD","date":"2026-01-02","rates":{"DKK":6.83}}
			""";

		private const string FrankfurterJsonWithoutDkk = """
			{"amount":1,"base":"USD","date":"2026-01-02","rates":{}}
			""";

		private static FrankfurterExchangeRateClient CreateClient(HttpStatusCode statusCode, string body, out TestLogger<FrankfurterExchangeRateClient> logger)
		{
			logger = new TestLogger<FrankfurterExchangeRateClient>();
			var client = new HttpClient(new FakeHttpMessageHandler(statusCode, body))
			{
				BaseAddress = new Uri("https://api.frankfurter.dev/")
			};
			return new FrankfurterExchangeRateClient(client, logger);
		}

		[Fact]
		public async Task GetRateToDkkAsync_ReturnsRate_FromResponse()
		{
			var client = CreateClient(HttpStatusCode.OK, FrankfurterJson, out _);

			var rate = await client.GetRateToDkkAsync("USD", new DateOnly(2026, 1, 2), Ctx, CancellationToken.None);

			Assert.Equal(6.83m, rate);
		}

		[Fact]
		public async Task GetRateToDkkAsync_ReturnsOne_WithoutCallingApi_WhenCurrencyIsDkk()
		{
			var client = CreateClient(HttpStatusCode.InternalServerError, "should not be called", out _);

			var rate = await client.GetRateToDkkAsync("DKK", new DateOnly(2026, 1, 2), Ctx, CancellationToken.None);

			Assert.Equal(1m, rate);
		}

		[Fact]
		public async Task GetRateToDkkAsync_ReturnsNull_AndLogsWarning_WhenDkkRateMissingFromResponse()
		{
			var client = CreateClient(HttpStatusCode.OK, FrankfurterJsonWithoutDkk, out var logger);

			var rate = await client.GetRateToDkkAsync("USD", new DateOnly(2026, 1, 2), Ctx, CancellationToken.None);

			Assert.Null(rate);
			Assert.Contains(logger.Messages, m => m.Contains("USD"));
		}

		[Fact]
		public async Task GetRateToDkkAsync_ReturnsNull_AndLogsError_WhenHttpRequestFails()
		{
			var client = CreateClient(HttpStatusCode.InternalServerError, "boom", out var logger);

			var rate = await client.GetRateToDkkAsync("USD", new DateOnly(2026, 1, 2), Ctx, CancellationToken.None);

			Assert.Null(rate);
			Assert.Contains(logger.Messages, m => m.Contains("USD"));
		}
	}
}
