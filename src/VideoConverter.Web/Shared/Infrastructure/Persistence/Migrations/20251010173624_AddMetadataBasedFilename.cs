using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Web.Migrations;

/// <inheritdoc />
public partial class AddMetadataBasedFilename : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<string>(
            name: "MetadataBasedFilename",
            table: "Conversions",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "MetadataBasedFilename",
            table: "Conversions");
    }
}
