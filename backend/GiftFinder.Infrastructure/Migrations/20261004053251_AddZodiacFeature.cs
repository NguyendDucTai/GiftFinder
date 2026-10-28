using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftFinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZodiacFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipientZodiac",
                table: "ReminderDates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "SuitableZodiacs",
                table: "Products",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientZodiac",
                table: "ReminderDates");

            migrationBuilder.DropColumn(
                name: "SuitableZodiacs",
                table: "Products");
        }
    }
}
