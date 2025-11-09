// Suppress analyzer/style warnings for EF Core migration scaffolding
#pragma warning disable IDE0161 // Convert to file-scoped namespace
#pragma warning disable MA0048  // File name must match type name
#pragma warning disable SA1516  // Using directives should be separated by blank line
#pragma warning disable MA0007  // Add trailing comma
#pragma warning disable MA0003  // Name the parameter to improve readability
#pragma warning disable IDE0058 // Expression value is never used
#pragma warning disable SA1413  // Use trailing comma in multi-line initializers
#pragma warning disable IDE0300 // Collection initialization can be simplified
#pragma warning disable IDE0005 // Using directive is unnecessary
#pragma warning disable IDE0055 // Fix formatting
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataResolutionQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rank",
                table: "Conversions",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Conversions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MetadataResolutionQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourcePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    InputKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ParsedTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ParsedYear = table.Column<int>(type: "INTEGER", nullable: true),
                    ParsedSeason = table.Column<int>(type: "INTEGER", nullable: true),
                    ParsedEpisode = table.Column<int>(type: "INTEGER", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    FailureDetails = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataResolutionQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversions_Status_Rank",
                table: "Conversions",
                columns: new[] { "Status", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataResolutionQueue_SourcePath_Status",
                table: "MetadataResolutionQueue",
                columns: new[] { "SourcePath", "Status" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetadataResolutionQueue");

            migrationBuilder.DropIndex(
                name: "IX_Conversions_Status_Rank",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Conversions");
        }
    }
}

#pragma warning restore IDE0055
#pragma warning restore IDE0005
#pragma warning restore IDE0300
#pragma warning restore SA1413
#pragma warning restore IDE0058
#pragma warning restore MA0003
#pragma warning restore MA0007
#pragma warning restore SA1516
#pragma warning restore MA0048
#pragma warning restore IDE0161
