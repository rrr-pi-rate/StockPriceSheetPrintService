using System.Text.Json.Serialization;

namespace StockPriceSheetPrintService.Outbound.Dto
{
	public sealed class FrankfurterRateResponse
	{
		[JsonPropertyName("rates")]
		public Dictionary<string, decimal> Rates { get; set; } = [];
	}
}
