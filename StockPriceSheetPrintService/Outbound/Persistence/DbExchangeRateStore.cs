using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Outbound.Persistence
{
	public class DbExchangeRateStore(IDbContextFactory<StockDbContext> dbFactory) : IExchangeRateStore
	{
		public async Task<decimal?> GetCachedRateAsync(string currency, DateOnly date, CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			return await db.ExchangeRates
				.Where(e => e.Currency == currency && e.Date == date)
				.Select(e => (decimal?)e.RateToDkk)
				.FirstOrDefaultAsync(ct);
		}

		public async Task InsertAsync(string currency, DateOnly date, decimal rateToDkk, CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);
			db.ExchangeRates.Add(new ExchangeRateEntity { Currency = currency, Date = date, RateToDkk = rateToDkk });

			try
			{
				await db.SaveChangesAsync(ct);
			}
			catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
			{
				// A concurrent request already cached this rate - safe to ignore, historical rates never change.
			}
		}
	}
}
