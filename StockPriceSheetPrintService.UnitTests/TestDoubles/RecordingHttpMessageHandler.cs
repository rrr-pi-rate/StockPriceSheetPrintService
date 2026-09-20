using System.Net;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class RecordingHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string responseBody = "") : HttpMessageHandler
	{
		public List<(string Url, string Body)> Requests { get; } = [];

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
			Requests.Add((request.RequestUri!.ToString(), body));

			return new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(responseBody)
			};
		}
	}
}
