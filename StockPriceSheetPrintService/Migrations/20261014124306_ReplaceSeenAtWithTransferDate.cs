using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPriceSheetPrintService.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSeenAtWithTransferDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeenAt",
                table: "SeenTransfers");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "SeenTransfers",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "SeenTransfers");

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "SeenTransfers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
