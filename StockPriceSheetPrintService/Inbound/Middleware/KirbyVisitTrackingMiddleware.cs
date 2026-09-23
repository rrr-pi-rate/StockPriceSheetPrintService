using Prometheus;

namespace StockPriceSheetPrintService.Inbound.Middleware
{
	public class KirbyVisitTrackingMiddleware(RequestDelegate next, ILogger<KirbyVisitTrackingMiddleware> logger)
	{
		private static readonly string[] TrackedPaths = ["/kirby.html", "/kirby-landing.html"];

		internal static readonly Counter Visits = Metrics.CreateCounter(
			"kirby_page_visits_total",
			"Antal visninger af Kirby-siderne, fordelt på side og operativsystem",
			new CounterConfiguration { LabelNames = ["page", "os"] });

		public async Task InvokeAsync(HttpContext context)
		{
			var requestPath = context.Request.Path.Value;
			var trackedPath = requestPath is null
				? null
				: TrackedPaths.FirstOrDefault(p => string.Equals(p, requestPath, StringComparison.OrdinalIgnoreCase));

			if (trackedPath is not null)
			{
				var clientIp = ResolveClientIp(context);
				var userAgent = context.Request.Headers.UserAgent.ToString();
				var os = ClassifyOs(userAgent);
				var page = trackedPath.TrimStart('/');

				Visits.WithLabels(page, os).Inc();

				logger.LogInformation(
					"Kirby page visit {Page} from {ClientIp} using {Os} ({UserAgent})",
					page, clientIp, os, userAgent);
			}

			await next(context);
		}

		internal static string ResolveClientIp(HttpContext context)
		{
			// Cloudflare sætter denne når trafik går igennem en Cloudflare tunnel/proxy.
			if (context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && cfIp.Count > 0 && cfIp[0] is { } cf)
				return cf;

			if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && forwardedFor.Count > 0 && forwardedFor[0] is { } xff)
				return xff.Split(',')[0].Trim();

			return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		}

		internal static string ClassifyOs(string userAgent)
		{
			if (string.IsNullOrWhiteSpace(userAgent))
				return "unknown";

			return userAgent switch
			{
				_ when userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
					userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) => "iOS",
				_ when userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) => "Android",
				_ when userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) => "Windows",
				_ when userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase) => "macOS",
				_ when userAgent.Contains("CrOS", StringComparison.OrdinalIgnoreCase) => "ChromeOS",
				_ when userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) => "Linux",
				_ => "Other"
			};
		}
	}
}
