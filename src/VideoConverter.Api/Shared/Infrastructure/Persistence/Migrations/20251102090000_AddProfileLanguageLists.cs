using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class AddProfileLanguageLists : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<string>(
            name: "AudioLanguages",
            table: "Profiles",
            type: "TEXT",
            nullable: true);

        _ = migrationBuilder.AddColumn<string>(
            name: "SubtitleLanguages",
            table: "Profiles",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "AudioLanguages",
            table: "Profiles");

        _ = migrationBuilder.DropColumn(
            name: "SubtitleLanguages",
            table: "Profiles");
    }
}
