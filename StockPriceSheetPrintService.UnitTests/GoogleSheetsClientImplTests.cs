using StockPriceSheetPrintService.Outbound.GoogleSheets;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.UnitTests
{
	public class GoogleSheetsClientImplTests
	{
		[Fact]
		public void BuildTotalFormula_UsesDanishDecimalComma_WithoutThousandsSeparator()
		{
			var values = new PortfolioValues(1234.5m, 200m, 3m);

			var formula = GoogleSheetsClientImpl.BuildTotalFormula(values);

			Assert.Equal("=1234,50+200,00+3,00", formula);
		}

		[Fact]
		public void BuildTotalFormula_HandlesNegativeValues()
		{
			var values = new PortfolioValues(-100m, 0m, 0m);

			var formula = GoogleSheetsClientImpl.BuildTotalFormula(values);

			Assert.Equal("=-100,00+0,00+0,00", formula);
		}
	}
}
