using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeHtmlScraper : IHtmlScraper
	{
		public Dictionary<string, FundNav?> YahooResultsBySymbol { get; } = [];
		public HashSet<string> SymbolsThatThrow { get; } = [];
		public Dictionary<string, int> YahooCallCountBySymbol { get; } = [];

		public Task<FundNav?> GetJuneNavAsync(string url, ClientContext ctx, CancellationToken token) =>
			throw new NotSupportedException("Not used by these tests");

		public Task<FundNav?> GetFromYahooApiAsync(string ticker, ClientContext ctx, CancellationToken token)
		{
			YahooCallCountBySymbol[ticker] = YahooCallCountBySymbol.GetValueOrDefault(ticker) + 1;

			if (SymbolsThatThrow.Contains(ticker))
				throw new HttpRequestException($"Simulated Yahoo failure for {ticker}");

			return Task.FromResult(YahooResultsBySymbol.GetValueOrDefault(ticker));
		}
	}
}
