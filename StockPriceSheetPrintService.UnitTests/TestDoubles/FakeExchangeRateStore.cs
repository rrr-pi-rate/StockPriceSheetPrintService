using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeExchangeRateStore : IExchangeRateStore
	{
		public Dictionary<(string Currency, DateOnly Date), decimal> CachedRates { get; } = [];
		public List<(string Currency, DateOnly Date, decimal Rate)> InsertedRates { get; } = [];

		public Task<decimal?> GetCachedRateAsync(string currency, DateOnly date, CancellationToken ct) =>
			Task.FromResult(CachedRates.TryGetValue((currency, date), out var rate) ? rate : (decimal?)null);

		public Task InsertAsync(string currency, DateOnly date, decimal rateToDkk, CancellationToken ct)
		{
			InsertedRates.Add((currency, date, rateToDkk));
			CachedRates[(currency, date)] = rateToDkk;
			return Task.CompletedTask;
		}
	}
}
