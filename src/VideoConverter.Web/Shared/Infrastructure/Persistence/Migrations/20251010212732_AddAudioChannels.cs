using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Web.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class AddAudioChannels : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<int>(
            name: "AudioChannels",
            table: "Conversions",
            type: "INTEGER",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "AudioChannels",
            table: "Conversions");
    }
}
