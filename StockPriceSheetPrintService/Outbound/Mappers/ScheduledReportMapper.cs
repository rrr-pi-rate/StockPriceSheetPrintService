using StockPriceSheetPrintService.Outbound.Dto;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Outbound.Mappers
{
	public static class ScheduledReportMapper
	{
		public static PendingReport ToDto(ScheduledReport domain) =>
			new(domain.Values.Saxo, domain.Values.Nordnet, domain.Values.June,
				domain.Values.Total, domain.PreviousDayValue, domain.TransferAmount,
				domain.GeminiInsights, domain.ScheduledAtUtc, domain.Atm);

		public static ScheduledReport ToDomain(PendingReport dto) =>
			new(new PortfolioValues(dto.SaxoBalance, dto.NordnetValue, dto.JuneValue),
				dto.PreviousDayValue, dto.TransferAmount,
				dto.GeminiInsights, dto.ScheduledAtUtc, dto.Atm);
	}
}
