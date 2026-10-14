namespace StockPriceSheetPrintService.Outbound.Persistence.Entities
{
	public class SeenTransferEntity
	{
		public string BookingId { get; set; } = "";
		public decimal Amount { get; set; }
		public DateTime SeenAt { get; set; }
	}
}
