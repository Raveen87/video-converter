using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVmafFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VmafEnabled",
                table: "Profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VmafEnabled",
                table: "Conversions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "VmafScore",
                table: "Conversions",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VmafEnabled",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "VmafEnabled",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "VmafScore",
                table: "Conversions");
        }
    }
}
