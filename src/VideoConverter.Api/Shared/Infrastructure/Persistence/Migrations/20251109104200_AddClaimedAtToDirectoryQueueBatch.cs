using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class AddClaimedAtToDirectoryQueueBatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<DateTime?>(
            name: "ClaimedAtUtc",
            table: "DirectoryQueueBatches",
            type: "TEXT",
            nullable: true);

        _ = migrationBuilder.CreateIndex(
            name: "IX_DirectoryQueueBatches_Status_ClaimedAtUtc",
            table: "DirectoryQueueBatches",
            columns: ["Status", "ClaimedAtUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropIndex(
            name: "IX_DirectoryQueueBatches_Status_ClaimedAtUtc",
            table: "DirectoryQueueBatches");

        _ = migrationBuilder.DropColumn(
            name: "ClaimedAtUtc",
            table: "DirectoryQueueBatches");
    }
}
