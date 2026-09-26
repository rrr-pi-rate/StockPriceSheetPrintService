using Google.GenAI;
using StockPriceSheetPrintService.Outbound.Dto;
using StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark;
using StockPriceSheetPrintService.Outbound.Mappers;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System.Text.Json;

namespace StockPriceSheetPrintService.Outbound.YahooFinance
{
	public class YahooFinanceClient(HttpClient client, ILogger<YahooFinanceClient> logger) : IYahooFinanceClient
	{
		public async Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, DateTimeOffset from, DateTimeOffset to, ClientContext ctx, CancellationToken ct)
		{
			var period1 = from.ToUnixTimeSeconds();
			var period2 = to.ToUnixTimeSeconds();
			var encodedSymbol = Uri.EscapeDataString(symbol);

			var url = $"v8/finance/chart/{encodedSymbol}?period1={period1}&period2={period2}&interval=1d";
			YahooChartResponse? response;
			try
			{
				response = await client.GetFromJsonAsync<YahooChartResponse>(url, ct);
			}
			catch (HttpRequestException ex)
			{
				logger.LogError(ex, "Error occurred while fetching benchmark data for symbol {Symbol} - ClientContext {ctx}", symbol, ctx);
				return [];
			}

			var result = response?.Chart.Result?.FirstOrDefault();
			if (response is null || result is null || response.Chart.Error is not null)
				return [];

			var closes = result.Indicators.Quote.FirstOrDefault()?.Close ?? [];

			return [.. result.Timestamp
				.Zip(closes, (ts, close) => (ts, close))
				.Where(x => x.close is not null)
				.Select(x => new BenchmarkDataPoint(
					DateTimeOffset.FromUnixTimeSeconds(x.ts).UtcDateTime.Date,
					x.close!.Value))];
		}

		public async Task<FundNav?> GetFromYahooApiAsync(string ticker, ClientContext ctx, CancellationToken ct)
		{
			var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}";
			var json = await client.GetStringAsync(url, ct);

			using var doc = JsonDocument.Parse(json);
			var result = doc.RootElement
				.GetProperty("chart")
				.GetProperty("result")[0];

			var price = result
				.GetProperty("meta")
				.GetProperty("regularMarketPrice")
				.GetDecimal();

			var timestamp = result
				.GetProperty("meta")
				.GetProperty("regularMarketTime")
				.GetInt64();

			var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;

			var currency = result.GetProperty("meta").TryGetProperty("currency", out var currencyProp)
				? currencyProp.GetString()
				: null;

			return JuneMapper.ToFundNav(new JuneData { Nav = price, Date = date, Currency = currency });
		}
	}
}
