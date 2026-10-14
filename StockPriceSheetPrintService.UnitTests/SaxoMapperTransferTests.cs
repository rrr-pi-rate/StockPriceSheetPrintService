using System.Text.Json;
using StockPriceSheetPrintService.Outbound.Dto.Saxo.Transactions;
using StockPriceSheetPrintService.Outbound.Mappers;

namespace StockPriceSheetPrintService.UnitTests
{
	public class SaxoMapperTransferTests
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

		[Fact]
		public void ToTransfer_UsesPositionIdAsId_AndMapsAmountAndDate()
		{
			var dto = JsonSerializer.Deserialize<SaxoTransaction>("""
				{
					"BookingId": "60247672468",
					"PositionId": "7765187990",
					"BookedAmount": 5000.0,
					"IsIntradayData": false,
					"Date": "2026-10-02"
				}
				""", JsonOptions)!;

			var transfer = SaxoMapper.ToTransfer(dto);

			Assert.Equal("7765187990", transfer.Id);
			Assert.Equal(5000m, transfer.Amount);
			Assert.Equal(new DateOnly(2026, 10, 2), transfer.Date);
		}

		[Fact]
		public void ToTransfer_FallsBackToBookingId_WhenPositionIdIsMissing()
		{
			var dto = JsonSerializer.Deserialize<SaxoTransaction>("""
				{
					"BookingId": "7765187990",
					"IntradayAmount": 5000.0,
					"IsIntradayData": true,
					"Date": "2026-10-02"
				}
				""", JsonOptions)!;

			var transfer = SaxoMapper.ToTransfer(dto);

			Assert.Equal("7765187990", transfer.Id);
			Assert.Equal(5000m, transfer.Amount);
		}
	}
}
