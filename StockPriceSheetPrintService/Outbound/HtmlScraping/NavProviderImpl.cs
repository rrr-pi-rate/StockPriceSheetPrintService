using HtmlAgilityPack;
using StockPriceSheetPrintService.Outbound.Dto;
using StockPriceSheetPrintService.Outbound.Mappers;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StockPriceSheetPrintService.Outbound.HtmlScraping
{
	public class NavProviderImpl(HttpClient client, ILogger<NavProviderImpl> logger) : IHtmlScraper
	{
		public async Task<FundNav?> GetJuneNavAsync(string url, ClientContext ctx, CancellationToken token)
		{
			var html = await client.GetStringAsync(url, token);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var valueNode = doc.DocumentNode.SelectSingleNode(
				"//div[contains(@class,'fund-number')]" +
				"[.//small[contains(@class,'description') and starts-with(normalize-space(.), 'Indre værdi pr')]]" +
				"//p[contains(@class,'value')]"
			);

			if (valueNode == null)
			{
				logger.LogError("[JUNE-NAV] Value node not found in HTML from {url} – siden har sandsynligvis ændret struktur", url);
				return null;
			}

			var navText = valueNode.InnerText.Trim().Replace(",", ".");
			if (!decimal.TryParse(navText, NumberStyles.Number, CultureInfo.InvariantCulture, out var nav))
			{
				logger.LogError("[JUNE-NAV] Could not parse NAV value from text: '{navText}'", navText);
				return null;
			}

			var smallNode = doc.DocumentNode.SelectSingleNode(
				"//div[contains(@class,'fund-number')]" +
				"//small[contains(@class,'description') and starts-with(normalize-space(.), 'Indre værdi pr')]"
			);

			var dateMatch = Regex.Match(smallNode?.InnerText ?? "", @"\d{2}\.\d{2}\.\d{4}", RegexOptions.None, TimeSpan.FromSeconds(1));
			if (!dateMatch.Success)
			{
				logger.LogError("[JUNE-NAV] No date found in description text: '{text}'", smallNode?.InnerText);
				return null;
			}

			if (!DateTime.TryParseExact(dateMatch.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			{
				logger.LogError("[JUNE-NAV] Could not parse date: '{date}'", dateMatch.Value);
				return null;
			}

			return JuneMapper.ToFundNav(new JuneData { Nav = nav, Date = date });
		}

		public async Task<FundNav?> GetFromYahooApiAsync(string ticker, ClientContext ctx, CancellationToken token)
		{
			var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}";
			var json = await client.GetStringAsync(url, token);

			using var doc = JsonDocument.Parse(json);
			var result = doc.RootElement
				.GetProperty("chart")
				.GetProperty("result")[0];

			var price = result
				.GetProperty("meta")
				.GetProperty("regularMarketPrice")
				.GetDecimal();

			var timestamp = result
				.GetProperty("meta")
				.GetProperty("regularMarketTime")
				.GetInt64();

			var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;

			var currency = result.GetProperty("meta").TryGetProperty("currency", out var currencyProp)
				? currencyProp.GetString()
				: null;

			return JuneMapper.ToFundNav(new JuneData { Nav = price, Date = date, Currency = currency });
		}
	}
}
