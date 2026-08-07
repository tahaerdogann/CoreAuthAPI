using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderToCourtPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "CourtPhotos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "CourtPhotos");
        }
    }
}
