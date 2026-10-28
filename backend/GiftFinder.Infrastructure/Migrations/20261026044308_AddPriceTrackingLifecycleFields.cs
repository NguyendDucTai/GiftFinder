using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftFinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceTrackingLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "PriceTrackings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "PriceTrackings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "PriceTrackings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PriceTrackings");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "PriceTrackings");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "PriceTrackings");
        }
    }
}
