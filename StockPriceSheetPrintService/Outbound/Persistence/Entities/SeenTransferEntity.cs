namespace StockPriceSheetPrintService.Outbound.Persistence.Entities
{
	public sealed class SeenTransferEntity
	{
		public string BookingId { get; set; } = default!;
		public decimal Amount { get; set; }
		public DateOnly Date { get; set; }
	}
}
