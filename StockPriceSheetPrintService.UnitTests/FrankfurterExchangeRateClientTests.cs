using StockPriceSheetPrintService.Outbound.ExchangeRates;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class FrankfurterExchangeRateClientTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private const string FrankfurterRangeJson = """
			{"amount":1,"base":"USD","start_date":"2026-01-02","end_date":"2026-01-05","rates":{"2026-01-02":{"DKK":6.83},"2026-01-05":{"DKK":6.90}}}
			""";

		private const string FrankfurterRangeJsonWithoutDkk = """
			{"amount":1,"base":"USD","start_date":"2026-01-02","end_date":"2026-01-02","rates":{"2026-01-02":{}}}
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
		public async Task GetRatesToDkkAsync_ReturnsAllRates_FromRangeResponse()
		{
			var client = CreateClient(HttpStatusCode.OK, FrankfurterRangeJson, out _);

			var rates = await client.GetRatesToDkkAsync("USD", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), Ctx, CancellationToken.None);

			Assert.Equal(2, rates.Count);
			Assert.Equal(6.83m, rates[new DateOnly(2026, 1, 2)]);
			Assert.Equal(6.90m, rates[new DateOnly(2026, 1, 5)]);
		}

		[Fact]
		public async Task GetRatesToDkkAsync_OmitsDates_WhereDkkRateIsMissingFromResponse()
		{
			var client = CreateClient(HttpStatusCode.OK, FrankfurterRangeJsonWithoutDkk, out _);

			var rates = await client.GetRatesToDkkAsync("USD", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 2), Ctx, CancellationToken.None);

			Assert.Empty(rates);
		}

		[Fact]
		public async Task GetRatesToDkkAsync_RequestsTheV1RangeEndpoint()
		{
			var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, FrankfurterRangeJson);
			var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.dev/") };
			var client = new FrankfurterExchangeRateClient(httpClient, new TestLogger<FrankfurterExchangeRateClient>());

			await client.GetRatesToDkkAsync("USD", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), Ctx, CancellationToken.None);

			var request = Assert.Single(handler.Requests);
			Assert.Equal("https://api.frankfurter.dev/v1/2026-01-02..2026-01-05?base=USD&symbols=DKK", request.Url);
		}

		[Fact]
		public async Task GetRatesToDkkAsync_ReturnsEmpty_AndLogsError_WhenHttpRequestFails()
		{
			var client = CreateClient(HttpStatusCode.InternalServerError, "boom", out var logger);

			var rates = await client.GetRatesToDkkAsync("USD", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), Ctx, CancellationToken.None);

			Assert.Empty(rates);
			Assert.Contains(logger.Messages, m => m.Contains("USD"));
		}
	}
}
