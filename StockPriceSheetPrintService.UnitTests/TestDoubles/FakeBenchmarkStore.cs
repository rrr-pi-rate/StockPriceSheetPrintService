using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeBenchmarkStore : IBenchmarkStore
	{
		public Dictionary<string, List<BenchmarkDataPoint>> DataBySymbol { get; } = [];
		public DateOnly? LatestDateToReturn { get; set; }
		public string? LastInsertedSymbol { get; private set; }
		public IReadOnlyList<BenchmarkDataPoint>? LastInsertedPoints { get; private set; }

		public Task<DateOnly?> GetLatestDateAsync(string symbol, CancellationToken ct) =>
			Task.FromResult(LatestDateToReturn);

		public Task InsertAsync(string symbol, IReadOnlyList<BenchmarkDataPoint> points, CancellationToken ct)
		{
			LastInsertedSymbol = symbol;
			LastInsertedPoints = points;
			DataBySymbol[symbol] = [.. points];
			return Task.CompletedTask;
		}

		public Task<IReadOnlyList<BenchmarkDataPoint>> GetCachedDataAsync(string symbol, CancellationToken ct) =>
			Task.FromResult<IReadOnlyList<BenchmarkDataPoint>>(DataBySymbol.GetValueOrDefault(symbol, []));
	}
}
