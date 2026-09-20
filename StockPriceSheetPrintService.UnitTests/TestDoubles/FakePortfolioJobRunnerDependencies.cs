using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeExecutionGuard : IExecutionGuard
	{
		public bool Safe { get; set; } = true;
		public int LogExecutionCallCount { get; private set; }

		public bool IsExecutionSafe() => Safe;
		public void LogExecution() => LogExecutionCallCount++;
	}

	public class FakePortfolioDataFetcher : IPortfolioDataFetcher
	{
		public decimal SaxoBalance { get; set; }
		public decimal NordnetValue { get; set; }
		public decimal JuneValue { get; set; }
		public List<Transfer> Transfers { get; set; } = [];
		public decimal PreviousDayValue { get; set; }
		public List<Instrument> NetPositions { get; set; } = [];
		public string Atm { get; set; } = "No";

		public Task<decimal> GetSaxoBalanceAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(SaxoBalance);
		public Task<decimal> GetNordnetValueAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(NordnetValue);
		public Task<decimal> GetJuneValueAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(JuneValue);
		public Task<List<Transfer>> GetNewTransfersAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(Transfers);
		public Task<decimal> GetPreviousDayValueAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(PreviousDayValue);
		public Task<List<Instrument>> GetNetPositionsAsync(ClientContext ctx, CancellationToken ct) => Task.FromResult(NetPositions);
		public Task<string> GetAtmValue(ClientContext ctx, CancellationToken ct) => Task.FromResult(Atm);
	}

	public class FakeNordnetSymbolStore : INordnetSymbolStore
	{
		public Dictionary<string, decimal> Symbols { get; set; } = [];

		public Task<Dictionary<string, decimal>> GetSymbolsAsync() => Task.FromResult(Symbols);
		public Task AddOrUpdateSymbolAsync(string ticker, decimal shares) => Task.CompletedTask;
		public Task RemoveSymbolAsync(string ticker) => Task.CompletedTask;
	}

	public class FakeGeminiToggle : IGeminiToggle
	{
		public bool Enabled { get; set; }

		public Task<bool> IsEnabledAsync() => Task.FromResult(Enabled);
		public Task ToggleAsync() => Task.CompletedTask;
	}

	public class FakeGeminiReportInsights : IGeminiReportInsights
	{
		public int CallCount { get; private set; }
		public PortfolioValues? LastValues { get; private set; }
		public string? InsightsToReturn { get; set; }

		public Task<string?> GetInsightsAsync(PortfolioValues values, decimal previousDayValue, List<Transfer> newTransfers, List<string> nordnetTickers, List<Instrument> saxoPositions, ClientContext ctx, CancellationToken ct)
		{
			CallCount++;
			LastValues = values;
			return Task.FromResult(InsightsToReturn);
		}
	}

	public class FakePortfolioReporter : IPortfolioReporter
	{
		public int UpdateGoogleSheetsCallCount { get; private set; }
		public PortfolioValues? LastUpdateGoogleSheetsValues { get; private set; }
		public int ReportMorningCallCount { get; private set; }
		public PortfolioValues? LastReportMorningValues { get; private set; }

		public Task UpdateGoogleSheetsAsync(PortfolioValues values, ClientContext ctx, CancellationToken ct)
		{
			UpdateGoogleSheetsCallCount++;
			LastUpdateGoogleSheetsValues = values;
			return Task.CompletedTask;
		}

		public Task ReportMorningAsync(PortfolioValues values, decimal previousDayValue, List<Transfer> newTransfers, bool sendDiscordImmediately, string? geminiInsights, string atm, ClientContext ctx, CancellationToken ct)
		{
			ReportMorningCallCount++;
			LastReportMorningValues = values;
			return Task.CompletedTask;
		}
	}
}
