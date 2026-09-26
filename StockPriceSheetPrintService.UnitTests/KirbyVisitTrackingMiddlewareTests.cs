using System.Net;
using Microsoft.AspNetCore.Http;
using StockPriceSheetPrintService.Inbound.Middleware;
using StockPriceSheetPrintService.UnitTests.TestDoubles;

namespace StockPriceSheetPrintService.UnitTests
{
	public class KirbyVisitTrackingMiddlewareTests
	{
		private const string WindowsUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
		private const string MacUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15";
		private const string IPhoneUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
		private const string IPadUserAgent = "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
		private const string AndroidUserAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36";
		private const string LinuxUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
		private const string ChromeOsUserAgent = "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
		private const string CurlUserAgent = "curl/8.4.0";

		[Theory]
		[InlineData(WindowsUserAgent, "Windows")]
		[InlineData(MacUserAgent, "macOS")]
		[InlineData(IPhoneUserAgent, "iOS")]
		[InlineData(IPadUserAgent, "iOS")]
		[InlineData(AndroidUserAgent, "Android")]
		[InlineData(LinuxUserAgent, "Linux")]
		[InlineData(ChromeOsUserAgent, "ChromeOS")]
		[InlineData(CurlUserAgent, "Other")]
		public void ClassifyOs_ReturnsExpectedFamily_ForKnownUserAgents(string userAgent, string expectedOs)
		{
			Assert.Equal(expectedOs, KirbyVisitTrackingMiddleware.ClassifyOs(userAgent));
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		public void ClassifyOs_ReturnsUnknown_WhenUserAgentIsMissing(string userAgent)
		{
			Assert.Equal("unknown", KirbyVisitTrackingMiddleware.ClassifyOs(userAgent));
		}

		[Fact]
		public void ResolveClientIp_PrefersCfConnectingIp_OverOtherSources()
		{
			var context = new DefaultHttpContext();
			context.Request.Headers["CF-Connecting-IP"] = "198.51.100.1";
			context.Request.Headers["X-Forwarded-For"] = "203.0.113.9";
			context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

			Assert.Equal("198.51.100.1", KirbyVisitTrackingMiddleware.ResolveClientIp(context));
		}

		[Fact]
		public void ResolveClientIp_FallsBackToFirstXForwardedForHop_WhenNoCloudflareHeader()
		{
			var context = new DefaultHttpContext();
			context.Request.Headers["X-Forwarded-For"] = "203.0.113.9, 10.0.0.2";
			context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

			Assert.Equal("203.0.113.9", KirbyVisitTrackingMiddleware.ResolveClientIp(context));
		}

		[Fact]
		public void ResolveClientIp_FallsBackToRemoteIpAddress_WhenNoForwardingHeadersPresent()
		{
			var context = new DefaultHttpContext();
			context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.50");

			Assert.Equal("192.168.1.50", KirbyVisitTrackingMiddleware.ResolveClientIp(context));
		}

		[Fact]
		public void ResolveClientIp_ReturnsUnknown_WhenNoIpInformationIsAvailable()
		{
			var context = new DefaultHttpContext();

			Assert.Equal("unknown", KirbyVisitTrackingMiddleware.ResolveClientIp(context));
		}

		[Fact]
		public async Task InvokeAsync_TracksVisit_ForKirbyLandingPage()
		{
			var tracker = new NextInvocationTracker();
			var logger = new TestLogger<KirbyVisitTrackingMiddleware>();
			var middleware = new KirbyVisitTrackingMiddleware(tracker.Next, logger);

			var context = new DefaultHttpContext();
			context.Request.Path = "/kirby-landing.html";
			context.Request.Headers.UserAgent = WindowsUserAgent;
			context.Request.Headers["CF-Connecting-IP"] = "198.51.100.42";

			var before = KirbyVisitTrackingMiddleware.Visits.WithLabels("kirby-landing.html", "Windows").Value;

			await middleware.InvokeAsync(context);

			var after = KirbyVisitTrackingMiddleware.Visits.WithLabels("kirby-landing.html", "Windows").Value;
			Assert.Equal(before + 1, after);
			Assert.True(tracker.WasCalled);

			var message = Assert.Single(logger.Messages);
			Assert.Contains("kirby-landing.html", message);
			Assert.Contains("198.51.100.42", message);
			Assert.Contains("Windows", message);
		}

		[Fact]
		public async Task InvokeAsync_MatchesTrackedPathCaseInsensitively_AndNormalizesPageLabelToLowerCase()
		{
			var tracker = new NextInvocationTracker();
			var logger = new TestLogger<KirbyVisitTrackingMiddleware>();
			var middleware = new KirbyVisitTrackingMiddleware(tracker.Next, logger);

			var context = new DefaultHttpContext();
			context.Request.Path = "/KIRBY.HTML";
			context.Request.Headers.UserAgent = CurlUserAgent;

			var before = KirbyVisitTrackingMiddleware.Visits.WithLabels("kirby.html", "Other").Value;

			await middleware.InvokeAsync(context);

			var after = KirbyVisitTrackingMiddleware.Visits.WithLabels("kirby.html", "Other").Value;
			Assert.Equal(before + 1, after);
		}

		[Fact]
		public async Task InvokeAsync_DoesNotTrackOrLog_ForUnrelatedPaths()
		{
			var tracker = new NextInvocationTracker();
			var logger = new TestLogger<KirbyVisitTrackingMiddleware>();
			var middleware = new KirbyVisitTrackingMiddleware(tracker.Next, logger);

			var context = new DefaultHttpContext();
			context.Request.Path = "/index.html";
			context.Request.Headers.UserAgent = WindowsUserAgent;

			await middleware.InvokeAsync(context);

			Assert.True(tracker.WasCalled);
			Assert.Empty(logger.Messages);
		}

		[Fact]
		public async Task InvokeAsync_AlwaysCallsNext_RegardlessOfPath()
		{
			var tracker = new NextInvocationTracker();
			var logger = new TestLogger<KirbyVisitTrackingMiddleware>();
			var middleware = new KirbyVisitTrackingMiddleware(tracker.Next, logger);

			var context = new DefaultHttpContext();
			context.Request.Path = "/kirby.html";
			context.Request.Headers.UserAgent = MacUserAgent;

			await middleware.InvokeAsync(context);

			Assert.True(tracker.WasCalled);
		}

		private sealed class NextInvocationTracker
		{
			public bool WasCalled { get; private set; }

			public Task Next(HttpContext context)
			{
				WasCalled = true;
				return Task.CompletedTask;
			}
		}
	}
}
