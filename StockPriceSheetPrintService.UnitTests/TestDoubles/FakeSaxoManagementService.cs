using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Ports.Inbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeSaxoManagementService : ISaxoManagementService
	{
		public SaxoCallbackResult CallbackResult { get; set; } = new(0m, "DKK");
		public Exception? CallbackExceptionToThrow { get; set; }
		public string? AccessToken { get; set; }

		public Task<SaxoCallbackResult> HandleCallbackAsync(string code, ClientContext ctx, CancellationToken ct)
		{
			if (CallbackExceptionToThrow != null)
				throw CallbackExceptionToThrow;
			return Task.FromResult(CallbackResult);
		}

		public Task<string?> GetOrRefreshAccessTokenAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(AccessToken);
	}
}
