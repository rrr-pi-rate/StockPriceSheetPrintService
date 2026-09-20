using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	// Gør den protected ExecuteAsync (fra BackgroundService) kaldbar fra en test,
	// uden at ændre selve produktionsklassens synlighed.
	file class TestableStockpriceWorker(
		Microsoft.Extensions.Logging.ILogger<StockpriceWorker> logger,
		StockPriceSheetPrintService.Service.Ports.Outbound.ISaxoTokenService saxoTokenService,
		StockPriceSheetPrintService.Service.Ports.Inbound.IPortfolioJobRunner jobRunner,
		SchedulerStatusStore statusStore) : StockpriceWorker(logger, saxoTokenService, jobRunner, statusStore)
	{
		public Task InvokeExecuteAsync(CancellationToken ct) => ExecuteAsync(ct);
	}

	public class StockpriceWorkerTests
	{
		private static StockpriceWorker CreateWorker(FakePortfolioJobRunner jobRunner, SchedulerStatusStore statusStore, FakeSaxoTokenService? saxoTokenService = null) =>
			new(new TestLogger<StockpriceWorker>(), saxoTokenService ?? new FakeSaxoTokenService(), jobRunner, statusStore);

		[Fact]
		public void GetNextRunTime_ReturnsATimeAtTheGivenHourAndMinute_InTheFuture()
		{
			var worker = CreateWorker(new FakePortfolioJobRunner(), new SchedulerStatusStore());

			var result = worker.GetNextRunTime(3, 30);

			Assert.Equal(3, result.Hour);
			Assert.Equal(30, result.Minute);
			Assert.Equal(0, result.Second);
			Assert.True(result > DateTimeOffset.UtcNow);
		}

		[Theory]
		[InlineData(DayOfWeek.Saturday, DayOfWeek.Saturday)] // ikke weekend/mandag -> uændret
		[InlineData(DayOfWeek.Tuesday, DayOfWeek.Tuesday)]
		[InlineData(DayOfWeek.Sunday, DayOfWeek.Tuesday)]    // søndag -> springer søndag+mandag over
		[InlineData(DayOfWeek.Monday, DayOfWeek.Tuesday)]    // mandag -> springer mandag over
		public void SkipWeekends_SkipsSundayAndMonday_OtherDaysUnchanged(DayOfWeek input, DayOfWeek expected)
		{
			var candidate = new DateTimeOffset(2026, 1, 1, 3, 30, 0, TimeSpan.Zero);
			while (candidate.DayOfWeek != input)
				candidate = candidate.AddDays(1);

			var result = StockpriceWorker.SkipWeekends(candidate);

			Assert.Equal(expected, result.DayOfWeek);
		}

		[Fact]
		public async Task RunScheduledJobAsync_RecordsSuccess_WhenJobRunnerSucceeds()
		{
			var jobRunner = new FakePortfolioJobRunner();
			var statusStore = new SchedulerStatusStore();
			var worker = CreateWorker(jobRunner, statusStore);

			await worker.RunScheduledJobAsync(CancellationToken.None);

			Assert.Equal(1, jobRunner.CallCount);
			Assert.True(statusStore.LastRunSucceeded);
			Assert.NotNull(statusStore.LastRunAt);
		}

		[Fact]
		public async Task RunScheduledJobAsync_RecordsFailure_AndRethrows_WhenJobRunnerThrows()
		{
			var jobRunner = new FakePortfolioJobRunner { ExceptionToThrow = new InvalidOperationException("boom") };
			var statusStore = new SchedulerStatusStore();
			var worker = CreateWorker(jobRunner, statusStore);

			await Assert.ThrowsAsync<InvalidOperationException>(() => worker.RunScheduledJobAsync(CancellationToken.None));

			Assert.False(statusStore.LastRunSucceeded);
			Assert.NotNull(statusStore.LastRunAt);
		}

		[Fact]
		public async Task RunScheduledJobAsync_DoesNotRecordStatus_WhenCancelled()
		{
			var jobRunner = new FakePortfolioJobRunner { ExceptionToThrow = new OperationCanceledException() };
			var statusStore = new SchedulerStatusStore();
			var worker = CreateWorker(jobRunner, statusStore);

			await Assert.ThrowsAsync<OperationCanceledException>(() => worker.RunScheduledJobAsync(CancellationToken.None));

			Assert.Null(statusStore.LastRunSucceeded);
			Assert.Null(statusStore.LastRunAt);
		}

		[Fact]
		public async Task PerformStartupTokenRefreshAsync_CallsSaxoTokenService()
		{
			var saxoTokenService = new FakeSaxoTokenService();
			var worker = CreateWorker(new FakePortfolioJobRunner(), new SchedulerStatusStore(), saxoTokenService);

			await worker.PerformStartupTokenRefreshAsync(CancellationToken.None);

			Assert.Equal(1, saxoTokenService.CallCount);
		}

		[Fact]
		public async Task WaitUntilNextRunAsync_WaitsUntilTheGivenTime_WithoutRefreshingToken_WhenCloseToTarget()
		{
			var saxoTokenService = new FakeSaxoTokenService();
			var worker = CreateWorker(new FakePortfolioJobRunner(), new SchedulerStatusStore(), saxoTokenService);
			// Under 45 min til target -> springer refresh-loopet over, venter bare den korte tid.
			var nextRunUtc = DateTimeOffset.UtcNow.AddMilliseconds(200);

			await worker.WaitUntilNextRunAsync(nextRunUtc, CancellationToken.None);

			// Task.Delay's timer-præcision kan i sjældne tilfælde afvige nogle få ms -
			// giv en lille margin i stedet for et strengt ">=" der er sårbart over for det.
			Assert.True(DateTimeOffset.UtcNow >= nextRunUtc.AddMilliseconds(-20));
			Assert.Equal(0, saxoTokenService.CallCount);
		}

		[Fact]
		public async Task WaitUntilNextRunAsync_ReturnsImmediately_WhenTargetIsInThePast()
		{
			var worker = CreateWorker(new FakePortfolioJobRunner(), new SchedulerStatusStore());
			var nextRunUtc = DateTimeOffset.UtcNow.AddMinutes(-1);

			await worker.WaitUntilNextRunAsync(nextRunUtc, CancellationToken.None);

			// Ingen assertion udover at den rent faktisk returnerer uden at hænge/vente.
		}

		[Fact]
		public async Task ExecuteAsync_PerformsStartupRefresh_AndStopsImmediately_WhenAlreadyCancelled()
		{
			var saxoTokenService = new FakeSaxoTokenService();
			var jobRunner = new FakePortfolioJobRunner();
			var worker = new TestableStockpriceWorker(new TestLogger<StockpriceWorker>(), saxoTokenService, jobRunner, new SchedulerStatusStore());

			await worker.InvokeExecuteAsync(new CancellationToken(canceled: true));

			Assert.Equal(1, saxoTokenService.CallCount);
			Assert.Equal(0, jobRunner.CallCount);
		}
	}
}
