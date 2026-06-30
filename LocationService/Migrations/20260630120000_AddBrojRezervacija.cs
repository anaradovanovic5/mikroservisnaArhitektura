using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocationService.Migrations
{
    /// <inheritdoc />
    public partial class AddBrojRezervacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BrojRezervacija",
                table: "Lokacije",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrojRezervacija",
                table: "Lokacije");
        }
    }
}