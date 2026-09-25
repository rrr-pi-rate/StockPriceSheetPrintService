namespace StockPriceSheetPrintService.Inbound.Dto
{
	public record MessageResponseDto(string Message);
	public record CallbackSuccessResponseDto(string Message, string NextRunTime);
	public record AccessTokenResponseDto(string AccessToken);
}
