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
		IConfiguration configuration,
		ILogger<DashboardServiceImpl> logger) : IDashboardService
	{
		private static readonly Dictionary<string, (string RateCurrency, decimal Divisor)> CurrencyAliases = new()
		{
			["GBp"] = ("GBP", 100m),
			["GBX"] = ("GBP", 100m),
		};

		private readonly DateOnly from = benchmarkOptions.Value.PortfolioStartDate;
		public async Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(
			string symbol, ClientContext ctx, CancellationToken ct)
		{
			var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
			var latestCachedDate = await repository.GetLatestDateAsync(symbol, ct);

			if (latestCachedDate is null || latestCachedDate < yesterday)
			{
				var fetchFrom = latestCachedDate?.AddDays(1) ?? from;
				var quote = await yahooClient.GetBenchmarkDataAsync(
					symbol, fetchFrom.ToDateTime(TimeOnly.MinValue), yesterday.ToDateTime(TimeOnly.MinValue), ctx, ct);

				var dkkPoints = await ConvertToDkkAsync(symbol, quote, ctx, ct);
				await repository.InsertAsync(symbol, dkkPoints, ct);
			}

			return await repository.GetCachedDataAsync(symbol, ct);
		}

		private async Task<IReadOnlyList<BenchmarkDataPoint>> ConvertToDkkAsync(
			string symbol, BenchmarkQuote quote, ClientContext ctx, CancellationToken ct)
		{
			if (quote.Points.Count == 0 || string.IsNullOrEmpty(quote.Currency) || quote.Currency == "DKK")
				return quote.Points;

			var (rateCurrency, divisor) = CurrencyAliases.TryGetValue(quote.Currency, out var alias) ? alias : (quote.Currency, 1m);

			var rates = await GetRatesToDkkAsync(rateCurrency, quote.Points, ctx, ct);

			var sanitizedSymbol = (symbol ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);

			var missingDates = new List<DateOnly>();
			var converted = new List<BenchmarkDataPoint>(quote.Points.Count);
			foreach (var point in quote.Points)
			{
				var date = DateOnly.FromDateTime(point.Date);
				if (!rates.TryGetValue(date, out var rate))
				{
					rate = 1m;
					missingDates.Add(date);
				}

				converted.Add(new BenchmarkDataPoint(point.Date, point.Value / (double)divisor * (double)rate));
			}

			if (missingDates.Count > 0)
			{
				const int maxDatesInLogMessage = 10;
				var datesText = missingDates.Count <= maxDatesInLogMessage
					? string.Join(", ", missingDates)
					: string.Join(", ", missingDates.Take(maxDatesInLogMessage)) + $" (+{missingDates.Count - maxDatesInLogMessage} more)";

				logger.LogWarning(
					"[EXCHANGE-RATE] No DKK rate found for {Currency} on {MissingCount} date(s) for symbol {Symbol}" +
					" - falling back to 1:1 - Dates: {Dates} - ClientContext {ctx}",
					rateCurrency, missingDates.Count, sanitizedSymbol, datesText, ctx);
			}

			return converted;
		}

		private async Task<Dictionary<DateOnly, decimal>> GetRatesToDkkAsync(
			string currency, IReadOnlyList<BenchmarkDataPoint> points, ClientContext ctx, CancellationToken ct)
		{
			var dates = points.Select(p => DateOnly.FromDateTime(p.Date)).Distinct().ToList();
			var rangeFrom = dates.Min();
			var rangeTo = dates.Max();

			var rates = await exchangeRateStore.GetCachedRatesAsync(currency, rangeFrom, rangeTo, ct);
			if (dates.All(rates.ContainsKey))
				return rates;

			var fetched = await exchangeRateProvider.GetRatesToDkkAsync(currency, rangeFrom, rangeTo, ctx, ct);
			var newRates = fetched.Where(kv => !rates.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

			if (newRates.Count > 0)
				await exchangeRateStore.InsertRangeAsync(currency, newRates, ct);

			foreach (var (date, rate) in newRates)
				rates[date] = rate;

			return rates;
		}

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(ClientContext ctx, CancellationToken ct)
		{
			var spreadsheetId = configuration["SheetsApi:SheetsKey"]
				?? throw new InvalidOperationException("SheetsApi:SheetsKey is not configured");
			return googleSheetsClient.GetHistoricalDataAsync(spreadsheetId, "Daily", ctx, ct);
		}
	}
}
