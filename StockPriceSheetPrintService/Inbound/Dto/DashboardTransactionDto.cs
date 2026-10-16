namespace StockPriceSheetPrintService.Inbound.Dto
{
	public record DashboardTransactionDto(string BookingId, decimal Amount, DateOnly Date);
}
