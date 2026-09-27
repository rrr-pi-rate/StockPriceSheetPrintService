using Microsoft.EntityFrameworkCore;
using Npgsql;
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
			if (points.Count == 0) return;

			await using var db = await dbFactory.CreateDbContextAsync(ct);

			// SaveChangesAsync batches all rows into one transaction, so a single already-cached
			// date (e.g. Yahoo re-returning a boundary day we already have) would roll back the
			// whole batch - including genuinely new rows - and get silently swallowed by the
			// catch below, leaving the cache permanently stuck one day behind. Filter out dates
			// we already have first so only genuinely new rows are ever inserted.
			var dates = points.Select(p => DateOnly.FromDateTime(p.Date)).ToList();
			var existingDates = await db.BenchmarkData
				.Where(e => e.Symbol == symbol && dates.Contains(e.Date))
				.Select(e => e.Date)
				.ToHashSetAsync(ct);

			var entities = points
				.Where(p => !existingDates.Contains(DateOnly.FromDateTime(p.Date)))
				.Select(p => new BenchmarkDataEntity
				{
					Symbol = symbol,
					Date = DateOnly.FromDateTime(p.Date),
					CloseValue = p.Value,
					FetchedAt = DateTimeOffset.UtcNow,
				})
				.ToList();

			if (entities.Count == 0) return;

			db.BenchmarkData.AddRange(entities);
			try
			{
				await db.SaveChangesAsync(ct);
			}
			catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
			{
				// A concurrent request inserted the same dates between our existence check and
				// this save - safe to ignore.
			}
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
