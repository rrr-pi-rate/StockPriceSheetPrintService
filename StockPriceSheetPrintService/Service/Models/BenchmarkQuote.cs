namespace StockPriceSheetPrintService.Service.Models
{
	public sealed record BenchmarkQuote(string? Currency, IReadOnlyList<BenchmarkDataPoint> Points);
}
