using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooQuote
	{
		[JsonPropertyName("close")]
		public double?[] Close { get; set; } = [];
	}
}
