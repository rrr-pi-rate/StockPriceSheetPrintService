using StockPriceSheetPrintService.Outbound.Dto;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System.Text.Json;

namespace StockPriceSheetPrintService.Outbound.ExchangeRates
{
	public class FrankfurterExchangeRateClient(HttpClient client, ILogger<FrankfurterExchangeRateClient> logger) : IHistoricalExchangeRateProvider
	{
		public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetRatesToDkkAsync(string currency, DateOnly from, DateOnly to, ClientContext ctx, CancellationToken ct)
		{
			var url = $"v1/{from:yyyy-MM-dd}..{to:yyyy-MM-dd}?base={Uri.EscapeDataString(currency)}&symbols=DKK";
			try
			{
				var response = await client.GetFromJsonAsync<FrankfurterRateResponse>(url, ct);
				if (response is null)
					return new Dictionary<DateOnly, decimal>();

				var rates = new Dictionary<DateOnly, decimal>();
				foreach (var (dateString, ratesForDate) in response.Rates)
				{
					if (DateOnly.TryParse(dateString, out var date) && ratesForDate.TryGetValue("DKK", out var rate))
						rates[date] = rate;
				}
				return rates;
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
			{
				logger.LogError(ex, "[EXCHANGE-RATE] Error fetching historical rates for {Currency} from {From} to {To} - ClientContext {ctx}", currency, from, to, ctx);
				return new Dictionary<DateOnly, decimal>();
			}
		}
	}
}
