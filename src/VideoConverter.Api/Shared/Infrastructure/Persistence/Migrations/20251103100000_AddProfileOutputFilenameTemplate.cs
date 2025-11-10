using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class AddProfileOutputFilenameTemplate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<string>(
            name: "OutputFilenameTemplate",
            table: "Profiles",
            type: "TEXT",
            maxLength: 1024,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "OutputFilenameTemplate",
            table: "Profiles");
    }
}
