using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedRulesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvancedRulesJson",
                table: "CourtSchedules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvancedRulesJson",
                table: "CourtSchedules");
        }
    }
}
