using StockPriceSheetPrintService.Outbound.Dto.Saxo;
using StockPriceSheetPrintService.Outbound.Dto.Saxo.InstrumentDetails;
using StockPriceSheetPrintService.Outbound.Dto.Saxo.Transactions;
using StockPriceSheetPrintService.Service.Models;

namespace StockPriceSheetPrintService.Outbound.Mappers
{
	public static class SaxoMapper
	{
		public static OAuthTokens ToOAuthTokens(SaxoTokenResult dto) =>
			new(dto.AccessToken, dto.RefreshToken);

		public static AccountBalance ToAccountBalance(SaxoBalanceResponse dto) =>
			new(dto.TotalValue, dto.CashBalance, dto.Currency, dto.CalculationAssetValue);

		public static Transfer ToTransfer(SaxoTransaction dto) =>
			new(dto.BookingId, dto.Amount, dto.Date);

		// Intraday-transaktioner springes over: Saxo dokumenterer, at identifikatorer ikke er tilgængelige før
		// transaktionen er bogført, og at overførslen får et andet BookingId når den bogføres – så den ville blive
		// meldt to gange. Kun bogførte overførsler har det endelige BookingId.
		public static List<Transfer> ToTransfers(SaxoTransactionsResponse dto) =>
			dto.Data.Where(t => !t.IsIntradayData).Select(ToTransfer).ToList();

		public static Instrument ToInstrument(SaxoInstrument dto) => new()
		{
			Uic            = dto.Uic,
			Symbol         = dto.Symbol,
			Description    = dto.Description,
			AssetType      = dto.AssetType,
			Currency       = dto.CurrencyCode,
			ExchangeId      = dto.Exchange?.ExchangeId ?? string.Empty,
			ExchangeCountry = dto.Exchange?.CountryCode ?? string.Empty,
			ExchangeName    = dto.Exchange?.Name ?? string.Empty,
		};

		public static SaxoInstrument ToDto(Instrument domain) => new()
		{
			Uic          = domain.Uic,
			Symbol       = domain.Symbol,
			Description  = domain.Description,
			AssetType    = domain.AssetType,
			CurrencyCode = domain.Currency,
			Exchange     = new ExchangeDto
			{
				ExchangeId  = domain.ExchangeId,
				CountryCode = domain.ExchangeCountry,
				Name        = domain.ExchangeName,
			},
		};
	}
}
