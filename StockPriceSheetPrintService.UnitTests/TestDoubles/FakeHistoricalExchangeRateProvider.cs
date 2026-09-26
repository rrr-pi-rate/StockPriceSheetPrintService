using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeHistoricalExchangeRateProvider : IHistoricalExchangeRateProvider
	{
		public Dictionary<(string Currency, DateOnly Date), decimal> RatesToReturn { get; } = [];
		public List<(string Currency, DateOnly From, DateOnly To)> Calls { get; } = [];

		public Task<IReadOnlyDictionary<DateOnly, decimal>> GetRatesToDkkAsync(string currency, DateOnly from, DateOnly to, ClientContext ctx, CancellationToken ct)
		{
			Calls.Add((currency, from, to));

			var result = RatesToReturn
				.Where(kv => kv.Key.Currency == currency && kv.Key.Date >= from && kv.Key.Date <= to)
				.ToDictionary(kv => kv.Key.Date, kv => kv.Value);

			return Task.FromResult<IReadOnlyDictionary<DateOnly, decimal>>(result);
		}
	}
}
