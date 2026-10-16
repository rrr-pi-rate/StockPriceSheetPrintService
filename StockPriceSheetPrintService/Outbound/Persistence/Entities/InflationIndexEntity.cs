namespace StockPriceSheetPrintService.Outbound.Persistence.Entities
{
	public sealed class InflationIndexEntity
	{
		public DateOnly Month { get; set; }
		public double IndexValue { get; set; }
		public DateTimeOffset FetchedAt { get; set; }
	}
}
