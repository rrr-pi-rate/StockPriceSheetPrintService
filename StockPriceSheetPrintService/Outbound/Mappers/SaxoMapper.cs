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

		private const string IntraClientTransfer = "IntraClientTransfer";

		// Også foreløbige (intraday) transaktioner medtages: de ligger allerede i den live Saxo-saldo. Deres BookingId
		// er ikke endeligt (overførslen får et andet id ved bogføring), så dedup sker på rækkens dato, ikke på id alene.
		// Overførsler mellem klientens egne konti springes over: de har et +ben og et −ben, men ingen penge kom udefra.
		public static List<Transfer> ToTransfers(SaxoTransactionsResponse dto) =>
			dto.Data.Where(t => t.FundingSubType != IntraClientTransfer).Select(ToTransfer).ToList();

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
