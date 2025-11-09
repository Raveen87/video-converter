using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Web.Shared.Infrastructure.Persistence.Migrations;

#pragma warning disable MA0048 // Migration files should start with date prefix
/// <inheritdoc />
public partial class AddDirectoryBatches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.CreateTable(
            name: "DirectoryQueueBatches",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", value: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TotalItems = table.Column<int>(type: "INTEGER", nullable: false),
                ProcessedItems = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentItemPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                ProfileId = table.Column<int>(type: "INTEGER", nullable: true),
                RequestJson = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: true),
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_DirectoryQueueBatches", x => x.Id);
            });

        _ = migrationBuilder.CreateTable(
            name: "DirectoryQueueBatchItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", value: true),
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
            name: "IX_DirectoryQueueBatchItems_BatchId_Status_Id",
            table: "DirectoryQueueBatchItems",
            columns: ["BatchId", "Status", "Id"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "DirectoryQueueBatchItems");

        _ = migrationBuilder.DropTable(
            name: "DirectoryQueueBatches");
    }
}
