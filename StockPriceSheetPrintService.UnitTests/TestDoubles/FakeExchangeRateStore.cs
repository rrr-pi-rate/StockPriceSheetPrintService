using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeExchangeRateStore : IExchangeRateStore
	{
		public Dictionary<(string Currency, DateOnly Date), decimal> CachedRates { get; } = [];
		public List<(string Currency, IReadOnlyDictionary<DateOnly, decimal> Rates)> InsertedRanges { get; } = [];

		public Task<Dictionary<DateOnly, decimal>> GetCachedRatesAsync(string currency, DateOnly from, DateOnly to, CancellationToken ct)
		{
			var result = CachedRates
				.Where(kv => kv.Key.Currency == currency && kv.Key.Date >= from && kv.Key.Date <= to)
				.ToDictionary(kv => kv.Key.Date, kv => kv.Value);

			return Task.FromResult(result);
		}

		public Task InsertRangeAsync(string currency, IReadOnlyDictionary<DateOnly, decimal> rates, CancellationToken ct)
		{
			InsertedRanges.Add((currency, rates));
			foreach (var (date, rate) in rates)
				CachedRates[(currency, date)] = rate;

			return Task.CompletedTask;
		}
	}
}
