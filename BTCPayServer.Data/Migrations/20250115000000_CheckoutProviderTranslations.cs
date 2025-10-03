using System;
using BTCPayServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250115000000_CheckoutProviderTranslations")]
    public partial class CheckoutProviderTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckoutProviderTranslations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    IntroText = table.Column<string>(type: "text", nullable: true),
                    OutroText = table.Column<string>(type: "text", nullable: true),
                    Steps = table.Column<string>(type: "jsonb", nullable: true),
                    EnabledCountries = table.Column<string>(type: "jsonb", nullable: true),
                    IconClass = table.Column<string>(type: "text", nullable: true),
                    ButtonClass = table.Column<string>(type: "text", nullable: true),
                    FeeText = table.Column<string>(type: "text", nullable: true),
                    BadgeIcon = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutProviderTranslations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutProviderTranslations_ProviderName",
                table: "CheckoutProviderTranslations",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutProviderTranslations_Language",
                table: "CheckoutProviderTranslations",
                column: "Language");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckoutProviderTranslations");
        }
    }
}