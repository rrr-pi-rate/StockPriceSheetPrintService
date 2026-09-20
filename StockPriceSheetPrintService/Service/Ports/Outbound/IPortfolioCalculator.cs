using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IPortfolioCalculator
	{
		Task<decimal> CalculateTotalStockValueAsync(ClientContext ctx, CancellationToken ct);
		Task<decimal> FindTotalJuneValueAsync(ClientContext ctx, CancellationToken ct);
	}
}
