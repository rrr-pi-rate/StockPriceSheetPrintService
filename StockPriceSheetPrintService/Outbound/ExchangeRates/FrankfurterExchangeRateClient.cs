using StockPriceSheetPrintService.Outbound.Dto;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System.Text.Json;

namespace StockPriceSheetPrintService.Outbound.ExchangeRates
{
	public class FrankfurterExchangeRateClient(HttpClient client, ILogger<FrankfurterExchangeRateClient> logger) : IHistoricalExchangeRateProvider
	{
		public async Task<decimal?> GetRateToDkkAsync(string currency, DateOnly date, ClientContext ctx, CancellationToken ct)
		{
			if (currency == "DKK")
				return 1m;

			var url = $"v1/{date:yyyy-MM-dd}?base={Uri.EscapeDataString(currency)}&symbols=DKK";
			try
			{
				var response = await client.GetFromJsonAsync<FrankfurterRateResponse>(url, ct);
				if (response is not null && response.Rates.TryGetValue("DKK", out var rate))
					return rate;

				logger.LogWarning("[EXCHANGE-RATE] No DKK rate returned for {Currency} on {Date} - ClientContext {ctx}", currency, date, ctx);
				return null;
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
			{
				logger.LogError(ex, "[EXCHANGE-RATE] Error fetching historical rate for {Currency} on {Date} - ClientContext {ctx}", currency, date, ctx);
				return null;
			}
		}
	}
}
