namespace StockPriceSheetPrintService.Outbound.Persistence.Entities
{
	public sealed class ExchangeRateEntity
	{
		public string Currency { get; set; } = default!;
		public DateOnly Date { get; set; }
		public decimal RateToDkk { get; set; }
	}
}
