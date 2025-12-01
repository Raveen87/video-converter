using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchedEpisode",
                table: "Conversions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchedSeason",
                table: "Conversions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchedTitle",
                table: "Conversions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchedYear",
                table: "Conversions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Conversions",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchedEpisode",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "MatchedSeason",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "MatchedTitle",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "MatchedYear",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Conversions");
        }
    }
}
