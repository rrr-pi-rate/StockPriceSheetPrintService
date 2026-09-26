using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeHtmlScraper : IHtmlScraper
	{
		public Task<FundNav?> GetJuneNavAsync(string url, ClientContext ctx, CancellationToken token) =>
			throw new NotSupportedException("Not used by these tests");
	}
}
