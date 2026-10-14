using System.Text.Json;
using StockPriceSheetPrintService.Outbound.Dto.Saxo.Transactions;
using StockPriceSheetPrintService.Outbound.Mappers;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SaxoMapperTransferTests
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

		[Fact]
		public void ToTransfers_MapsBookedTransferIdAmountAndDate()
		{
			var dto = JsonSerializer.Deserialize<SaxoTransactionsResponse>("""
				{
					"Data": [
						{
							"BookingId": "60247672468",
							"PositionId": "7765187990",
							"BookedAmount": 5000.0,
							"IsIntradayData": false,
							"Date": "2026-10-02"
						}
					]
				}
				""", JsonOptions)!;

			var transfer = Assert.Single(SaxoMapper.ToTransfers(dto));

			Assert.Equal("60247672468", transfer.Id);
			Assert.Equal(5000m, transfer.Amount);
			Assert.Equal(new DateOnly(2026, 10, 2), transfer.Date);
		}

		[Fact]
		public void ToTransfers_SkipsIntradayTransactions()
		{
			var dto = JsonSerializer.Deserialize<SaxoTransactionsResponse>("""
				{
					"Data": [
						{
							"BookingId": "7765187990",
							"IntradayAmount": 5000.0,
							"IsIntradayData": true,
							"Date": "2026-10-02"
						}
					]
				}
				""", JsonOptions)!;

			Assert.Empty(SaxoMapper.ToTransfers(dto));
		}
	}
}
