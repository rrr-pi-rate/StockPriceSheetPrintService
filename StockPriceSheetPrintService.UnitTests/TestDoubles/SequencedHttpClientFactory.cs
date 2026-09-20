using System.Net;

namespace StockPriceSheetPrintService.UnitTests.TestDoubles
{
	// Returnerer ét svar pr. HTTP-kald, i rækkefølge – bruges til at simulere
	// "primær nøgle fejler, fallback-nøgle lykkes"-scenarier.
	public class SequencedHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
	{
		private int _index;
		public List<string> RequestUrls { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestUrls.Add(request.RequestUri!.ToString());
			var response = responses[Math.Min(_index, responses.Length - 1)];
			_index++;
			return Task.FromResult(response);
		}
	}

	public class SequencedHttpClientFactory : IHttpClientFactory
	{
		private readonly SequencedHttpMessageHandler _handler;

		public SequencedHttpClientFactory(params HttpResponseMessage[] responses)
		{
			_handler = new SequencedHttpMessageHandler(responses);
		}

		public List<string> RequestUrls => _handler.RequestUrls;

		public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
		{
			BaseAddress = new Uri("https://example.com/")
		};
	}

	public static class HttpResponses
	{
		public static HttpResponseMessage Json(HttpStatusCode statusCode, string body) =>
			new(statusCode) { Content = new StringContent(body) };
	}
}
