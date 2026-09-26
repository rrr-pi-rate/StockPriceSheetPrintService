namespace StockPriceSheetPrintService.Outbound.Persistence.Entities
{
	public sealed class BenchmarkDataEntity
	{
		public string Symbol { get; set; } = default!;
		public DateOnly Date { get; set; }
		public double CloseValue { get; set; }
		public DateTimeOffset FetchedAt { get; set; }
	}
}
