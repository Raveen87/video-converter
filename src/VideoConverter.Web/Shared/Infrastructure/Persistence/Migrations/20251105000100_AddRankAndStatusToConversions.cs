using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Web.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
public partial class AddRankAndStatusToConversions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<string>(
            name: "Rank",
            table: "Conversions",
            type: "TEXT",
            maxLength: 64,
            nullable: true);

        _ = migrationBuilder.AddColumn<int>(
            name: "Status",
            table: "Conversions",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        _ = migrationBuilder.CreateIndex(
            name: "IX_Conversions_Status_Rank",
            table: "Conversions",
            columns: ["Status", "Rank"]);

        // Backfill moved to application startup to satisfy analyzer constraints
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropIndex(
            name: "IX_Conversions_Status_Rank",
            table: "Conversions");

        _ = migrationBuilder.DropColumn(
            name: "Rank",
            table: "Conversions");

        _ = migrationBuilder.DropColumn(
            name: "Status",
            table: "Conversions");
    }
}
