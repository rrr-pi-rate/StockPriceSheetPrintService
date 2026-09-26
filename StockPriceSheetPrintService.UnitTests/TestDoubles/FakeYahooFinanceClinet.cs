using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	internal class FakeYahooFinanceClinet : IYahooFinanceClient
	{
		public Dictionary<string, FundNav?> YahooResultsBySymbol { get; } = [];
		public HashSet<string> SymbolsThatThrow { get; } = [];
		public Dictionary<string, int> YahooCallCountBySymbol { get; } = [];

		public Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, DateTimeOffset from, DateTimeOffset to, ClientContext ctx, CancellationToken ct)
		{
			throw new NotImplementedException();
		}

		public Task<FundNav?> GetFromYahooApiAsync(string ticker, ClientContext ctx, CancellationToken token)
		{
			YahooCallCountBySymbol[ticker] = YahooCallCountBySymbol.GetValueOrDefault(ticker) + 1;

			if (SymbolsThatThrow.Contains(ticker))
				throw new HttpRequestException($"Simulated Yahoo failure for {ticker}");

			return Task.FromResult(YahooResultsBySymbol.GetValueOrDefault(ticker));
		}
	}
}
