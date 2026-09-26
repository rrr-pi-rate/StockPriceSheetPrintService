using Microsoft.Extensions.Options;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Inbound;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Service.Application
{
	public class DashboardServiceImpl(
		IOptions<BenchmarkOptions> benchmarkOptions,
		IGoogleSheetsClient googleSheetsClient,
		IYahooFinanceClient yahooClient,
		IBenchmarkStore repository,
		IHistoricalExchangeRateProvider exchangeRateProvider,
		IExchangeRateStore exchangeRateStore,
		IConfiguration configuration) : IDashboardService
	{
		private static readonly Dictionary<string, (string RateCurrency, decimal Divisor)> CurrencyAliases = new()
		{
			["GBp"] = ("GBP", 100m),
			["GBX"] = ("GBP", 100m),
		};

		private readonly DateOnly from = benchmarkOptions.Value.PortfolioStartDate;
		public async Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, ClientContext ctx, CancellationToken ct)
		{
			var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
			var latestCachedDate = await repository.GetLatestDateAsync(symbol, ct);

			if (latestCachedDate is null || latestCachedDate < yesterday)
			{
				var fetchFrom = latestCachedDate?.AddDays(1) ?? from;
				var quote = await yahooClient.GetBenchmarkDataAsync(
					symbol, fetchFrom.ToDateTime(TimeOnly.MinValue), yesterday.ToDateTime(TimeOnly.MinValue), ctx, ct);

				var dkkPoints = await ConvertToDkkAsync(quote, ctx, ct);
				await repository.InsertAsync(symbol, dkkPoints, ct);
			}

			return await repository.GetCachedDataAsync(symbol, ct);
		}

		private async Task<IReadOnlyList<BenchmarkDataPoint>> ConvertToDkkAsync(BenchmarkQuote quote, ClientContext ctx, CancellationToken ct)
		{
			if (quote.Points.Count == 0 || string.IsNullOrEmpty(quote.Currency) || quote.Currency == "DKK")
				return quote.Points;

			var (rateCurrency, divisor) = CurrencyAliases.TryGetValue(quote.Currency, out var alias) ? alias : (quote.Currency, 1m);

			var converted = new List<BenchmarkDataPoint>(quote.Points.Count);
			foreach (var point in quote.Points)
			{
				var date = DateOnly.FromDateTime(point.Date);
				var rate = await GetRateToDkkAsync(rateCurrency, date, ctx, ct);
				converted.Add(new BenchmarkDataPoint(point.Date, point.Value / (double)divisor * (double)rate));
			}

			return converted;
		}

		private async Task<decimal> GetRateToDkkAsync(string currency, DateOnly date, ClientContext ctx, CancellationToken ct)
		{
			var cachedRate = await exchangeRateStore.GetCachedRateAsync(currency, date, ct);
			if (cachedRate is not null)
				return cachedRate.Value;

			var rate = await exchangeRateProvider.GetRateToDkkAsync(currency, date, ctx, ct) ?? 1m;
			await exchangeRateStore.InsertAsync(currency, date, rate, ct);
			return rate;
		}

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(ClientContext ctx, CancellationToken ct)
		{
			var spreadsheetId = configuration["SheetsApi:SheetsKey"]
				?? throw new InvalidOperationException("SheetsApi:SheetsKey is not configured");
			return googleSheetsClient.GetHistoricalDataAsync(spreadsheetId, "Daily", ctx, ct);
		}
	}
}
