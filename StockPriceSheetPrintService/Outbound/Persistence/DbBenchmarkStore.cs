using Microsoft.EntityFrameworkCore;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Outbound.Persistence
{
	public class DbBenchmarkStore(IDbContextFactory<StockDbContext> dbFactory) : IBenchmarkStore
	{
		public async Task<DateOnly?> GetLatestDateAsync(string symbol, CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			return await db.BenchmarkData
				.Where(e => e.Symbol == symbol)
				.MaxAsync(e => (DateOnly?)e.Date, ct);
		}

		public async Task InsertAsync(string symbol, IReadOnlyList<BenchmarkDataPoint> points, CancellationToken ct)
		{
			var entities = points.Select(p => new BenchmarkDataEntity
			{
				Symbol = symbol,
				Date = DateOnly.FromDateTime(p.Date),
				CloseValue = p.Value,
				FetchedAt = DateTimeOffset.UtcNow,
			});

			await using var db = await dbFactory.CreateDbContextAsync(ct);
			db.BenchmarkData.AddRange(entities);
			await db.SaveChangesAsync(ct);
		}

		public async Task<IReadOnlyList<BenchmarkDataPoint>> GetCachedDataAsync(string symbol, CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			return await db.BenchmarkData
				.Where(e => e.Symbol == symbol)
				.OrderBy(e => e.Date)
				.Select(e => new BenchmarkDataPoint(e.Date.ToDateTime(TimeOnly.MinValue), e.CloseValue))
				.ToListAsync(ct);
		}
	}
}
