using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Inbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakePortfolioJobRunner : IPortfolioJobRunner
	{
		public int CallCount { get; private set; }
		public Exception? ExceptionToThrow { get; set; }

		public Task RunJobAsync(ClientContext ctx, CancellationToken ct, bool sendDiscordImmediately = false)
		{
			CallCount++;
			if (ExceptionToThrow != null)
				throw ExceptionToThrow;
			return Task.CompletedTask;
		}
	}
}
