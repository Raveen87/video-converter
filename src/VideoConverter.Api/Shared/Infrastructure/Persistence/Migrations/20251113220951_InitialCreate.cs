using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
#pragma warning disable MA0051 // Method is too long
#pragma warning disable MA0003 // Name the parameter to improve code readability
#pragma warning disable IDE0300 // Collection initialization can be simplified
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
                    .Annotation("Sqlite:Autoincrement", true),
                InputPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                OutputDirectory = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Rank = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                Crf = table.Column<int>(type: "INTEGER", nullable: true),
                AudioBitrateKbps = table.Column<int>(type: "INTEGER", nullable: true),
                AudioChannels = table.Column<int>(type: "INTEGER", nullable: true),
                Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                SelectedAudioStreamIndicesCsv = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                SelectedSubtitleStreamIndicesCsv = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                SelectedExternalSubtitlePathsCsv = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                DetectedCrop = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                MetadataBasedFilename = table.Column<string>(type: "TEXT", nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_Conversions", x => x.Id);
            });

        _ = migrationBuilder.CreateTable(
            name: "DirectoryQueueBatches",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TotalItems = table.Column<int>(type: "INTEGER", nullable: false),
                ProcessedItems = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentItemPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                ClaimedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                ProfileId = table.Column<int>(type: "INTEGER", nullable: true),
                RequestJson = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_DirectoryQueueBatches", x => x.Id);
            });

        _ = migrationBuilder.CreateTable(
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
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_MetadataResolutionQueue", x => x.Id);
            });

        _ = migrationBuilder.CreateTable(
            name: "Profiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OutputDirectory = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                Crf = table.Column<int>(type: "INTEGER", nullable: true),
                AudioBitrateKbps = table.Column<int>(type: "INTEGER", nullable: true),
                AudioChannels = table.Column<int>(type: "INTEGER", nullable: true),
                Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                MetadataBasedFilename = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                OutputFilenameTemplate = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                AudioLanguages = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                SubtitleLanguages = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_Profiles", x => x.Id);
            });

        _ = migrationBuilder.CreateTable(
            name: "DirectoryQueueBatchItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                BatchId = table.Column<int>(type: "INTEGER", nullable: false),
                InputPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Reason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                ConversionId = table.Column<int>(type: "INTEGER", nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_DirectoryQueueBatchItems", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_DirectoryQueueBatchItems_DirectoryQueueBatches_BatchId",
                    column: x => x.BatchId,
                    principalTable: "DirectoryQueueBatches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "IX_Conversions_Status_Rank",
            table: "Conversions",
            columns: new[] { "Status", "Rank" });

        _ = migrationBuilder.CreateIndex(
            name: "IX_DirectoryQueueBatches_Status_ClaimedAtUtc",
            table: "DirectoryQueueBatches",
            columns: new[] { "Status", "ClaimedAtUtc" });

        _ = migrationBuilder.CreateIndex(
            name: "IX_DirectoryQueueBatchItems_BatchId_Status_Id",
            table: "DirectoryQueueBatchItems",
            columns: new[] { "BatchId", "Status", "Id" });

        _ = migrationBuilder.CreateIndex(
            name: "IX_MetadataResolutionQueue_SourcePath_Status",
            table: "MetadataResolutionQueue",
            columns: new[] { "SourcePath", "Status" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "Conversions");

        _ = migrationBuilder.DropTable(
            name: "DirectoryQueueBatchItems");

        _ = migrationBuilder.DropTable(
            name: "MetadataResolutionQueue");

        _ = migrationBuilder.DropTable(
            name: "Profiles");

        _ = migrationBuilder.DropTable(
            name: "DirectoryQueueBatches");
    }
}
