namespace StockPriceSheetPrintService.Service.Models
{
	/// <param name="NewTransfers">Overførsler der skal forklares i dagens rapport.</param>
	/// <param name="LaterTransfersSum">Sum af overførsler dateret efter rækkens dato. De ligger i den live Saxo-saldo,
	/// men hører til en senere række, og skal derfor trækkes fra saldoen.</param>
	public record TransferReport(List<Transfer> NewTransfers, decimal LaterTransfersSum)
	{
		public static TransferReport Empty => new([], 0m);
	}
}
