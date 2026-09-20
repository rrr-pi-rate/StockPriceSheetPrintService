namespace StockPriceSheetPrintService.Service.Models
{
	public record PortfolioValues(decimal Saxo, decimal Nordnet, decimal June)
	{
		public decimal Total => Saxo + Nordnet + June;
	}
}
