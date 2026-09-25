using Microsoft.AspNetCore.Mvc;
using StockPriceSheetPrintService.Inbound.Controllers;
using StockPriceSheetPrintService.Inbound.Dto;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SaxoAuthControllerTests
	{
		private static SaxoAuthController CreateController(
			FakeSaxoLoginService? loginService = null,
			FakeSaxoManagementService? managementService = null,
			FakePortfolioJobRunner? jobRunner = null) =>
			new(
				loginService ?? new FakeSaxoLoginService(),
				managementService ?? new FakeSaxoManagementService(),
				jobRunner ?? new FakePortfolioJobRunner(),
				new TestLogger<SaxoAuthController>());

		[Fact]
		public async Task GetLoginUrl_ReturnsContentResult_WithLoginUrl()
		{
			var loginService = new FakeSaxoLoginService { LoginUrl = "https://example.com/authorize" };
			var controller = CreateController(loginService: loginService);

			var result = await controller.GetLoginUrl(CancellationToken.None);

			var contentResult = Assert.IsType<ContentResult>(result);
			Assert.Equal("https://example.com/authorize", contentResult.Content);
		}

		[Fact]
		public async Task Callback_ReturnsBadRequest_WhenCodeIsMissing()
		{
			var controller = CreateController();

			var result = await controller.Callback(string.Empty, CancellationToken.None);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public async Task Callback_ReturnsOk_WithCallbackSuccessResponseDto_WhenCodeIsValid()
		{
			var controller = CreateController();

			var result = await controller.Callback("valid-code", CancellationToken.None);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var response = Assert.IsType<CallbackSuccessResponseDto>(okResult.Value);
			Assert.Equal("Everything is set up! Your worker will now run automatically.", response.Message);
		}

		[Fact]
		public async Task Callback_Returns500_WhenManagementServiceThrows()
		{
			var managementService = new FakeSaxoManagementService { CallbackExceptionToThrow = new InvalidOperationException("boom") };
			var controller = CreateController(managementService: managementService);

			var result = await controller.Callback("valid-code", CancellationToken.None);

			var statusResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(500, statusResult.StatusCode);
		}

		[Fact]
		public async Task TriggerJob_ReturnsOk_AndRunsJob()
		{
			var jobRunner = new FakePortfolioJobRunner();
			var controller = CreateController(jobRunner: jobRunner);

			var result = await controller.TriggerJob(CancellationToken.None);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var response = Assert.IsType<MessageResponseDto>(okResult.Value);
			Assert.Equal("Job completed.", response.Message);
			Assert.Equal(1, jobRunner.CallCount);
		}

		[Fact]
		public async Task RefreshSaxoAccessTokenAsync_ReturnsNotFound_WhenNoTokenIsAvailable()
		{
			var managementService = new FakeSaxoManagementService { AccessToken = null };
			var controller = CreateController(managementService: managementService);

			var result = await controller.RefreshSaxoAccessTokenAsync(CancellationToken.None);

			var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
			Assert.IsType<MessageResponseDto>(notFoundResult.Value);
		}

		[Fact]
		public async Task RefreshSaxoAccessTokenAsync_ReturnsOk_WhenTokenIsAvailable()
		{
			var managementService = new FakeSaxoManagementService { AccessToken = "token-123" };
			var controller = CreateController(managementService: managementService);

			var result = await controller.RefreshSaxoAccessTokenAsync(CancellationToken.None);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var response = Assert.IsType<MessageResponseDto>(okResult.Value);
			Assert.Equal("Token refresh completed.", response.Message);
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsNotFound_WhenNoTokenIsAvailable()
		{
			var managementService = new FakeSaxoManagementService { AccessToken = null };
			var controller = CreateController(managementService: managementService);

			var result = await controller.GetAccessTokenAsync(CancellationToken.None);

			var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
			Assert.IsType<MessageResponseDto>(notFoundResult.Value);
		}

		[Fact]
		public async Task GetAccessTokenAsync_ReturnsOk_WithAccessTokenResponseDto_WhenTokenIsAvailable()
		{
			var managementService = new FakeSaxoManagementService { AccessToken = "token-123" };
			var controller = CreateController(managementService: managementService);

			var result = await controller.GetAccessTokenAsync(CancellationToken.None);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var response = Assert.IsType<AccessTokenResponseDto>(okResult.Value);
			Assert.Equal("token-123", response.AccessToken);
		}
	}
}
