using Microsoft.AspNetCore.Mvc;
using StockPriceSheetPrintService.Inbound.Controllers;
using StockPriceSheetPrintService.Inbound.Dto;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class DashboardControllerTests
	{
		[Fact]
		public async Task GetData_ReturnsOk_WithDashboardDataPointDtosMappedFromHistoricalEntries()
		{
			var dashboardService = new FakeDashboardService
			{
				Entries =
				[
					(new DateOnly(2026, 1, 2), 100.5m),
					(new DateOnly(2026, 1, 3), 101.25m)
				]
			};
			var controller = new DashboardController(dashboardService);

			var result = await controller.GetData(CancellationToken.None);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var payload = Assert.IsType<List<DashboardDataPointDto>>(okResult.Value);
			Assert.Equal(2, payload.Count);
			Assert.Equal("2026-01-02", payload[0].Date);
			Assert.Equal(100.5m, payload[0].Value);
			Assert.Equal("2026-01-03", payload[1].Date);
			Assert.Equal(101.25m, payload[1].Value);
		}

		[Fact]
		public async Task GetData_ReturnsOk_WithEmptyList_WhenNoHistoricalDataExists()
		{
			var dashboardService = new FakeDashboardService { Entries = [] };
			var controller = new DashboardController(dashboardService);

			var result = await controller.GetData(CancellationToken.None);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var payload = Assert.IsType<List<DashboardDataPointDto>>(okResult.Value);
			Assert.Empty(payload);
		}
	}
}
