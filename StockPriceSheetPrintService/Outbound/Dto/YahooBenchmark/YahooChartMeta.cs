using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooChartMeta
	{
		[JsonPropertyName("currency")]
		public string Currency { get; set; } = default!;

		[JsonPropertyName("symbol")]
		public string Symbol { get; set; } = default!;
	}
}
