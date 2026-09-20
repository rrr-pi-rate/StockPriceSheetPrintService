using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeMarketStackService : IMarketStackService
	{
		public Dictionary<string, StockPrice?> PricesBySymbol { get; } = [];
		public Dictionary<string, int> CallCountBySymbol { get; } = [];

		public Task<StockPrice?> GetStockPriceAsync(string symbol, ClientContext ctx, CancellationToken ct)
		{
			CallCountBySymbol[symbol] = CallCountBySymbol.GetValueOrDefault(symbol) + 1;
			return Task.FromResult(PricesBySymbol.GetValueOrDefault(symbol));
		}
	}
}
