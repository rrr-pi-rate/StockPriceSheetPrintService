using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Outbound.Saxo;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SaxoTokenServiceTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private sealed record Fixture(
			SaxoTokenService Service,
			TestLogger<SaxoTokenService> Logger,
			FakeTokenStore TokenStore,
			FakeDiscordNotifier DiscordNotifier,
			FakeHealthCheckPinger HealthCheckPinger);

		private static Fixture CreateService(HttpStatusCode statusCode, string responseBody)
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Saxo:AppKey"] = "app-key",
					["Saxo:AppSecret"] = "app-secret",
					["Saxo:TokenEndpoint"] = "https://example.com/token",
				})
				.Build();

			var logger = new TestLogger<SaxoTokenService>();
			var tokenStore = new FakeTokenStore { RefreshToken = "existing-refresh-token" };
			var discordNotifier = new FakeDiscordNotifier();
			var healthCheckPinger = new FakeHealthCheckPinger();

			var service = new SaxoTokenService(
				logger,
				configuration,
				new FakeHttpClientFactory(statusCode, responseBody),
				tokenStore,
				new FakeSaxoAuthService(),
				discordNotifier,
				healthCheckPinger);

			return new Fixture(service, logger, tokenStore, discordNotifier, healthCheckPinger);
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsNull_AndNotifiesLogin_WhenNoRefreshTokenStored()
		{
			var fixture = CreateService(HttpStatusCode.OK, "{}");
			fixture.TokenStore.RefreshToken = null;

			var token = await fixture.Service.GetAccessTokenAsync(Ctx, CancellationToken.None);

			Assert.Null(token);
			Assert.Equal(1, fixture.DiscordNotifier.LoginUrlSentCount);
			Assert.Equal(1, fixture.HealthCheckPinger.FailureCount);
			Assert.Equal(0, fixture.HealthCheckPinger.SuccessCount);
		}

		[Fact]
		public async Task GetAccessTokenAsync_LogsStatusAndResponseBody_WhenSaxoRejectsToken()
		{
			const string body = """{"error":"invalid_grant","error_description":"Refresh token expired"}""";
			var fixture = CreateService(HttpStatusCode.BadRequest, body);

			var token = await fixture.Service.GetAccessTokenAsync(Ctx, CancellationToken.None);

			Assert.Null(token);
			Assert.Equal(1, fixture.DiscordNotifier.LoginUrlSentCount);
			Assert.Equal(1, fixture.HealthCheckPinger.FailureCount);
			Assert.Contains(fixture.Logger.Messages, m =>
				m.Contains("400") && m.Contains("invalid_grant"));
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsAccessToken_AndSavesNewRefreshToken_OnSuccess()
		{
			const string body = """{"access_token":"new-access-token","refresh_token":"new-refresh-token"}""";
			var fixture = CreateService(HttpStatusCode.OK, body);

			var token = await fixture.Service.GetAccessTokenAsync(Ctx, CancellationToken.None);

			Assert.Equal("new-access-token", token);
			Assert.Equal("new-refresh-token", fixture.TokenStore.SavedRefreshToken);
			Assert.Equal(1, fixture.HealthCheckPinger.SuccessCount);
			Assert.Equal(0, fixture.DiscordNotifier.LoginUrlSentCount);
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsNull_AndNotifiesLogin_WhenResponseIsMissingExpectedProperties()
		{
			const string body = """{"foo":"bar"}""";
			var fixture = CreateService(HttpStatusCode.OK, body);

			var token = await fixture.Service.GetAccessTokenAsync(Ctx, CancellationToken.None);

			Assert.Null(token);
			Assert.Equal(1, fixture.DiscordNotifier.LoginUrlSentCount);
			Assert.Contains(fixture.Logger.Messages, m => m.Contains("missing expected properties"));
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsNull_AndNotifiesLogin_WhenResponseHasEmptyValues()
		{
			const string body = """{"access_token":"","refresh_token":""}""";
			var fixture = CreateService(HttpStatusCode.OK, body);

			var token = await fixture.Service.GetAccessTokenAsync(Ctx, CancellationToken.None);

			Assert.Null(token);
			Assert.Equal(1, fixture.DiscordNotifier.LoginUrlSentCount);
			Assert.Contains(fixture.Logger.Messages, m => m.Contains("empty values"));
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsNull_AndNotifiesLogin_WhenResponseIsNotValidJson()
		{
			const string body = "this is not json";
			var fixture = CreateService(HttpStatusCode.OK, body);

			var token = await fixture.Service.GetAccessTokenAsync(Ctx, CancellationToken.None);

			Assert.Null(token);
			Assert.Equal(1, fixture.DiscordNotifier.LoginUrlSentCount);
			Assert.Contains(fixture.Logger.Messages, m => m.Contains("Error parsing token response"));
		}
	}
}
