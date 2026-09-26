using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System.Globalization;
using System.Xml.Linq;

namespace StockPriceSheetPrintService.Service.Application
{
	public class PortfolioCalculator(
		IHttpClientFactory httpClientFactory,
		ILogger<PortfolioCalculator> logger,
		IHtmlScraper htmlScraper,
		IMarketStackService marketStackService,
		IConfiguration configuration,
		IJuneStore juneStore,
		IYahooFinanceClient yahooFinanceClient,
		INordnetSymbolStore nordnetSymbolStore) : IPortfolioCalculator
	{
		private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
		private readonly ILogger<PortfolioCalculator> _logger = logger;
		private readonly IHtmlScraper _htmlScraper = htmlScraper;
		private readonly IYahooFinanceClient _yahooFinanceClient = yahooFinanceClient;
		private readonly IMarketStackService _marketStackService = marketStackService;
		private readonly IConfiguration _configuration = configuration;
		private readonly IJuneStore _juneStore = juneStore;
		private readonly INordnetSymbolStore _nordnetSymbolStore = nordnetSymbolStore;
		private Dictionary<string, decimal>? _exchangeRateCache;
		private DateTime _cacheDate;

		private static readonly Dictionary<string, string> ExchangeCurrencyFallback = new()
		{
			["XETR"] = "EUR",
			["XPAR"] = "EUR",
			["XAMS"] = "EUR",
			["XNAS"] = "USD",
			["XNYS"] = "USD",
			["XLON"] = "GBp",
			["XCSE"] = "DKK",
		};

		public async Task<decimal> CalculateTotalStockValueAsync(ClientContext ctx, CancellationToken ct)
		{
			decimal totalPrice = 0;

			_exchangeRateCache = null;
			var rates = await GetExchangeRatesAsync(ct);

			var nordnetSymbols = await _nordnetSymbolStore.GetSymbolsAsync();

			foreach (var (symbol, multiplier) in nordnetSymbols)
			{
				var d = await GetPriceAsync(symbol, ctx, ct);

				var effectiveCurrency = !string.IsNullOrEmpty(d.Currency) ? d.Currency
					: (ExchangeCurrencyFallback.TryGetValue(d.Exchange ?? "", out var fb) ? fb : "?");

				var closePrice = d.Close ?? 0m;
				var priceInDkk = ConvertCurrencyToDkk(closePrice, d.Currency, d.Exchange, rates);
				_logger.LogInformation("[JOB] {Multiplier} x {Symbol} closed at: {Close} {Currency} = {Dkk:F4} DKK, total: {Total:F2} DKK",
					multiplier, symbol, closePrice, effectiveCurrency, priceInDkk, multiplier * priceInDkk);
				totalPrice += priceInDkk * multiplier;
			}

			_logger.LogInformation("[JOB] Total stock value: {totalPrice:F2} DKK", totalPrice);
			return totalPrice;
		}

		// Yahoo Finance er primær kilde pr. symbol; MarketStack kaldes kun som backup,
		// og kun for det specifikke symbol Yahoo fejlede på (ikke som en forudgående batch).
		private async Task<StockPrice> GetPriceAsync(string symbol, ClientContext ctx, CancellationToken ct)
		{
			FundNav? yahooData = null;
			try
			{
				yahooData = await _yahooFinanceClient.GetFromYahooApiAsync(symbol, ctx, ct);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[STOCK-PRICE] Yahoo Finance-opslag fejlede for {Symbol}", symbol);
			}

			if (yahooData != null && yahooData.Nav != 0m)
			{
				return new StockPrice
				{
					Symbol = symbol,
					Date = yahooData.Date,
					Close = yahooData.Nav,
					Currency = yahooData.Currency ?? string.Empty
				};
			}

			_logger.LogWarning("[STOCK-PRICE] Yahoo Finance havde ingen kurs for {Symbol} – falder tilbage til MarketStack", symbol);
			var marketStackPrice = await _marketStackService.GetStockPriceAsync(symbol, ctx, ct);
			if (marketStackPrice != null && marketStackPrice.Close is not (null or 0m))
				return marketStackPrice;

			_logger.LogError("[STOCK-PRICE] Hverken Yahoo Finance eller MarketStack havde en kurs for {Symbol}", symbol);
			return new StockPrice { Symbol = symbol };
		}

		public async Task<decimal> FindTotalJuneValueAsync(ClientContext ctx, CancellationToken ct)
		{
			decimal totalJuneValue = 0m;

			var junePrice = await _htmlScraper.GetJuneNavAsync(_configuration["JuneUrl"] ?? string.Empty, ctx, ct);
			if (junePrice != null)
			{
				_logger.LogInformation("Todays June price: {nav} pr. {date}", junePrice.Nav, junePrice.Date.ToString("dd/MM/yyyy"));
				var holding = await _juneStore.GetJuneSharesAmountAsync();
				totalJuneValue = junePrice.Nav * holding.Amount;
			}

			return totalJuneValue;
		}

		private decimal ConvertCurrencyToDkk(decimal price, string? currency, string? exchange, Dictionary<string, decimal> rates)
		{
			if (string.IsNullOrEmpty(currency) && !string.IsNullOrEmpty(exchange))
			{
				if (ExchangeCurrencyFallback.TryGetValue(exchange, out var fallback))
				{
					_logger.LogWarning("[CURRENCY] {exchange} has null currency – using fallback: {currency}", exchange, fallback);
					currency = fallback;
				}
			}

			if (string.IsNullOrEmpty(currency))
			{
				_logger.LogWarning("[CURRENCY] Could not determine currency – using rate 1:1");
				return price;
			}

			if (exchange == "XLON" || currency == "GBp" || currency == "GBX")
			{
				if (rates.TryGetValue("GBP", out var gbpRate))
					return (price / 100m) * gbpRate;
			}

			if (rates.TryGetValue(currency, out var rate))
				return price * rate;

			_logger.LogWarning("[CURRENCY] Unknown currency: {currency} – using rate 1:1", currency);
			return price;
		}

		private async Task<Dictionary<string, decimal>> GetExchangeRatesAsync(CancellationToken ct)
		{
			if (_exchangeRateCache != null && _cacheDate.Date == DateTime.UtcNow.Date)
				return _exchangeRateCache;

			var client = _httpClientFactory.CreateClient("NationalbankApi");
			var response = await client.GetAsync("api/currencyratesxml?lang=da", ct);
			response.EnsureSuccessStatusCode();

			var xml = await response.Content.ReadAsStringAsync(ct);
			var doc = XDocument.Parse(xml);

			_exchangeRateCache = new Dictionary<string, decimal> { ["DKK"] = 1m };

			foreach (var c in doc.Descendants("currency"))
			{
				var code = c.Attribute("code")?.Value;
				var rateStr = c.Attribute("rate")?.Value;

				if (code != null && rateStr != null &&
					decimal.TryParse(rateStr, NumberStyles.Any, new CultureInfo("da-DK"), out decimal rate))
				{
					_exchangeRateCache[code] = (rate / 100m);
				}
			}

			_logger.LogInformation("[CURRENCY] Exchange rates – USD: {usd:F4} DKK, EUR: {eur:F4} DKK",
				_exchangeRateCache.GetValueOrDefault("USD"),
				_exchangeRateCache.GetValueOrDefault("EUR"));

			_cacheDate = DateTime.UtcNow;
			return _exchangeRateCache;
		}
	}
}
