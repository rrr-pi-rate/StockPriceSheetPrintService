using Microsoft.Extensions.Options;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Inbound;
using StockPriceSheetPrintService.Service.Ports.Outbound;

namespace StockPriceSheetPrintService.Service.Application
{
	public class DashboardServiceImpl(IOptions<BenchmarkOptions> benchmarkOptions, IGoogleSheetsClient googleSheetsClient, IYahooFinanceClient yahooClient, IBenchmarkStore repository, IConfiguration configuration) : IDashboardService
	{
		private readonly DateOnly from = benchmarkOptions.Value.PortfolioStartDate;
		public async Task<IReadOnlyList<BenchmarkDataPoint>> GetBenchmarkDataAsync(string symbol, ClientContext ctx, CancellationToken ct)
		{
			var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
			var latestCachedDate = await repository.GetLatestDateAsync(symbol, ct);

			if (latestCachedDate is null || latestCachedDate < yesterday)
			{
				var fetchFrom = latestCachedDate?.AddDays(1) ?? from;
				var freshData = await yahooClient.GetBenchmarkDataAsync(
					symbol, fetchFrom.ToDateTime(TimeOnly.MinValue), yesterday.ToDateTime(TimeOnly.MinValue), ctx, ct);

				await repository.InsertAsync(symbol, freshData, ct);
			}

			return await repository.GetCachedDataAsync(symbol, ct);
		}

		public Task<List<(DateOnly Date, decimal Value)>> GetHistoricalDataAsync(ClientContext ctx, CancellationToken ct)
		{
			var spreadsheetId = configuration["SheetsApi:SheetsKey"]
				?? throw new InvalidOperationException("SheetsApi:SheetsKey is not configured");
			return googleSheetsClient.GetHistoricalDataAsync(spreadsheetId, "Daily", ctx, ct);
		}
	}
}
