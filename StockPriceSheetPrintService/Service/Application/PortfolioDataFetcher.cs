using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Service.Application
{
	public class PortfolioDataFetcher(
		ILogger<PortfolioDataFetcher> logger,
		IConfiguration configuration,
		ISaxoTokenService saxoTokenService,
		ISaxoAccountService saxoAccountService,
		IGoogleSheetsClient googleSheetsClient,
		IPortfolioCalculator portfolioCalculator,
		ISeenTransferStore seenTransferStore,
		INordnetStore nordnetStore) : IPortfolioDataFetcher
	{
		private readonly ILogger<PortfolioDataFetcher> _logger = logger;
		private readonly IConfiguration _configuration = configuration;
		private readonly ISaxoTokenService _saxoTokenService = saxoTokenService;
		private readonly ISaxoAccountService _saxoAccountService = saxoAccountService;
		private readonly IGoogleSheetsClient _googleSheetsClient = googleSheetsClient;
		private readonly IPortfolioCalculator _portfolioCalculator = portfolioCalculator;
		private readonly ISeenTransferStore _seenTransferStore = seenTransferStore;
		private readonly INordnetStore _nordnetStore = nordnetStore;

		public async Task<decimal> GetSaxoBalanceAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var saxoToken = await _saxoTokenService.GetAccessTokenAsync(ctx, ct);
				if (saxoToken == null)
				{
					_logger.LogWarning("[FETCHER] Saxo token not available");
					return 0m;
				}

				var balance = await _saxoAccountService.GetBalanceAsync(saxoToken, ctx, ct);
				if (balance == null)
				{
					_logger.LogError("[FETCHER] Failed to get Saxo balance");
					return 0m;
				}

				_logger.LogInformation("[FETCHER] Saxo balance: {balance:F2} DKK", balance.TotalValue);
				return balance.TotalValue;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching Saxo balance");
				return 0m;
			}
		}

		public async Task<decimal> GetNordnetValueAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var stockValue = await _portfolioCalculator.CalculateTotalStockValueAsync(ctx, ct);
				var cash = await _nordnetStore.GetNordnetCashAmountAsync();
				var totalNordnetValue = stockValue + cash.Amount;

				_logger.LogInformation("[FETCHER] Nordnet value: {value:F2} DKK", totalNordnetValue);
				return totalNordnetValue;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching Nordnet value");
				return 0m;
			}
		}

		public async Task<decimal> GetJuneValueAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var juneValue = await _portfolioCalculator.FindTotalJuneValueAsync(ctx, ct);
				if (juneValue == 0m)
				{
					_logger.LogError("[FETCHER] Failed to get June value");
					return 0m;
				}

				_logger.LogInformation("[FETCHER] June value: {value:F2} DKK", juneValue);
				return juneValue;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching June value");
				return 0m;
			}
		}

		// Rækken i arket (og rapporten) dateres i går
		private static DateOnly GetRowDate() => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

		// Overførsler dateret efter rækkens dato ligger i den live Saxo-saldo, men hører til en senere række.
		// Datoløse poster (Date = default) ignoreres.
		public static decimal SumTransfersAfter(IEnumerable<Transfer> transfers, DateOnly rowDate) =>
			transfers.Where(t => t.Date > rowDate).Sum(t => t.Amount);

		// Overførsler der skal forklares i rækkens rapport: dateret efter forrige række og til og med rækkens dato,
		// ikke allerede meldt. Datoløse poster (Date = default) ignoreres.
		public static List<Transfer> SelectTransfersForRow(IEnumerable<Transfer> transfers, HashSet<string> seenIds, DateOnly previousRowDate, DateOnly rowDate) =>
			transfers
				.Where(t => t.Date > previousRowDate && t.Date <= rowDate)
				.Where(t => !seenIds.Contains(t.Id))
				.Where(t => t.Amount > 0)
				.ToList();

		private async Task<DateOnly> GetPreviousRowDateAsync(DateOnly rowDate, ClientContext ctx, CancellationToken ct)
		{
			var fallback = rowDate.AddDays(-1);
			var spreadsheetId = _configuration["SheetsApi:SheetsKey"];
			if (string.IsNullOrEmpty(spreadsheetId))
				return fallback;

			var history = await _googleSheetsClient.GetHistoricalDataAsync(spreadsheetId, "Daily", ctx, ct);
			var previous = history.Where(x => x.Date < rowDate).Select(x => x.Date).DefaultIfEmpty(fallback).Max();
			return previous;
		}

		public async Task<TransferReport> GetNewTransfersAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var rowDate = GetRowDate();

				var saxoToken = await _saxoTokenService.GetAccessTokenAsync(ctx, ct);
				if (saxoToken == null)
				{
					_logger.LogWarning("[FETCHER] Saxo token not available for transfer check");
					return TransferReport.Empty;
				}

				var transfers = await _saxoAccountService.GetSaxoTransactionsAsync(saxoToken, DateTime.UtcNow.AddDays(-14), DateTime.UtcNow, ctx, ct);
				var laterTransfersSum = SumTransfersAfter(transfers, rowDate);
				var previousRowDate = await GetPreviousRowDateAsync(rowDate, ctx, ct);

				var seenIds = await _seenTransferStore.LoadAsync(ct);
				var newTransfers = SelectTransfersForRow(transfers, seenIds, previousRowDate, rowDate);

				if (newTransfers.Count > 0)
				{
					await _seenTransferStore.SaveAsync(newTransfers, ct);
					_logger.LogInformation("[FETCHER] Found {count} new transfers", newTransfers.Count);
				}

				return new TransferReport(newTransfers, laterTransfersSum);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching transfers");
				return TransferReport.Empty;
			}
		}

		public async Task<List<Instrument>> GetNetPositionsAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var saxoToken = await _saxoTokenService.GetAccessTokenAsync(ctx, ct);
				if (saxoToken == null)
				{
					_logger.LogWarning("[FETCHER] Saxo token not available for net positions fetch");
					return [];
				}

				var positions = await _saxoAccountService.GetNetPositionsAsync(saxoToken, ctx, ct);
				_logger.LogInformation("[FETCHER] Fetched {count} Saxo net positions", positions.Count);
				return positions;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching Saxo net positions");
				return [];
			}
		}

		public async Task<decimal> GetPreviousDayValueAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var spreadsheetId = _configuration["SheetsApi:SheetsKey"];
				const string sheetName = "Daily";

				if (string.IsNullOrEmpty(spreadsheetId))
				{
					_logger.LogWarning("[FETCHER] SheetsApi:SheetsKey configuration missing, cannot fetch previous day value");
					return 0m;
				}

				var historicalData = await _googleSheetsClient.GetHistoricalDataAsync(spreadsheetId, sheetName, ctx, ct);

				if (historicalData.Count == 0)
				{
					_logger.LogWarning("[FETCHER] No historical data in Google Sheets");
					return 0m;
				}

				var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

				// Use the latest entry strictly before yesterday — the current run will write
				// yesterday's entry, so any existing entry for that date is from a same-day
				// earlier run with identical market data, which would produce a false 0 difference.
				var previousDayEntry = historicalData
					.Where(x => x.Date < yesterday)
					.OrderByDescending(x => x.Date)
					.FirstOrDefault();

				if (previousDayEntry == default)
				{
					_logger.LogWarning("[FETCHER] No entry found before {date}", yesterday);
					return 0m;
				}

				_logger.LogInformation("[FETCHER] Previous day value ({date}): {value:F2} DKK",
					previousDayEntry.Date, previousDayEntry.Value);
				return previousDayEntry.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching previous day value");
				return 0m;
			}
		}

		public async Task<decimal?> GetHighestValueAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				var spreadsheetId = _configuration["SheetsApi:SheetsKey"];
				const string sheetName = "Daily";
				if (string.IsNullOrEmpty(spreadsheetId))
				{
					_logger.LogWarning("[FETCHER] SheetsApi:SheetsKey configuration missing, cannot fetch highest value");
					return null;
				}

				var history = await _googleSheetsClient.GetHistoricalDataAsync(spreadsheetId, sheetName, ctx, ct);
				if (history.Count == 0)
				{
					_logger.LogWarning("[FETCHER] No historical values available, cannot determine all time high");
					return null;
				}

				return history.Max(h => h.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[FETCHER] Unexpected error fetching highest value");
				return null;
			}
		}
	}
}
