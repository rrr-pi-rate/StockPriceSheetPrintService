using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SelectTransfersForRowTests
	{
		private static readonly DateOnly Wednesday = new(2026, 10, 7);

		[Fact]
		public void KeepsOnlyTransfersBookedOnTheRowDate()
		{
			var transfers = new[]
			{
				new Transfer("tue", 100m, Wednesday.AddDays(-1)),
				new Transfer("wed", 200m, Wednesday),
				new Transfer("thu", 300m, Wednesday.AddDays(1)),
			};

			var result = PortfolioDataFetcher.SelectTransfersForRow(transfers, [], Wednesday);

			Assert.Equal("wed", Assert.Single(result).Id);
		}

		[Fact]
		public void SkipsAlreadySeenTransfers()
		{
			var transfers = new[] { new Transfer("a", 100m, Wednesday), new Transfer("b", 200m, Wednesday) };

			var result = PortfolioDataFetcher.SelectTransfersForRow(transfers, ["a"], Wednesday);

			Assert.Equal("b", Assert.Single(result).Id);
		}

		[Fact]
		public void SkipsNonPositiveAmounts()
		{
			var transfers = new[] { new Transfer("withdrawal", -500m, Wednesday), new Transfer("zero", 0m, Wednesday) };

			Assert.Empty(PortfolioDataFetcher.SelectTransfersForRow(transfers, [], Wednesday));
		}
	}
}
