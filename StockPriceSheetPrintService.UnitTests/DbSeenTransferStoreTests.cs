using StockPriceSheetPrintService.Outbound.Persistence;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DbSeenTransferStoreTests
	{
		private static readonly DateOnly Day = new(2026, 10, 2);

		[Fact]
		public async Task LoadAsync_ReturnsEmpty_WhenNoTransfersSaved()
		{
			var store = new DbSeenTransferStore(new InMemoryDbContextFactory());

			var ids = await store.LoadAsync(CancellationToken.None);

			Assert.Empty(ids);
		}

		[Fact]
		public async Task SaveAsync_ThenLoadAsync_ReturnsSavedIds()
		{
			var store = new DbSeenTransferStore(new InMemoryDbContextFactory());

			await store.SaveAsync([new Transfer("a", 100m, Day), new Transfer("b", 250m, Day)], CancellationToken.None);

			var ids = await store.LoadAsync(CancellationToken.None);

			Assert.Equal(new HashSet<string> { "a", "b" }, ids);
		}

		[Fact]
		public async Task SaveAsync_PersistsAmountAndDate()
		{
			var factory = new InMemoryDbContextFactory();
			var store = new DbSeenTransferStore(factory);

			await store.SaveAsync([new Transfer("a", 1234.56m, Day)], CancellationToken.None);

			await using var db = await factory.CreateDbContextAsync();
			var entity = Assert.Single(db.SeenTransfers);
			Assert.Equal("a", entity.BookingId);
			Assert.Equal(1234.56m, entity.Amount);
			Assert.Equal(Day, entity.Date);
		}

		[Fact]
		public async Task SaveAsync_DoesNotDuplicateOrOverwriteExistingTransfer()
		{
			var factory = new InMemoryDbContextFactory();
			var store = new DbSeenTransferStore(factory);

			await store.SaveAsync([new Transfer("a", 100m, Day)], CancellationToken.None);
			await store.SaveAsync([new Transfer("a", 999m, Day), new Transfer("b", 50m, Day)], CancellationToken.None);

			await using var db = await factory.CreateDbContextAsync();
			Assert.Equal(2, db.SeenTransfers.Count());
			Assert.Equal(100m, db.SeenTransfers.Single(e => e.BookingId == "a").Amount);
		}

		[Fact]
		public async Task SaveAsync_DoesNotDeleteOldTransfers()
		{
			var factory = new InMemoryDbContextFactory();
			await using (var db = await factory.CreateDbContextAsync())
			{
				db.SeenTransfers.Add(new SeenTransferEntity { BookingId = "old", Amount = 500m, Date = Day.AddDays(-365) });
				await db.SaveChangesAsync();
			}
			var store = new DbSeenTransferStore(factory);

			await store.SaveAsync([new Transfer("new", 100m, Day)], CancellationToken.None);

			var ids = await store.LoadAsync(CancellationToken.None);
			Assert.Equal(new HashSet<string> { "old", "new" }, ids);
		}
	}
}
