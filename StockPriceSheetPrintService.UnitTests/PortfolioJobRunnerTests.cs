using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class PortfolioJobRunnerTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private sealed record Fixture(
			PortfolioJobRunner Runner,
			FakeExecutionGuard ExecutionGuard,
			FakePortfolioDataFetcher DataFetcher,
			FakePortfolioReporter Reporter,
			FakeGeminiReportInsights GeminiInsights,
			FakeGeminiToggle GeminiToggle);

		private static Fixture CreateRunner()
		{
			var executionGuard = new FakeExecutionGuard();
			var dataFetcher = new FakePortfolioDataFetcher
			{
				SaxoBalance = 100m,
				NordnetValue = 200m,
				JuneValue = 300m,
			};
			var reporter = new FakePortfolioReporter();
			var geminiInsights = new FakeGeminiReportInsights();
			var geminiToggle = new FakeGeminiToggle();

			var runner = new PortfolioJobRunner(
				new TestLogger<PortfolioJobRunner>(),
				executionGuard,
				dataFetcher,
				reporter,
				geminiInsights,
				new FakeNordnetSymbolStore(),
				geminiToggle);

			return new Fixture(runner, executionGuard, dataFetcher, reporter, geminiInsights, geminiToggle);
		}

		[Fact]
		public async Task RunJobAsync_DoesNothing_WhenExecutionIsNotSafe()
		{
			var fixture = CreateRunner();
			fixture.ExecutionGuard.Safe = false;

			await fixture.Runner.RunJobAsync(Ctx, CancellationToken.None);

			Assert.Equal(0, fixture.Reporter.UpdateGoogleSheetsCallCount);
			Assert.Equal(0, fixture.Reporter.ReportMorningCallCount);
		}

		[Fact]
		public async Task RunJobAsync_CombinesFetchedValues_AndUpdatesSheetsAndReportsWithTheCombinedTotal()
		{
			var fixture = CreateRunner();
			var expectedValues = new PortfolioValues(100m, 200m, 300m);

			await fixture.Runner.RunJobAsync(Ctx, CancellationToken.None);

			Assert.Equal(1, fixture.ExecutionGuard.LogExecutionCallCount);
			Assert.Equal(1, fixture.Reporter.UpdateGoogleSheetsCallCount);
			Assert.Equal(expectedValues, fixture.Reporter.LastUpdateGoogleSheetsValues);
			Assert.Equal(1, fixture.Reporter.ReportMorningCallCount);
			Assert.Equal(expectedValues, fixture.Reporter.LastReportMorningValues);
		}

		[Fact]
		public async Task RunJobAsync_SkipsGemini_WhenToggleIsDisabled()
		{
			var fixture = CreateRunner();
			fixture.GeminiToggle.Enabled = false;

			await fixture.Runner.RunJobAsync(Ctx, CancellationToken.None);

			Assert.Equal(0, fixture.GeminiInsights.CallCount);
		}

		[Fact]
		public async Task RunJobAsync_CallsGemini_WithCombinedValues_WhenToggleIsEnabled()
		{
			var fixture = CreateRunner();
			fixture.GeminiToggle.Enabled = true;

			await fixture.Runner.RunJobAsync(Ctx, CancellationToken.None);

			Assert.Equal(1, fixture.GeminiInsights.CallCount);
			Assert.Equal(new PortfolioValues(100m, 200m, 300m), fixture.GeminiInsights.LastValues);
		}
	}
}
