using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using StockPriceSheetPrintService.Inbound.Dto;
using StockPriceSheetPrintService.Service;
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
		public async Task<IActionResult> GetBenchmarkData([FromQuery] string symbol, CancellationToken ct)
		{
			var ctx = ClientContextFactory.New("HTTP:dashboard-benchmark");
			using var _1 = LogContext.PushProperty("CorrelationId", ctx.CorrelationId);
			using var _2 = LogContext.PushProperty("Source", ctx.Source);

			var benchmarkEntries = await dashboardService.GetBenchmarkDataAsync(symbol, ctx, ct);

			var payload = benchmarkEntries.Select(e => new BenchmarkDataPointDto(
				e.Date.ToString("yyyy-MM-dd"),
				e.Value));

			return Ok(payload);
		}
	}
}
