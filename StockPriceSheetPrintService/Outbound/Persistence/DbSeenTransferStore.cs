using Microsoft.EntityFrameworkCore;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Outbound.Persistence
{
	public class DbSeenTransferStore(IDbContextFactory<StockDbContext> dbFactory) : ISeenTransferStore
	{
		public async Task<HashSet<string>> LoadAsync(CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);
			return await db.SeenTransfers
				.Select(e => e.BookingId)
				.ToHashSetAsync(ct);
		}

		public async Task SaveAsync(IEnumerable<Transfer> newTransfers, CancellationToken ct)
		{
			await using var db = await dbFactory.CreateDbContextAsync(ct);

			var existing = await db.SeenTransfers.Select(e => e.BookingId).ToHashSetAsync(ct);
			foreach (var t in newTransfers.Where(t => !existing.Contains(t.Id)))
				db.SeenTransfers.Add(new SeenTransferEntity { BookingId = t.Id, Amount = t.Amount, Date = t.Date });

			await db.SaveChangesAsync(ct);
		}
	}
}
