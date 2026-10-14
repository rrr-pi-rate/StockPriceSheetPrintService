using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SelectTransfersForRowTests
	{
		private static readonly DateOnly Wednesday = new(2026, 10, 7);
		private static readonly DateOnly Tuesday = Wednesday.AddDays(-1);

		[Fact]
		public void KeepsTransfersDatedAfterPreviousRowUpToAndIncludingRowDate()
		{
			var transfers = new[]
			{
				new Transfer("tue", 100m, Tuesday),
				new Transfer("wed", 200m, Wednesday),
				new Transfer("thu", 300m, Wednesday.AddDays(1)),
			};

			var result = PortfolioDataFetcher.SelectTransfersForRow(transfers, [], Tuesday, Wednesday);

			Assert.Equal("wed", Assert.Single(result).Id);
		}

		[Fact]
		public void CoversSkippedDaysBetweenRows()
		{
			var saturday = new DateOnly(2026, 10, 3);
			var tuesdayRow = new DateOnly(2026, 10, 6);
			var transfers = new[] { new Transfer("sat", 100m, saturday), new Transfer("mon", 200m, tuesdayRow) };

			var result = PortfolioDataFetcher.SelectTransfersForRow(transfers, [], new DateOnly(2026, 10, 2), tuesdayRow);

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public void SkipsTransfersFromAnEarlierRow()
		{
			var transfers = new[] { new Transfer("booked-version-of-yesterdays", 5000m, Tuesday) };

			Assert.Empty(PortfolioDataFetcher.SelectTransfersForRow(transfers, [], Tuesday, Wednesday));
		}

		[Fact]
		public void SkipsAlreadySeenTransfers()
		{
			var transfers = new[] { new Transfer("a", 100m, Wednesday), new Transfer("b", 200m, Wednesday) };

			var result = PortfolioDataFetcher.SelectTransfersForRow(transfers, ["a"], Tuesday, Wednesday);

			Assert.Equal("b", Assert.Single(result).Id);
		}

		[Fact]
		public void SkipsNonPositiveAmounts()
		{
			var transfers = new[] { new Transfer("withdrawal", -500m, Wednesday), new Transfer("zero", 0m, Wednesday) };

			Assert.Empty(PortfolioDataFetcher.SelectTransfersForRow(transfers, [], Tuesday, Wednesday));
		}

		[Fact]
		public void SkipsTransfersWithoutDate()
		{
			var transfers = new[] { new Transfer("undated", 100m, default) };

			Assert.Empty(PortfolioDataFetcher.SelectTransfersForRow(transfers, [], Tuesday, Wednesday));
		}

		[Fact]
		public void SumTransfersAfter_SumsOnlyTransfersDatedAfterRowDate()
		{
			var transfers = new[]
			{
				new Transfer("tue", 100m, Tuesday),
				new Transfer("wed", 200m, Wednesday),
				new Transfer("thu", 5000m, Wednesday.AddDays(1)),
				new Transfer("thu-withdrawal", -1000m, Wednesday.AddDays(1)),
				new Transfer("undated", 999m, default),
			};

			Assert.Equal(4000m, PortfolioDataFetcher.SumTransfersAfter(transfers, Wednesday));
		}
	}
}
