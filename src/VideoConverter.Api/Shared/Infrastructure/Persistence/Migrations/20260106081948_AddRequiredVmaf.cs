using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredVmaf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RequiredVmaf",
                table: "Profiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RequiredVmaf",
                table: "Conversions",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredVmaf",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "RequiredVmaf",
                table: "Conversions");
        }
    }
}
