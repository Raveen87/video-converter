using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutputPathTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutputPathTemplate",
                table: "Profiles",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputPathTemplate",
                table: "Conversions",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputPathTemplate",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "OutputPathTemplate",
                table: "Conversions");
        }
    }
}
