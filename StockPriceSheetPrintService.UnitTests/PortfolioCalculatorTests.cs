using Microsoft.Extensions.Configuration;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Application;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class PortfolioCalculatorTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		// Ingen currency-elementer nødvendige – "DKK" er altid 1:1 i cachen,
		// og alle testens kurser bruger DKK, så valutakonvertering bliver en no-op.
		private const string EmptyNationalbankXml = "<currencies></currencies>";

		private sealed record Fixture(
			PortfolioCalculator Calculator,
			FakeHtmlScraper HtmlScraper,
			FakeMarketStackService MarketStackService,
			FakeNordnetSymbolStore SymbolStore);

		private static Fixture CreateCalculator(string symbol, int multiplier)
		{
			var configuration = new ConfigurationBuilder().Build();
			var htmlScraper = new FakeHtmlScraper();
			var marketStackService = new FakeMarketStackService();
			var symbolStore = new FakeNordnetSymbolStore
			{
				Symbols = new Dictionary<string, decimal> { [symbol] = multiplier }
			};

			var calculator = new PortfolioCalculator(
				new FakeHttpClientFactory(HttpStatusCode.OK, EmptyNationalbankXml),
				new TestLogger<PortfolioCalculator>(),
				htmlScraper,
				marketStackService,
				configuration,
				new FakeJuneStore(),
				symbolStore);

			return new Fixture(calculator, htmlScraper, marketStackService, symbolStore);
		}

		[Fact]
		public async Task CalculateTotalStockValueAsync_UsesYahooPrice_AndNeverCallsMarketStack_WhenYahooSucceeds()
		{
			var fixture = CreateCalculator("TEST", multiplier: 10);
			fixture.HtmlScraper.YahooResultsBySymbol["TEST"] = new FundNav { Nav = 100m, Currency = "DKK", Date = DateTime.UtcNow };

			var total = await fixture.Calculator.CalculateTotalStockValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(1000m, total);
			Assert.Equal(0, fixture.MarketStackService.CallCountBySymbol.GetValueOrDefault("TEST"));
		}

		[Fact]
		public async Task CalculateTotalStockValueAsync_FallsBackToMarketStack_WhenYahooReturnsNoData()
		{
			var fixture = CreateCalculator("TEST", multiplier: 10);
			// Ingen Yahoo-resultat konfigureret -> GetFromYahooApiAsync returnerer null
			fixture.MarketStackService.PricesBySymbol["TEST"] = new StockPrice { Symbol = "TEST", Close = 50m, Currency = "DKK" };

			var total = await fixture.Calculator.CalculateTotalStockValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(500m, total);
			Assert.Equal(1, fixture.MarketStackService.CallCountBySymbol["TEST"]);
		}

		[Fact]
		public async Task CalculateTotalStockValueAsync_FallsBackToMarketStack_WhenYahooReturnsZeroNav()
		{
			var fixture = CreateCalculator("TEST", multiplier: 10);
			fixture.HtmlScraper.YahooResultsBySymbol["TEST"] = new FundNav { Nav = 0m, Currency = "DKK", Date = DateTime.UtcNow };
			fixture.MarketStackService.PricesBySymbol["TEST"] = new StockPrice { Symbol = "TEST", Close = 50m, Currency = "DKK" };

			var total = await fixture.Calculator.CalculateTotalStockValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(500m, total);
		}

		[Fact]
		public async Task CalculateTotalStockValueAsync_FallsBackToMarketStack_WhenYahooThrows()
		{
			var fixture = CreateCalculator("TEST", multiplier: 10);
			fixture.HtmlScraper.SymbolsThatThrow.Add("TEST");
			fixture.MarketStackService.PricesBySymbol["TEST"] = new StockPrice { Symbol = "TEST", Close = 75m, Currency = "DKK" };

			var total = await fixture.Calculator.CalculateTotalStockValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(750m, total);
			Assert.Equal(1, fixture.MarketStackService.CallCountBySymbol["TEST"]);
		}

		[Fact]
		public async Task CalculateTotalStockValueAsync_ContributesZero_WhenBothYahooAndMarketStackFail()
		{
			var fixture = CreateCalculator("TEST", multiplier: 10);
			// Hverken Yahoo- eller MarketStack-resultat konfigureret -> begge returnerer null

			var total = await fixture.Calculator.CalculateTotalStockValueAsync(Ctx, CancellationToken.None);

			Assert.Equal(0m, total);
			Assert.Equal(1, fixture.MarketStackService.CallCountBySymbol["TEST"]);
		}
	}
}
