using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMetadataBasedFilenameFromProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetadataBasedFilename",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetadataBasedFilename",
                table: "Profiles",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);
        }
    }
}
