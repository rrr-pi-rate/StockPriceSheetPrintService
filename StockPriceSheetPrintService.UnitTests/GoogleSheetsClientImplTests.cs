using Google.Apis.Sheets.v4;
using StockPriceSheetPrintService.Outbound.GoogleSheets;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

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
		public async Task CreateServiceAsync_BuildsServiceOnce_AndReusesIt()
		{
			var builds = 0;
			var client = new GoogleSheetsClientImpl(new TestLogger<GoogleSheetsClientImpl>())
			{
				ServiceFactory = () =>
				{
					builds++;
					return Task.FromResult(new SheetsService());
				}
			};

			var first = await client.CreateServiceAsync(CancellationToken.None);
			var second = await client.CreateServiceAsync(CancellationToken.None);

			Assert.Same(first, second);
			Assert.Equal(1, builds);
		}

		[Fact]
		public async Task CreateServiceAsync_RetriesAfterFailedBuild()
		{
			var builds = 0;
			var client = new GoogleSheetsClientImpl(new TestLogger<GoogleSheetsClientImpl>())
			{
				ServiceFactory = () =>
				{
					builds++;
					return builds == 1
						? Task.FromException<SheetsService>(new IOException("credentials missing"))
						: Task.FromResult(new SheetsService());
				}
			};

			await Assert.ThrowsAsync<IOException>(() => client.CreateServiceAsync(CancellationToken.None));
			var service = await client.CreateServiceAsync(CancellationToken.None);

			Assert.NotNull(service);
			Assert.Equal(2, builds);
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
