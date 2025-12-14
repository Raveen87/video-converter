using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameToManualResolutionQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old unique index before renaming table
            migrationBuilder.DropIndex(
                name: "IX_MetadataResolutionQueue_SourcePath_Status",
                table: "MetadataResolutionQueue");

            // Rename the table
            migrationBuilder.RenameTable(
                name: "MetadataResolutionQueue",
                newName: "ManualResolutionQueue");

            // Add new column for resolution type (default to 'Metadata' for existing rows)
            migrationBuilder.AddColumn<string>(
                name: "ResolutionType",
                table: "ManualResolutionQueue",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Metadata");

            // Add new columns for stream selection
            migrationBuilder.AddColumn<string>(
                name: "AvailableAudioStreamsJson",
                table: "ManualResolutionQueue",
                type: "TEXT",
                maxLength: 8192,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailableSubtitleStreamsJson",
                table: "ManualResolutionQueue",
                type: "TEXT",
                maxLength: 8192,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingAudioLanguages",
                table: "ManualResolutionQueue",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingSubtitleLanguages",
                table: "ManualResolutionQueue",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            // Create new unique index including ResolutionType
            migrationBuilder.CreateIndex(
                name: "IX_ManualResolutionQueue_SourcePath_ResolutionType_Status",
                table: "ManualResolutionQueue",
                columns: new[] { "SourcePath", "ResolutionType", "Status" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new unique index
            migrationBuilder.DropIndex(
                name: "IX_ManualResolutionQueue_SourcePath_ResolutionType_Status",
                table: "ManualResolutionQueue");

            // Remove new columns
            migrationBuilder.DropColumn(
                name: "ResolutionType",
                table: "ManualResolutionQueue");

            migrationBuilder.DropColumn(
                name: "AvailableAudioStreamsJson",
                table: "ManualResolutionQueue");

            migrationBuilder.DropColumn(
                name: "AvailableSubtitleStreamsJson",
                table: "ManualResolutionQueue");

            migrationBuilder.DropColumn(
                name: "MissingAudioLanguages",
                table: "ManualResolutionQueue");

            migrationBuilder.DropColumn(
                name: "MissingSubtitleLanguages",
                table: "ManualResolutionQueue");

            // Rename the table back
            migrationBuilder.RenameTable(
                name: "ManualResolutionQueue",
                newName: "MetadataResolutionQueue");

            // Recreate the old unique index
            migrationBuilder.CreateIndex(
                name: "IX_MetadataResolutionQueue_SourcePath_Status",
                table: "MetadataResolutionQueue",
                columns: new[] { "SourcePath", "Status" },
                unique: true);
        }
    }
}
