using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeSaxoTokenService : ISaxoTokenService
	{
		public Task<string?> GetAccessTokenAsync(ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
	}

	public class FakeSaxoAccountService : ISaxoAccountService
	{
		public Task<AccountBalance?> GetBalanceAsync(string accessToken, ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
		public Task<List<Transfer>> GetSaxoTransactionsAsync(string accessToken, DateTime fromDate, DateTime toDate, ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
		public Task<List<Instrument>> GetNetPositionsAsync(string accessToken, ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
	}

	public class FakeSeenTransferStore : ISeenTransferStore
	{
		public Task<HashSet<string>> LoadAsync(CancellationToken ct) => Task.FromResult(new HashSet<string>());
		public Task SaveAsync(IEnumerable<string> newIds, CancellationToken ct) => Task.CompletedTask;
	}

	public class FakeNordnetStore : INordnetStore
	{
		public CashBalance Cash { get; set; } = new(0m, DateTime.UtcNow);

		public Task<CashBalance> GetNordnetCashAmountAsync() => Task.FromResult(Cash);
		public Task SetNordnetCashAmountAsync(decimal newAmount) => Task.CompletedTask;
	}

	public class FakePortfolioCalculator : IPortfolioCalculator
	{
		public decimal StockValueToReturn { get; set; }
		public int CalculateTotalStockValueCallCount { get; private set; }

		public Task<decimal> CalculateTotalStockValueAsync(ClientContext ctx, CancellationToken ct)
		{
			CalculateTotalStockValueCallCount++;
			return Task.FromResult(StockValueToReturn);
		}

		public Task<decimal> FindTotalJuneValueAsync(ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
	}
}
