using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Outbound.MarketStack;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class MarketStackServiceTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private const string ValidEodJson = """
			{"data":[{"symbol":"TEST","exchange":"XETR","date":"2026-01-01T00:00:00+00:00","close":123.45,"price_currency":"EUR"}]}
			""";

		private static MarketStackService CreateService(SequencedHttpClientFactory httpClientFactory)
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["StockApi:AccessKey"] = "primary-key",
					["StockApi:AccessKey2"] = "fallback-key",
				})
				.Build();

			return new MarketStackService(httpClientFactory, configuration, new TestLogger<MarketStackService>());
		}

		[Fact]
		public async Task GetStockPriceAsync_ReturnsPrice_WhenPrimaryKeySucceeds()
		{
			var factory = new SequencedHttpClientFactory(HttpResponses.Json(HttpStatusCode.OK, ValidEodJson));
			var service = CreateService(factory);

			var price = await service.GetStockPriceAsync("TEST", Ctx, CancellationToken.None);

			Assert.NotNull(price);
			Assert.Equal("TEST", price!.Symbol);
			Assert.Equal(123.45m, price.Close);
			Assert.Equal("EUR", price.Currency);
			Assert.Single(factory.RequestUrls);
		}

		[Fact]
		public async Task GetStockPriceAsync_FallsBackToSecondKey_WhenPrimaryKeyFails()
		{
			var factory = new SequencedHttpClientFactory(
				HttpResponses.Json(HttpStatusCode.TooManyRequests, ""),
				HttpResponses.Json(HttpStatusCode.OK, ValidEodJson));
			var service = CreateService(factory);

			var price = await service.GetStockPriceAsync("TEST", Ctx, CancellationToken.None);

			Assert.NotNull(price);
			Assert.Equal(123.45m, price!.Close);
			Assert.Equal(2, factory.RequestUrls.Count);
			Assert.Contains("primary-key", factory.RequestUrls[0]);
			Assert.Contains("fallback-key", factory.RequestUrls[1]);
		}

		[Fact]
		public async Task GetStockPriceAsync_ReturnsNull_WhenBothKeysFail()
		{
			var factory = new SequencedHttpClientFactory(
				HttpResponses.Json(HttpStatusCode.TooManyRequests, ""),
				HttpResponses.Json(HttpStatusCode.TooManyRequests, ""));
			var service = CreateService(factory);

			var price = await service.GetStockPriceAsync("TEST", Ctx, CancellationToken.None);

			Assert.Null(price);
		}

		[Fact]
		public async Task GetStockPriceAsync_ReturnsNull_WhenResponseHasNoData()
		{
			var factory = new SequencedHttpClientFactory(HttpResponses.Json(HttpStatusCode.OK, """{"data":[]}"""));
			var service = CreateService(factory);

			var price = await service.GetStockPriceAsync("TEST", Ctx, CancellationToken.None);

			Assert.Null(price);
		}

		[Fact]
		public async Task GetStockPriceAsync_ReturnsNull_WhenResponseIsNotValidJson()
		{
			var factory = new SequencedHttpClientFactory(HttpResponses.Json(HttpStatusCode.OK, "this is not json"));
			var service = CreateService(factory);

			var price = await service.GetStockPriceAsync("TEST", Ctx, CancellationToken.None);

			Assert.Null(price);
		}
	}
}
