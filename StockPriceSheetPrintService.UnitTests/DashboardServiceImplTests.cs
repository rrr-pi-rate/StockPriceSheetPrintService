using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DashboardServiceImplTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);
		private static readonly DateOnly PortfolioStartDate = new(2025, 9, 15);

		private sealed record Fixture(
			DashboardServiceImpl Service,
			FakeGoogleSheetsClient GoogleSheetsClient,
			FakeYahooFinanceClinet YahooClient,
			FakeBenchmarkStore BenchmarkStore,
			FakeHistoricalExchangeRateProvider ExchangeRateProvider,
			FakeExchangeRateStore ExchangeRateStore,
			TestLogger<DashboardServiceImpl> Logger);

		private static Fixture CreateService(bool withSheetsKey = true)
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(withSheetsKey
					? new Dictionary<string, string?> { ["SheetsApi:SheetsKey"] = "sheet-123" }
					: new Dictionary<string, string?>())
				.Build();

			var googleSheetsClient = new FakeGoogleSheetsClient();
			var yahooClient = new FakeYahooFinanceClinet();
			var benchmarkStore = new FakeBenchmarkStore();
			var exchangeRateProvider = new FakeHistoricalExchangeRateProvider();
			var exchangeRateStore = new FakeExchangeRateStore();
			var logger = new TestLogger<DashboardServiceImpl>();

			var service = new DashboardServiceImpl(
				Options.Create(new BenchmarkOptions { PortfolioStartDate = PortfolioStartDate }),
				googleSheetsClient,
				yahooClient,
				benchmarkStore,
				exchangeRateProvider,
				exchangeRateStore,
				configuration,
				logger);

			return new Fixture(service, googleSheetsClient, yahooClient, benchmarkStore, exchangeRateProvider, exchangeRateStore, logger);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_FetchesFromPortfolioStartDate_WhenNoCacheExists()
		{
			var fixture = CreateService();
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("USD", [new BenchmarkDataPoint(new DateTime(2025, 9, 15), 4000.5)]);

			await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			var call = Assert.Single(fixture.YahooClient.BenchmarkCalls);
			Assert.Equal("^GSPC", call.Symbol);
			Assert.Equal(PortfolioStartDate.ToDateTime(TimeOnly.MinValue), call.From);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_ConvertsToDkk_UsingRateFromProvider_AndCachesIt_InOneBatch()
		{
			var fixture = CreateService();
			var date1 = new DateTime(2026, 1, 2);
			var date2 = new DateTime(2026, 1, 5);
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("USD", [
				new BenchmarkDataPoint(date1, 4000.0),
				new BenchmarkDataPoint(date2, 4100.0),
			]);
			fixture.ExchangeRateProvider.RatesToReturn[("USD", DateOnly.FromDateTime(date1))] = 6.83m;
			fixture.ExchangeRateProvider.RatesToReturn[("USD", DateOnly.FromDateTime(date2))] = 6.90m;

			await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			var inserted = fixture.BenchmarkStore.LastInsertedPoints!;
			Assert.Equal(2, inserted.Count);
			Assert.Equal(4000.0 * 6.83, inserted[0].Value, precision: 5);
			Assert.Equal(4100.0 * 6.90, inserted[1].Value, precision: 5);

			var call = Assert.Single(fixture.ExchangeRateProvider.Calls);
			Assert.Equal("USD", call.Currency);
			Assert.Equal(DateOnly.FromDateTime(date1), call.From);
			Assert.Equal(DateOnly.FromDateTime(date2), call.To);

			var insertedRange = Assert.Single(fixture.ExchangeRateStore.InsertedRanges);
			Assert.Equal(2, insertedRange.Rates.Count);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_UsesCachedRates_WithoutCallingProviderAgain()
		{
			var fixture = CreateService();
			var date = new DateTime(2026, 1, 2);
			var dateOnly = DateOnly.FromDateTime(date);
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("USD", [new BenchmarkDataPoint(date, 4000.0)]);
			fixture.ExchangeRateStore.CachedRates[("USD", dateOnly)] = 7.0m;

			await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			Assert.Empty(fixture.ExchangeRateProvider.Calls);
			var inserted = Assert.Single(fixture.BenchmarkStore.LastInsertedPoints!);
			Assert.Equal(4000.0 * 7.0, inserted.Value, precision: 5);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_SkipsPoint_WithoutCachingAnyRate_AndLogsWarning_WhenNoRateIsFoundAnywhere()
		{
			var fixture = CreateService();
			var date = new DateTime(2026, 1, 2);
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("USD", [new BenchmarkDataPoint(date, 4000.0)]);

			await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			Assert.Empty(fixture.BenchmarkStore.LastInsertedPoints!);
			Assert.Empty(fixture.ExchangeRateStore.InsertedRanges);
			Assert.Contains(fixture.Logger.Messages, m => m.Contains("USD") && m.Contains("^GSPC"));
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_UsesNearestPrecedingRate_WhenExactDateHasNoRate()
		{
			var fixture = CreateService();
			var date1 = new DateTime(2026, 1, 2);
			var date2 = new DateTime(2026, 1, 3); // e.g. a US market day the Danish/European FX market has no rate for
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("USD", [
				new BenchmarkDataPoint(date1, 4000.0),
				new BenchmarkDataPoint(date2, 4010.0),
			]);
			fixture.ExchangeRateStore.CachedRates[("USD", DateOnly.FromDateTime(date1))] = 6.83m;

			await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			var inserted = fixture.BenchmarkStore.LastInsertedPoints!;
			Assert.Equal(2, inserted.Count);
			Assert.Equal(4000.0 * 6.83, inserted[0].Value, precision: 5);
			Assert.Equal(4010.0 * 6.83, inserted[1].Value, precision: 5);
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_LogsExactlyOneWarning_RegardlessOfHowManyDatesAreMissing()
		{
			var fixture = CreateService();
			var points = Enumerable.Range(0, 50)
				.Select(i => new BenchmarkDataPoint(new DateTime(2026, 1, 1).AddDays(i), 4000.0 + i))
				.ToList();
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("USD", points);

			await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			Assert.Single(fixture.Logger.Messages);
			Assert.Contains(fixture.Logger.Messages, m => m.Contains("50 date(s)") && m.Contains("more)"));
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_DoesNotConvert_WhenCurrencyIsDkk()
		{
			var fixture = CreateService();
			var date = new DateTime(2026, 1, 2);
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote("DKK", [new BenchmarkDataPoint(date, 100.0)]);

			await fixture.Service.GetBenchmarkDataAsync("OMXC25", Ctx, CancellationToken.None);

			Assert.Empty(fixture.ExchangeRateProvider.Calls);
			Assert.Empty(fixture.ExchangeRateStore.InsertedRanges);
			var inserted = Assert.Single(fixture.BenchmarkStore.LastInsertedPoints!);
			Assert.Equal(100.0, inserted.Value);
		}

		[Theory]
		[InlineData("GBp")]
		[InlineData("GBX")]
		public async Task GetBenchmarkDataAsync_DividesByHundred_AndUsesGbpRate_ForPenceCurrencies(string penceCurrency)
		{
			var fixture = CreateService();
			var date = new DateTime(2026, 1, 2);
			fixture.BenchmarkStore.LatestDateToReturn = null;
			fixture.YahooClient.BenchmarkQuoteToReturn = new BenchmarkQuote(penceCurrency, [new BenchmarkDataPoint(date, 500.0)]);
			fixture.ExchangeRateProvider.RatesToReturn[("GBP", DateOnly.FromDateTime(date))] = 10m;

			await fixture.Service.GetBenchmarkDataAsync("VOD.L", Ctx, CancellationToken.None);

			var inserted = Assert.Single(fixture.BenchmarkStore.LastInsertedPoints!);
			Assert.Equal(500.0 / 100.0 * 10.0, inserted.Value, precision: 5);
			Assert.Contains(fixture.ExchangeRateProvider.Calls, c => c.Currency == "GBP");
		}

		[Fact]
		public async Task GetBenchmarkDataAsync_SkipsYahooFetch_WhenCachedDataIsAlreadyUpToDate()
		{
			var fixture = CreateService();
			var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
			fixture.BenchmarkStore.LatestDateToReturn = yesterday;
			fixture.BenchmarkStore.DataBySymbol["^GSPC"] = [new BenchmarkDataPoint(yesterday.ToDateTime(TimeOnly.MinValue), 42.0)];

			var result = await fixture.Service.GetBenchmarkDataAsync("^GSPC", Ctx, CancellationToken.None);

			Assert.Empty(fixture.YahooClient.BenchmarkCalls);
			Assert.Single(result);
			Assert.Equal(42.0, result[0].Value);
		}

		[Fact]
		public async Task GetHistoricalDataAsync_ReturnsDataFromGoogleSheets_WhenSheetsKeyConfigured()
		{
			var fixture = CreateService();

			var result = await fixture.Service.GetHistoricalDataAsync(Ctx, CancellationToken.None);

			Assert.NotNull(result);
		}

		[Fact]
		public async Task GetHistoricalDataAsync_Throws_WhenSheetsKeyIsNotConfigured()
		{
			var fixture = CreateService(withSheetsKey: false);

			await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GetHistoricalDataAsync(Ctx, CancellationToken.None));
		}
	}
}
