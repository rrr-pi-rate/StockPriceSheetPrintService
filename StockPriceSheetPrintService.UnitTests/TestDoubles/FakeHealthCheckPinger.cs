using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeHealthCheckPinger : IHealthCheckPinger
	{
		public int SuccessCount { get; private set; }
		public int FailureCount { get; private set; }

		public Task PingSuccessAsync(CancellationToken ct)
		{
			SuccessCount++;
			return Task.CompletedTask;
		}

		public Task PingFailureAsync(CancellationToken ct)
		{
			FailureCount++;
			return Task.CompletedTask;
		}
	}
}
