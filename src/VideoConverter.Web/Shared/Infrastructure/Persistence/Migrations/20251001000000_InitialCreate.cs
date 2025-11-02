using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Web.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.CreateTable(
            name: "Conversions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", value: true),
                InputPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                OutputDirectory = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Crf = table.Column<int>(type: "INTEGER", nullable: true),
                AudioBitrateKbps = table.Column<int>(type: "INTEGER", nullable: true),
                Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                SelectedAudioStreamIndicesCsv = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                SelectedSubtitleStreamIndicesCsv = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                SelectedExternalSubtitlePathsCsv = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                DetectedCrop = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_Conversions", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "Conversions");
    }
}
