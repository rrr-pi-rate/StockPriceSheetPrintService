using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface IInflationIndexProvider
	{
		/// <summary>
		/// Henter alle månedlige forbrugerprisindeks-værdier. Date = første dag i måneden indekset gælder for.
		/// Returnerer null hvis kilden ikke kunne læses (aldrig et fallback-tal).
		/// </summary>
		Task<IReadOnlyList<BenchmarkDataPoint>?> GetConsumerPriceIndexAsync(ClientContext ctx, CancellationToken ct);
	}
}
