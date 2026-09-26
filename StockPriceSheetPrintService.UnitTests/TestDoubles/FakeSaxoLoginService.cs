using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Inbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeSaxoLoginService : ISaxoLoginService
	{
		public string LoginUrl { get; set; } = "https://live.logonvalidation.net/authorize?client_id=test";

		public Task<string> GetLoginUrlAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(LoginUrl);
	}
}
