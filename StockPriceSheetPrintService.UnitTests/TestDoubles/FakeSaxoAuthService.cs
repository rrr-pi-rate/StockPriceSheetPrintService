using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeSaxoAuthService : ISaxoAuthService
	{
		public const string LoginUrl = "https://example.com/saxo/login";

		public Task<string> BuildLoginUrl() => Task.FromResult(LoginUrl);

		public Task<OAuthTokens> ExchangeCodeForTokensAsync(string code, ClientContext ctx, CancellationToken ct) =>
			throw new NotSupportedException("Not used by these tests");
	}
}
