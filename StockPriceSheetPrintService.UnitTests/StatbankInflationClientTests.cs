using StockPriceSheetPrintService.Outbound.Statbank;
using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.UnitTests.TestDoubles;
using System.Net;

namespace StockPriceSheetPrintService.UnitTests
{
	public class StatbankInflationClientTests
	{
		private static readonly ClientContext Ctx = new(Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);

		private const string Csv = "﻿VAREGR;ENHED;TID;INDHOLD\r\n00 Forbrugerprisindeks i alt;Indeks;2025M09;100,41\r\n00 Forbrugerprisindeks i alt;Indeks;2026M09;102,60\r\n00 Forbrugerprisindeks i alt;Indeks;2026M10;..\r\n";

		private static StatbankInflationClient CreateClient(HttpStatusCode statusCode, string body)
		{
			var client = new HttpClient(new FakeHttpMessageHandler(statusCode, body))
			{
				BaseAddress = new Uri("https://api.statbank.dk/")
			};
			return new StatbankInflationClient(client, new TestLogger<StatbankInflationClient>());
		}

		[Fact]
		public async Task GetConsumerPriceIndexAsync_ParsesDanishDecimalsAndFirstOfMonthDates()
		{
			var result = await CreateClient(HttpStatusCode.OK, Csv).GetConsumerPriceIndexAsync(Ctx, CancellationToken.None);

			Assert.NotNull(result);
			Assert.Equal(2, result.Count);
			Assert.Equal(new DateTime(2025, 9, 1), result[0].Date);
			Assert.Equal(100.41, result[0].Value);
			Assert.Equal(new DateTime(2026, 9, 1), result[1].Date);
			Assert.Equal(102.60, result[1].Value);
		}

		[Fact]
		public async Task GetConsumerPriceIndexAsync_ReturnsNull_OnHttpError()
		{
			var result = await CreateClient(HttpStatusCode.InternalServerError, "").GetConsumerPriceIndexAsync(Ctx, CancellationToken.None);

			Assert.Null(result);
		}

		[Fact]
		public async Task GetConsumerPriceIndexAsync_ReturnsNull_WhenNoRowsCanBeParsed()
		{
			var result = await CreateClient(HttpStatusCode.OK, "TID;INDHOLD\r\n").GetConsumerPriceIndexAsync(Ctx, CancellationToken.None);

			Assert.Null(result);
		}
	}
}
