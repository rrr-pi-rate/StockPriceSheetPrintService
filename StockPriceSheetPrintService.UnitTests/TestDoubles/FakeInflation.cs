using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeInflationIndexProvider : IInflationIndexProvider
	{
		public IReadOnlyList<BenchmarkDataPoint>? PointsToReturn { get; set; }
		public int CallCount { get; private set; }

		public Task<IReadOnlyList<BenchmarkDataPoint>?> GetConsumerPriceIndexAsync(ClientContext ctx, CancellationToken ct)
		{
			CallCount++;
			return Task.FromResult(PointsToReturn);
		}
	}

	public class FakeInflationStore : IInflationStore
	{
		public List<BenchmarkDataPoint> Data { get; } = [];
		public DateTimeOffset? LastFetchedAt { get; set; }

		public Task<DateTimeOffset?> GetLastFetchedAtAsync(CancellationToken ct) => Task.FromResult(LastFetchedAt);

		public Task UpsertAsync(IReadOnlyList<BenchmarkDataPoint> points, CancellationToken ct)
		{
			Data.Clear();
			Data.AddRange(points);
			LastFetchedAt = DateTimeOffset.UtcNow;
			return Task.CompletedTask;
		}

		public Task<IReadOnlyList<BenchmarkDataPoint>> GetCachedDataAsync(CancellationToken ct) =>
			Task.FromResult<IReadOnlyList<BenchmarkDataPoint>>([.. Data]);
	}
}
