using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeJuneStore : IJuneStore
	{
		public FundHolding Holding { get; set; } = new(0m, DateTime.UtcNow);

		public Task<FundHolding> GetJuneSharesAmountAsync() => Task.FromResult(Holding);
		public Task SetJuneSharesAmountAsync(decimal amount) => Task.CompletedTask;
	}
}
