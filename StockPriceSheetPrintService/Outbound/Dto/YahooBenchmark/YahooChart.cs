using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto.YahooBenchmark
{
	public sealed class YahooChart
	{
		[JsonPropertyName("result")]
		public YahooChartResult[]? Result { get; set; }

		[JsonPropertyName("error")]
		public YahooChartError? Error { get; set; }
	}
}
