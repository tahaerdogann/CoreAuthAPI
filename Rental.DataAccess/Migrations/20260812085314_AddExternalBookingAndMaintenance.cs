using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalBookingAndMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_CustomerId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CourtSlots",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceNote",
                table: "CourtSlots",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE CourtSlots SET Status = 2 WHERE IsBooked = 1");

            migrationBuilder.DropColumn(
                name: "IsBooked",
                table: "CourtSlots");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ExternalCustomerName",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCustomerPhone",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_CustomerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "MaintenanceNote",
                table: "CourtSlots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CourtSlots");

            migrationBuilder.DropColumn(
                name: "ExternalCustomerName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExternalCustomerPhone",
                table: "Bookings");

            migrationBuilder.AddColumn<bool>(
                name: "IsBooked",
                table: "CourtSlots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
