using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Outbound.Persistence
{
	public class DbInflationStore(IDbContextFactory<StockDbContext> dbFactory) : IInflationStore
	{
		public async Task<DateTimeOffset?> GetLastFetchedAtAsync(CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			return await db.InflationIndex.MaxAsync(e => (DateTimeOffset?)e.FetchedAt, ct);
		}

		public async Task UpsertAsync(IReadOnlyList<BenchmarkDataPoint> points, CancellationToken ct)
		{
			if (points.Count == 0) return;

			await using var db = await dbFactory.CreateDbContextAsync(ct);

			var now = DateTimeOffset.UtcNow;
			var existing = await db.InflationIndex.ToDictionaryAsync(e => e.Month, ct);

			foreach (var point in points)
			{
				var month = DateOnly.FromDateTime(point.Date);
				if (existing.TryGetValue(month, out var entity))
				{
					// Danmarks Statistik kan revidere en tidligere udgivet måned.
					entity.IndexValue = point.Value;
					entity.FetchedAt = now;
				}
				else
				{
					db.InflationIndex.Add(new InflationIndexEntity { Month = month, IndexValue = point.Value, FetchedAt = now });
				}
			}

			try
			{
				await db.SaveChangesAsync(ct);
			}
			catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
			{
				// A concurrent request already inserted the same months - safe to ignore.
			}
		}

		public async Task<IReadOnlyList<BenchmarkDataPoint>> GetCachedDataAsync(CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			return await db.InflationIndex
				.OrderBy(e => e.Month)
				.Select(e => new BenchmarkDataPoint(e.Month.ToDateTime(TimeOnly.MinValue), e.IndexValue))
				.ToListAsync(ct);
		}
	}
}
