using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtDetailsAndAmenities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Courts",
                newName: "SurfaceType");

            migrationBuilder.AddColumn<string>(
                name: "AddressDetail",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Amenities",
                table: "Courts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RentalOptionsJson",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SportType",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressDetail",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "RentalOptionsJson",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "SportType",
                table: "Courts");

            migrationBuilder.RenameColumn(
                name: "SurfaceType",
                table: "Courts",
                newName: "Type");
        }
    }
}
