using Microsoft.EntityFrameworkCore;
using StockPriceSheetPrintService.Outbound.Persistence;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class InMemoryDbContextFactory : IDbContextFactory<StockDbContext>
	{
		private readonly DbContextOptions<StockDbContext> _options = new DbContextOptionsBuilder<StockDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		public StockDbContext CreateDbContext() => new(_options);

		public Task<StockDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(new StockDbContext(_options));
	}
}
