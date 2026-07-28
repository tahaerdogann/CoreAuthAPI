using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoScheduleEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoScheduleEnabled",
                table: "CourtSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoScheduleEnabled",
                table: "CourtSchedules");
        }
    }
}
