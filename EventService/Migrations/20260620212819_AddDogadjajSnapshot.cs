using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Migrations
{
    /// <inheritdoc />
    public partial class AddDogadjajSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Otkazan",
                table: "Dogadjaji",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DogadjajSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateId = table.Column<int>(type: "int", nullable: false),
                    NazivDogadjaja = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Trajanje = table.Column<double>(type: "float", nullable: false),
                    Cena = table.Column<double>(type: "float", nullable: false),
                    LokacijaId = table.Column<int>(type: "int", nullable: false),
                    VrstaId = table.Column<int>(type: "int", nullable: false),
                    Otkazan = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogadjajSnapshots", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogadjajSnapshots");

            migrationBuilder.DropColumn(
                name: "Otkazan",
                table: "Dogadjaji");
        }
    }
}
