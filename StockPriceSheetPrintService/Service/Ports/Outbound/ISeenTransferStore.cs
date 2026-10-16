using StockPriceSheetPrintService.Outbound.Persistence.Entities;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Service.Ports.Outbound
{
	public interface ISeenTransferStore
	{
		Task<HashSet<string>> LoadAsync(CancellationToken ct);
		Task SaveAsync(IEnumerable<Transfer> newTransfers, CancellationToken ct);
		Task<List<SeenTransferEntity>> GetTransactionsAsync(CancellationToken ct);
	}
}
