using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class AddProfiles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.CreateTable(
            name: "Profiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", value: true),
                OutputDirectory = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                Crf = table.Column<int>(type: "INTEGER", nullable: true),
                AudioBitrateKbps = table.Column<int>(type: "INTEGER", nullable: true),
                AudioChannels = table.Column<int>(type: "INTEGER", nullable: true),
                Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                MetadataBasedFilename = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_Profiles", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "Profiles");
    }
}
