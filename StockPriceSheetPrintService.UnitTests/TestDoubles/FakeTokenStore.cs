using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeTokenStore : ITokenStore
	{
		public string? RefreshToken { get; set; }
		public string? SavedRefreshToken { get; private set; }

		public Task<string?> ReadRefreshTokenAsync(CancellationToken ct) => Task.FromResult(RefreshToken);

		public Task SaveRefreshTokenAsync(string refreshToken, CancellationToken ct)
		{
			SavedRefreshToken = refreshToken;
			return Task.CompletedTask;
		}

		public bool TokenExists() => RefreshToken != null;
	}
}
