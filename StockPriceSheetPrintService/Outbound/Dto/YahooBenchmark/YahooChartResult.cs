using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooChartResult
	{
		[JsonPropertyName("meta")]
		public YahooChartMeta Meta { get; set; } = default!;

		[JsonPropertyName("timestamp")]
		public long[] Timestamp { get; set; } = [];

		[JsonPropertyName("indicators")]
		public YahooIndicators Indicators { get; set; } = default!;
	}
}
