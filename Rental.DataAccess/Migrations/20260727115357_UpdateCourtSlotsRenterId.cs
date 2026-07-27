using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourtSlotsRenterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourtSlots_Courts_CourtId",
                table: "CourtSlots");

            migrationBuilder.DropIndex(
                name: "IX_CourtSlots_CourtId",
                table: "CourtSlots");

            migrationBuilder.DropColumn(
                name: "RecordDate",
                table: "CourtSlots");

            migrationBuilder.DropColumn(
                name: "RecordUserCode",
                table: "CourtSlots");

            migrationBuilder.AddColumn<int>(
                name: "RenterId",
                table: "CourtSlots",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenterId",
                table: "CourtSlots");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordDate",
                table: "CourtSlots",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RecordUserCode",
                table: "CourtSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CourtSlots_CourtId",
                table: "CourtSlots",
                column: "CourtId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourtSlots_Courts_CourtId",
                table: "CourtSlots",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
