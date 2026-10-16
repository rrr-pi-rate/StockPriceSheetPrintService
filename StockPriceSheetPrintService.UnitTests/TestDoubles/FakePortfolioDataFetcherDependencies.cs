using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeSaxoTokenService : ISaxoTokenService
	{
		public bool ThrowIfCalled { get; set; }
		public string? TokenToReturn { get; set; } = "fake-token";
		public int CallCount { get; private set; }

		public Task<string?> GetAccessTokenAsync(ClientContext ctx, CancellationToken ct)
		{
			CallCount++;
			if (ThrowIfCalled)
				throw new NotSupportedException("Not used by these tests");
			return Task.FromResult(TokenToReturn);
		}
	}

	public class FakeSaxoAccountService : ISaxoAccountService
	{
		public Task<AccountBalance?> GetBalanceAsync(string accessToken, ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
		public List<Transfer>? Transfers { get; set; }
		public Task<List<Transfer>> GetSaxoTransactionsAsync(string accessToken, DateTime fromDate, DateTime toDate, ClientContext ctx, CancellationToken ct) =>
			Transfers is null
				? throw new NotSupportedException("Not used by these tests")
				: Task.FromResult(Transfers);
		public Task<List<Instrument>> GetNetPositionsAsync(string accessToken, ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
	}

	public class FakeSeenTransferStore : ISeenTransferStore
	{
		public HashSet<string> Seen { get; set; } = [];
		public List<Transfer> Saved { get; } = [];

		public List<Transfer> Transactions { get; set; } = [];

		public Task<List<Transfer>> GetTransactionsAsync(CancellationToken ct) => Task.FromResult(Transactions);

		public Task<HashSet<string>> LoadAsync(CancellationToken ct) => Task.FromResult(Seen);
		public Task SaveAsync(IEnumerable<Transfer> newTransfers, CancellationToken ct)
		{
			Saved.AddRange(newTransfers);
			return Task.CompletedTask;
		}
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
