using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistrationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Predavaci",
                columns: table => new
                {
                    PredavacId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Titula = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Oblast = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predavaci", x => x.PredavacId);
                });

            migrationBuilder.CreateTable(
                name: "Prijave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImeUcesnika = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrezimeUcesnika = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailUcesnika = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatumPrijave = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DogadjajId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prijave", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DogadjajPredavaci",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatumIVreme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DogadjajId = table.Column<int>(type: "int", nullable: false),
                    PredavacId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogadjajPredavaci", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DogadjajPredavaci_Predavaci_PredavacId",
                        column: x => x.PredavacId,
                        principalTable: "Predavaci",
                        principalColumn: "PredavacId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogadjajPredavaci_PredavacId",
                table: "DogadjajPredavaci",
                column: "PredavacId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogadjajPredavaci");

            migrationBuilder.DropTable(
                name: "Prijave");

            migrationBuilder.DropTable(
                name: "Predavaci");
        }
    }
}
