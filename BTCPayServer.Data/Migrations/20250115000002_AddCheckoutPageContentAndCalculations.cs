using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutPageContentAndCalculations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create CheckoutPageContent table for static page content
            migrationBuilder.CreateTable(
                name: "CheckoutPageContent",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PageKey = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutPageContent", x => x.Id);
                });

            // Create ProviderStepCalculations table for dynamic calculations
            migrationBuilder.CreateTable(
                name: "ProviderStepCalculations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    CalculationType = table.Column<string>(type: "text", nullable: false),
                    CalculationFormula = table.Column<string>(type: "text", nullable: false),
                    DisplayFormat = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderStepCalculations", x => x.Id);
                });

            // Create indexes for performance
            migrationBuilder.CreateIndex(
                name: "IX_CheckoutPageContent_PageKey_Language",
                table: "CheckoutPageContent",
                columns: new[] { "PageKey", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderStepCalculations_ProviderName_StepNumber",
                table: "ProviderStepCalculations",
                columns: new[] { "ProviderName", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderStepCalculations");

            migrationBuilder.DropTable(
                name: "CheckoutPageContent");
        }
    }
}