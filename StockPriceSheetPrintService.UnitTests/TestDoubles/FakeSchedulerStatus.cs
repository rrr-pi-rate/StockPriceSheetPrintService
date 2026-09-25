using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeSchedulerStatus : ISchedulerStatus
	{
		public DateTimeOffset? NextRunAt { get; set; }
		public DateTimeOffset? NextTokenRefreshAt { get; set; }
		public DateTimeOffset? LastRunAt { get; set; }
		public bool? LastRunSucceeded { get; set; }
	}
}
