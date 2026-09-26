using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooChartResponse
	{
		[JsonPropertyName("chart")]
		public YahooChart Chart { get; set; } = default!;
	}
}
