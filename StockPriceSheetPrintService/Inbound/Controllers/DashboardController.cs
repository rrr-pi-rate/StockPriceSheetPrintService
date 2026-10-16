using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using StockPriceSheetPrintService.Inbound.Dto;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Exceptions;
using StockPriceSheetPrintService.Service.Ports.Inbound;

namespace StockPriceSheetPrintService.Inbound.Controllers
{
    [ApiController]
    [Route("dashboard")]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        [HttpGet("data")]
        [ProducesResponseType(typeof(List<DashboardDataPointDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetData(CancellationToken ct)
        {
            var ctx = ClientContextFactory.New("HTTP:dashboard");
            using var _1 = LogContext.PushProperty("CorrelationId", ctx.CorrelationId);
            using var _2 = LogContext.PushProperty("Source", ctx.Source);
            var entries = await dashboardService.GetHistoricalDataAsync(ctx, ct);
            var payload = entries.Select(e => new DashboardDataPointDto(e.Date.ToString("yyyy-MM-dd"), e.Value)).ToList();
            return Ok(payload);
        }

		[HttpGet("benchmark")]
		[ProducesResponseType(typeof(List<BenchmarkDataPointDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetBenchmarkData([FromQuery] string symbol, CancellationToken ct)
		{
			var ctx = ClientContextFactory.New("HTTP:dashboard-benchmark");
			using var _1 = LogContext.PushProperty("CorrelationId", ctx.CorrelationId);
			using var _2 = LogContext.PushProperty("Source", ctx.Source);

			var benchmarkEntries = await dashboardService.GetBenchmarkDataAsync(symbol, ctx, ct);

			var payload = benchmarkEntries.Select(e => new BenchmarkDataPointDto(
				e.Date.ToString("yyyy-MM-dd"),
				e.Value)).ToList();

			return Ok(payload);
		}

		/// <summary>
		/// Rå månedlige forbrugerprisindeks-værdier (Danmarks Statistik PRIS01). `date` er første dag i den måned indekset gælder for.
		/// </summary>
		[HttpGet("inflation")]
		[ProducesResponseType(typeof(List<BenchmarkDataPointDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status502BadGateway)]
		public async Task<IActionResult> GetInflationData(CancellationToken ct)
		{
			var ctx = ClientContextFactory.New("HTTP:dashboard-inflation");
			using var _1 = LogContext.PushProperty("CorrelationId", ctx.CorrelationId);
			using var _2 = LogContext.PushProperty("Source", ctx.Source);

			try
			{
				var entries = await dashboardService.GetInflationDataAsync(ctx, ct);
				var payload = entries.Select(e => new BenchmarkDataPointDto(
					e.Date.ToString("yyyy-MM-dd"),
					e.Value)).ToList();
				return Ok(payload);
			}
			catch (InflationUnavailableException)
			{
				return StatusCode(StatusCodes.Status502BadGateway);
			}
		}

		[HttpGet("transactions")]
		[ProducesResponseType(typeof(List<DashboardTransactionDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetTransactions(CancellationToken ct)
		{
			var ctx = ClientContextFactory.New("HTTP:dashboard-transactions");
			using var _1 = LogContext.PushProperty("CorrelationId", ctx.CorrelationId);
			using var _2 = LogContext.PushProperty("Source", ctx.Source);
			
			var transactions = await dashboardService.GetTransactionsAsync(ctx, ct);
			
			var payload = transactions.Select(t => new DashboardTransactionDto(
				t.Id,
				t.Amount,
				t.Date.ToString("yyyy-MM-dd"))).ToList();

			return Ok(payload);
		}
	}
}
