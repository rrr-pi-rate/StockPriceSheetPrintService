using System.Net;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	public class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(responseBody)
			});
	}

	public class FakeHttpClientFactory(HttpStatusCode statusCode, string responseBody) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => new(new FakeHttpMessageHandler(statusCode, responseBody));
	}
}
