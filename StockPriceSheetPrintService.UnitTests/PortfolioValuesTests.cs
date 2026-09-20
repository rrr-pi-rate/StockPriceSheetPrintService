using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.UnitTests
{
	public class PortfolioValuesTests
	{
		[Theory]
		[InlineData(100, 200, 300, 600)]
		[InlineData(0, 0, 0, 0)]
		[InlineData(1000.50, -200.25, 0, 800.25)]
		public void Total_IsSumOfTheThreeComponents(decimal saxo, decimal nordnet, decimal june, decimal expectedTotal)
		{
			var values = new PortfolioValues(saxo, nordnet, june);

			Assert.Equal(expectedTotal, values.Total);
		}

		[Fact]
		public void Total_ReflectsComponentValues_NotJustConstructionOrder()
		{
			var values = new PortfolioValues(Saxo: 10m, Nordnet: 20m, June: 30m);

			Assert.Equal(10m, values.Saxo);
			Assert.Equal(20m, values.Nordnet);
			Assert.Equal(30m, values.June);
			Assert.Equal(60m, values.Total);
		}

		[Fact]
		public void RecordEquality_ComparesByValue()
		{
			var a = new PortfolioValues(1m, 2m, 3m);
			var b = new PortfolioValues(1m, 2m, 3m);

			Assert.Equal(a, b);
		}
	}
}
