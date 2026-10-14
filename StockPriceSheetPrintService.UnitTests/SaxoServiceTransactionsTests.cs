using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Outbound.Saxo;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SaxoServiceTransactionsTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private sealed class NoopNetPositionStore : ISaxoNetPositionStore
		{
			public Task<List<Instrument>> GetNetPositionsAsync() => Task.FromResult(new List<Instrument>());
			public Task UpsertPositionsAsync(List<Instrument> instruments) => Task.CompletedTask;
			public Task RemoveStalePositionsAsync(List<int> validUics) => Task.CompletedTask;
		}

		private static (SaxoService Service, SequencedHttpClientFactory Http, TestLogger<SaxoService> Logger) CreateService(params HttpResponseMessage[] responses)
		{
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Saxo:TokenEndpoint"] = "https://example.com/token",
					["Saxo:RedirectUrl"] = "https://example.com/callback",
					["Saxo:AppKey"] = "app-key",
					["Saxo:AppSecret"] = "app-secret",
					["Saxo:ApiBaseUrl"] = "https://gateway.example.com/openapi",
					["Saxo:AuthEndpoint"] = "https://example.com/authorize",
				})
				.Build();
			var http = new SequencedHttpClientFactory(responses);
			var logger = new TestLogger<SaxoService>();
			return (new SaxoService(http, config, new NoopNetPositionStore(), logger), http, logger);
		}

		private const string ClientKeyResponse = """{ "ClientKey": "client-key-123" }""";

		[Fact]
		public async Task GetSaxoTransactionsAsync_CallsTransactionsEndpointWithClientKeyDatesAndTransactionType()
		{
			var (service, http, _) = CreateService(
				HttpResponses.Json(HttpStatusCode.OK, ClientKeyResponse),
				HttpResponses.Json(HttpStatusCode.OK, """{ "Data": [] }"""));

			await service.GetSaxoTransactionsAsync("token", new DateTime(2026, 10, 1), new DateTime(2026, 10, 15), Ctx, CancellationToken.None);

			Assert.Equal("https://gateway.example.com/openapi/port/v1/clients/me", http.RequestUrls[0]);
			Assert.Equal(
				"https://gateway.example.com/openapi/hist/v1/transactions?ClientKey=client-key-123&FromDate=2026-10-01&ToDate=2026-10-15&TransactionType=CashTransfer",
				http.RequestUrls[1]);
		}

		[Fact]
		public async Task GetSaxoTransactionsAsync_MapsTransfers_SkipsInternalOnes_AndLogsIntradayTransactions()
		{
			var (service, _, logger) = CreateService(
				HttpResponses.Json(HttpStatusCode.OK, ClientKeyResponse),
				HttpResponses.Json(HttpStatusCode.OK, """
					{
						"Data": [
							{ "BookingId": "1", "BookedAmount": 5000.0, "IsIntradayData": false, "FundingSubType": "DepositFromExternal", "Date": "2026-10-02" },
							{ "BookingId": "2", "IntradayAmount": 700.0, "IsIntradayData": true, "FundingSubType": "DepositFromExternal", "Date": "2026-10-14" },
							{ "BookingId": "3", "BookedAmount": 100.0, "IsIntradayData": false, "FundingSubType": "IntraClientTransfer", "Date": "2026-10-02" }
						]
					}
					"""));

			var transfers = await service.GetSaxoTransactionsAsync("token", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow, Ctx, CancellationToken.None);

			Assert.Equal(["1", "2"], transfers.Select(t => t.Id));
			Assert.Equal(700m, transfers[1].Amount);
			var logged = Assert.Single(logger.Messages, m => m.Contains("Intraday transaction"));
			Assert.Contains("BookingId=2", logged);
			Assert.Contains("FundingSubType=DepositFromExternal", logged);
		}

		[Fact]
		public async Task GetSaxoTransactionsAsync_Throws_WhenSaxoReturnsAnError()
		{
			var (service, _, _) = CreateService(
				HttpResponses.Json(HttpStatusCode.OK, ClientKeyResponse),
				HttpResponses.Json(HttpStatusCode.InternalServerError, "boom"));

			await Assert.ThrowsAsync<HttpRequestException>(() =>
				service.GetSaxoTransactionsAsync("token", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow, Ctx, CancellationToken.None));
		}
	}
}
