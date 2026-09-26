using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Outbound.Persistence
{
	public class DbExchangeRateStore(IDbContextFactory<StockDbContext> dbFactory) : IExchangeRateStore
	{
		public async Task<Dictionary<DateOnly, decimal>> GetCachedRatesAsync(string currency, DateOnly from, DateOnly to, CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			return await db.ExchangeRates
				.Where(e => e.Currency == currency && e.Date >= from && e.Date <= to)
				.ToDictionaryAsync(e => e.Date, e => e.RateToDkk, ct);
		}

		public async Task InsertRangeAsync(string currency, IReadOnlyDictionary<DateOnly, decimal> rates, CancellationToken ct)
		{
			if (rates.Count == 0)
				return;

			var entities = rates.Select(kv => new ExchangeRateEntity
			{
				Currency = currency,
				Date = kv.Key,
				RateToDkk = kv.Value,
			});

			await using var db = await dbFactory.CreateDbContextAsync(ct);
			db.ExchangeRates.AddRange(entities);

			try
			{
				await db.SaveChangesAsync(ct);
			}
			catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
			{
				// A concurrent request already cached these dates for this currency - safe to ignore, historical rates never change.
			}
		}
	}
}
