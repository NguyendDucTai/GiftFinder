using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiftFinder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftPollEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiftPolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShareCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftPolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftPolls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftPollItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GiftPollId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftPollItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftPollItems_GiftPolls_GiftPollId",
                        column: x => x.GiftPollId,
                        principalTable: "GiftPolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiftPollItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftPollVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GiftPollId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftPollVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftPollVotes_GiftPolls_GiftPollId",
                        column: x => x.GiftPollId,
                        principalTable: "GiftPolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiftPollVotes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiftPollItems_GiftPollId_ProductId",
                table: "GiftPollItems",
                columns: new[] { "GiftPollId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_GiftPollItems_ProductId",
                table: "GiftPollItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftPolls_ShareCode",
                table: "GiftPolls",
                column: "ShareCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftPolls_UserId",
                table: "GiftPolls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftPollVotes_GiftPollId_IpAddress",
                table: "GiftPollVotes",
                columns: new[] { "GiftPollId", "IpAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftPollVotes_ProductId",
                table: "GiftPollVotes",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiftPollItems");

            migrationBuilder.DropTable(
                name: "GiftPollVotes");

            migrationBuilder.DropTable(
                name: "GiftPolls");
        }
    }
}
