namespace StockPriceSheetPrintService.Outbound.Dto.Saxo.Transactions
{
	public class SaxoTransaction
	{
		public string BookingId { get; set; } = string.Empty;
		public decimal BookedAmount { get; set; }
		public decimal IntradayAmount { get; set; }
		public bool IsIntradayData { get; set; }
		public DateOnly Date { get; set; }
		public string FundingSubType { get; set; } = string.Empty;

		public decimal Amount => BookedAmount != 0 ? BookedAmount : IntradayAmount;
	}
}
