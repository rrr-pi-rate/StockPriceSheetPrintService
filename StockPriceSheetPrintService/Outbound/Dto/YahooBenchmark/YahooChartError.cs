using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooChartError
	{
		[JsonPropertyName("code")]
		public string Code { get; set; } = default!;

		[JsonPropertyName("description")]
		public string Description { get; set; } = default!;
	}
}
