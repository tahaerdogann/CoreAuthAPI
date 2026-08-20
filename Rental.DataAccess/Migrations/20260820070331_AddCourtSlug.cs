using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rental.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Courts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Courts SET Slug = CAST(Id AS NVARCHAR(36)) WHERE Slug = ''");

            migrationBuilder.CreateIndex(
                name: "IX_Courts_Slug",
                table: "Courts",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courts_Slug",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Courts");
        }
    }
}
