using StockPriceSheetPrintService.Outbound.Dto;
using StockPriceSheetPrintService.Outbound.Mappers;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.UnitTests
{
	public class ScheduledReportMapperTests
	{
		[Fact]
		public void ToDto_FlattensPortfolioValues_IntoIndividualFields()
		{
			var scheduledAt = new DateTime(2026, 9, 20, 7, 0, 0, DateTimeKind.Utc);
			var domain = new ScheduledReport(
				new PortfolioValues(100m, 200m, 300m),
				PreviousDayValue: 550m,
				TransferAmount: 42m,
				GeminiInsights: "insight",
				ScheduledAtUtc: scheduledAt,
				Atm: "Yes");

			var dto = ScheduledReportMapper.ToDto(domain);

			Assert.Equal(100m, dto.SaxoBalance);
			Assert.Equal(200m, dto.NordnetValue);
			Assert.Equal(300m, dto.JuneValue);
			Assert.Equal(600m, dto.Total);
			Assert.Equal(550m, dto.PreviousDayValue);
			Assert.Equal(42m, dto.TransferAmount);
			Assert.Equal("insight", dto.GeminiInsights);
			Assert.Equal(scheduledAt, dto.ScheduledAtUtc);
			Assert.Equal("Yes", dto.Atm);
		}

		[Fact]
		public void ToDomain_RebuildsPortfolioValues_FromIndividualFields()
		{
			var scheduledAt = new DateTime(2026, 9, 20, 7, 0, 0, DateTimeKind.Utc);
			var dto = new PendingReport(100m, 200m, 300m, 600m, 550m, 42m, "insight", scheduledAt, "Yes");

			var domain = ScheduledReportMapper.ToDomain(dto);

			Assert.Equal(new PortfolioValues(100m, 200m, 300m), domain.Values);
			Assert.Equal(600m, domain.Values.Total);
			Assert.Equal(550m, domain.PreviousDayValue);
			Assert.Equal(42m, domain.TransferAmount);
			Assert.Equal("insight", domain.GeminiInsights);
			Assert.Equal(scheduledAt, domain.ScheduledAtUtc);
			Assert.Equal("Yes", domain.Atm);
		}

		[Fact]
		public void RoundTrip_ToDto_ThenToDomain_PreservesValues()
		{
			var original = new ScheduledReport(
				new PortfolioValues(1m, 2m, 3m),
				PreviousDayValue: 4m,
				TransferAmount: null,
				GeminiInsights: null,
				ScheduledAtUtc: DateTime.UtcNow,
				Atm: "No");

			var roundTripped = ScheduledReportMapper.ToDomain(ScheduledReportMapper.ToDto(original));

			Assert.Equal(original, roundTripped);
		}
	}
}
