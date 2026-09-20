using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakePendingReportStore : IPendingReportStore
	{
		private ScheduledReport? _current;
		public int ClearCallCount { get; private set; }

		public void Set(ScheduledReport report) => _current = report;
		public ScheduledReport? Get() => _current;

		public void Clear()
		{
			ClearCallCount++;
			_current = null;
		}
	}
}
