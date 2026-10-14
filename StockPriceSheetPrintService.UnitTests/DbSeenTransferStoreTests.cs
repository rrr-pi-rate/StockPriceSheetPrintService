using StockPriceSheetPrintService.Outbound.Persistence;
using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DbSeenTransferStoreTests
	{
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

			await store.SaveAsync([new Transfer("a", 100m), new Transfer("b", 250m)], CancellationToken.None);

			var ids = await store.LoadAsync(CancellationToken.None);

			Assert.Equal(new HashSet<string> { "a", "b" }, ids);
		}

		[Fact]
		public async Task SaveAsync_PersistsAmount()
		{
			var factory = new InMemoryDbContextFactory();
			var store = new DbSeenTransferStore(factory);

			await store.SaveAsync([new Transfer("a", 1234.56m)], CancellationToken.None);

			await using var db = await factory.CreateDbContextAsync();
			var entity = Assert.Single(db.SeenTransfers);
			Assert.Equal("a", entity.BookingId);
			Assert.Equal(1234.56m, entity.Amount);
		}

		[Fact]
		public async Task SaveAsync_DoesNotDuplicateOrOverwriteExistingTransfer()
		{
			var factory = new InMemoryDbContextFactory();
			var store = new DbSeenTransferStore(factory);

			await store.SaveAsync([new Transfer("a", 100m)], CancellationToken.None);
			await store.SaveAsync([new Transfer("a", 999m), new Transfer("b", 50m)], CancellationToken.None);

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
				db.SeenTransfers.Add(new SeenTransferEntity { BookingId = "old", Amount = 500m, SeenAt = DateTime.UtcNow.AddDays(-365) });
				await db.SaveChangesAsync();
			}
			var store = new DbSeenTransferStore(factory);

			await store.SaveAsync([new Transfer("new", 100m)], CancellationToken.None);

			var ids = await store.LoadAsync(CancellationToken.None);
			Assert.Equal(new HashSet<string> { "old", "new" }, ids);
		}
	}
}
