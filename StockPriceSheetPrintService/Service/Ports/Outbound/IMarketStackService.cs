using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IMarketStackService
	{
		Task<StockPrice?> GetStockPriceAsync(string symbol, ClientContext ctx, CancellationToken ct);
	}
}
