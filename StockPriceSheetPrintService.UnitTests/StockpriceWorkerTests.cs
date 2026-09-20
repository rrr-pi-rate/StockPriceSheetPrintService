using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class StockpriceWorkerTests
	{
		private static StockpriceWorker CreateWorker(FakePortfolioJobRunner jobRunner, SchedulerStatusStore statusStore) =>
			new(new TestLogger<StockpriceWorker>(), new FakeSaxoTokenService(), jobRunner, statusStore);

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
	}
}
