using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPriceSheetPrintService.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountToSeenTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "SeenTransfers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "SeenTransfers");
        }
    }
}
