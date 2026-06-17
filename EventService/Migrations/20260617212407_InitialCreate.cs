using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VrsteDogadjaja",
                columns: table => new
                {
                    VrstaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VrsteDogadjaja", x => x.VrstaId);
                });

            migrationBuilder.CreateTable(
                name: "Dogadjaji",
                columns: table => new
                {
                    DogadjajId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NazivDogadjaja = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Trajanje = table.Column<double>(type: "float", nullable: false),
                    Cena = table.Column<double>(type: "float", nullable: false),
                    LokacijaId = table.Column<int>(type: "int", nullable: false),
                    VrstaId = table.Column<int>(type: "int", nullable: false),
                    VrstaDogadjajaVrstaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dogadjaji", x => x.DogadjajId);
                    table.ForeignKey(
                        name: "FK_Dogadjaji_VrsteDogadjaja_VrstaDogadjajaVrstaId",
                        column: x => x.VrstaDogadjajaVrstaId,
                        principalTable: "VrsteDogadjaja",
                        principalColumn: "VrstaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dogadjaji_VrstaDogadjajaVrstaId",
                table: "Dogadjaji",
                column: "VrstaDogadjajaVrstaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dogadjaji");

            migrationBuilder.DropTable(
                name: "VrsteDogadjaja");
        }
    }
}
