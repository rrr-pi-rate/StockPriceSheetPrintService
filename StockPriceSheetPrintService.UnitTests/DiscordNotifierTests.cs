using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Outbound.DiscordUpdates;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Text.Json;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DiscordNotifierTests
	{
		private const string ReportWebhook = "https://discord.example.com/report";
		private const string LoginWebhook = "https://discord.example.com/login";

		private static (DiscordNotifier Notifier, RecordingHttpMessageHandler Handler) CreateNotifier()
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Discord:Webhook"] = ReportWebhook,
					["Discord:WebhookLogin"] = LoginWebhook,
				})
				.Build();

			var handler = new RecordingHttpMessageHandler();
			var client = new HttpClient(handler);
			var notifier = new DiscordNotifier(client, configuration, new TestLogger<DiscordNotifier>());
			return (notifier, handler);
		}

		private static JsonElement GetEmbed(string body, int index) =>
			JsonDocument.Parse(body).RootElement.GetProperty("embeds")[index].Clone();

		private static string GetFieldValue(JsonElement embed, string fieldName) =>
			embed.GetProperty("fields").EnumerateArray()
				.First(f => f.GetProperty("name").GetString() == fieldName)
				.GetProperty("value").GetString()!;

		[Fact]
		public async Task SendMorningReportAsync_PostsToReportWebhook_WithDanishFormattedTotals()
		{
			var (notifier, handler) = CreateNotifier();
			var values = new PortfolioValues(1000m, 0m, 0m);

			await notifier.SendMorningReportAsync(values, dayBeforeValue: 0m, lastTransferAmount: null, geminiInsights: null, atm: "No", CancellationToken.None);

			var request = Assert.Single(handler.Requests);
			Assert.Equal(ReportWebhook, request.Url);

			var embed = GetEmbed(request.Body, 0);
			Assert.Contains("1.000,00 DKK", GetFieldValue(embed, "🏛️ Portfolio"));
			Assert.Contains("+1.000,00 DKK", GetFieldValue(embed, "📈 Change Since Yesterday"));
		}

		[Theory]
		[InlineData(100, 200, 3066993)] // total went up -> positive/green embed color
		[InlineData(200, 100, 15158332)] // total went down -> negative/red embed color
		public async Task SendMorningReportAsync_UsesCorrectEmbedColor_BasedOnChangeSinceYesterday(decimal previousDayValue, decimal saxo, int expectedColor)
		{
			var (notifier, handler) = CreateNotifier();
			var values = new PortfolioValues(saxo, 0m, 0m);

			await notifier.SendMorningReportAsync(values, previousDayValue, lastTransferAmount: null, geminiInsights: null, atm: "No", CancellationToken.None);

			var embed = GetEmbed(handler.Requests.Single().Body, 0);
			Assert.Equal(expectedColor, embed.GetProperty("color").GetInt32());
		}

		[Fact]
		public async Task SendMorningReportAsync_IncludesTransferNote_OnlyWhenTransferAmountIsPositive()
		{
			var (notifier, handler) = CreateNotifier();
			var values = new PortfolioValues(100m, 0m, 0m);

			await notifier.SendMorningReportAsync(values, 100m, lastTransferAmount: 500m, geminiInsights: null, atm: "No", CancellationToken.None);

			var embed = GetEmbed(handler.Requests.Single().Body, 0);
			Assert.Contains("Inkluderer seneste indskud", GetFieldValue(embed, "📈 Change Since Yesterday"));
		}

		[Fact]
		public async Task SendMorningReportAsync_TruncatesGeminiInsights_WhenLongerThan1024Characters()
		{
			var (notifier, handler) = CreateNotifier();
			var values = new PortfolioValues(100m, 0m, 0m);
			var longInsight = new string('a', 1030);

			await notifier.SendMorningReportAsync(values, 100m, lastTransferAmount: null, geminiInsights: longInsight, atm: "No", CancellationToken.None);

			var embed = GetEmbed(handler.Requests.Single().Body, 0);
			var insightsValue = GetFieldValue(embed, "🤖 AI Insights");
			Assert.Equal(new string('a', 1021) + "…", insightsValue);
		}

		[Fact]
		public async Task SendMorningReportAsync_OmitsAiInsightsField_WhenNoInsightsProvided()
		{
			var (notifier, handler) = CreateNotifier();
			var values = new PortfolioValues(100m, 0m, 0m);

			await notifier.SendMorningReportAsync(values, 100m, lastTransferAmount: null, geminiInsights: null, atm: "No", CancellationToken.None);

			var embed = GetEmbed(handler.Requests.Single().Body, 0);
			var fieldNames = embed.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name").GetString());
			Assert.DoesNotContain("🤖 AI Insights", fieldNames);
		}

		[Fact]
		public async Task SendLoginUrlAsync_PostsToLoginWebhook_WithTheLoginUrl()
		{
			var (notifier, handler) = CreateNotifier();

			await notifier.SendLoginUrlAsync("https://saxo.example.com/login/abc", CancellationToken.None);

			var request = Assert.Single(handler.Requests);
			Assert.Equal(LoginWebhook, request.Url);

			var embed = GetEmbed(request.Body, 0);
			Assert.Contains("https://saxo.example.com/login/abc", embed.GetProperty("description").GetString());
		}

		[Fact]
		public async Task SendMorningReportAsync_DoesNotCallDiscord_WhenWebhookIsNotConfigured()
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>())
				.Build();
			var handler = new RecordingHttpMessageHandler();
			var notifier = new DiscordNotifier(new HttpClient(handler), configuration, new TestLogger<DiscordNotifier>());

			await notifier.SendMorningReportAsync(new PortfolioValues(1m, 1m, 1m), 1m, null, null, "No", CancellationToken.None);

			Assert.Empty(handler.Requests);
		}
	}
}
