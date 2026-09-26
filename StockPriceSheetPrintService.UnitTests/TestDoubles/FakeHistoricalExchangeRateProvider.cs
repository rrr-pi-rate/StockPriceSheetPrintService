using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeHistoricalExchangeRateProvider : IHistoricalExchangeRateProvider
	{
		public Dictionary<(string Currency, DateOnly Date), decimal?> RatesToReturn { get; } = [];
		public List<(string Currency, DateOnly Date)> Calls { get; } = [];

		public Task<decimal?> GetRateToDkkAsync(string currency, DateOnly date, ClientContext ctx, CancellationToken ct)
		{
			Calls.Add((currency, date));
			return Task.FromResult(RatesToReturn.GetValueOrDefault((currency, date)));
		}
	}
}
