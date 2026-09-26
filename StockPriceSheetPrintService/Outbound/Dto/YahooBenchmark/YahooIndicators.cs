using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooIndicators
	{
		[JsonPropertyName("quote")]
		public YahooQuote[] Quote { get; set; } = [];
	}
}
