using StockPriceSheetPrintService.Outbound.GeminiInsights;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.UnitTests
{
	public class GeminiReportInsightsImplTests
	{
		[Fact]
		public void BuildUserPrompt_IncludesAllThreePortfolioComponents_AndTheTotal()
		{
			var values = new PortfolioValues(100m, 200m, 300m);

			var prompt = GeminiReportInsightsImpl.BuildUserPrompt(
				values, previousDayValue: 500m, yesterdaysDate: "19-09-2026",
				saxoPositionsText: "positions", nordnetTickersText: "tickers", transfersText: "transfers");

			Assert.Contains($"Saxo: {100m:N2} DKK", prompt);
			Assert.Contains($"Nordnet: {200m:N2} DKK", prompt);
			Assert.Contains($"June (Danske Invest): {300m:N2} DKK", prompt);
			Assert.Contains($"Total: {600m:N2} DKK", prompt);
		}

		[Theory]
		[InlineData(500, 600, "+")] // total increased -> positive sign
		[InlineData(600, 500, "")] // total decreased -> no sign (negative number carries its own "-")
		public void BuildUserPrompt_UsesCorrectSign_ForChangeSincePreviousDay(decimal previousDayValue, decimal saxo, string expectedSign)
		{
			var values = new PortfolioValues(saxo, 0m, 0m);

			var prompt = GeminiReportInsightsImpl.BuildUserPrompt(
				values, previousDayValue, yesterdaysDate: "19-09-2026",
				saxoPositionsText: "positions", nordnetTickersText: "tickers", transfersText: "transfers");

			var change = values.Total - previousDayValue;
			Assert.Contains($"Ændring siden i går: {expectedSign}{change:N2} DKK", prompt);
		}

		[Fact]
		public void BuildUserPrompt_IncludesSuppliedContextText()
		{
			var prompt = GeminiReportInsightsImpl.BuildUserPrompt(
				new PortfolioValues(1m, 1m, 1m), previousDayValue: 3m, yesterdaysDate: "01-01-2026",
				saxoPositionsText: "MY_POSITIONS", nordnetTickersText: "MY_TICKERS", transfersText: "MY_TRANSFERS");

			Assert.Contains("01-01-2026", prompt);
			Assert.Contains("MY_POSITIONS", prompt);
			Assert.Contains("MY_TICKERS", prompt);
			Assert.Contains("MY_TRANSFERS", prompt);
		}
	}
}
