using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIMPE.Dashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    id_equipo = table.Column<string>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    ip = table.Column<string>(type: "TEXT", nullable: false),
                    usuario = table.Column<string>(type: "TEXT", nullable: false),
                    cpu_model = table.Column<string>(type: "TEXT", nullable: false),
                    ram_total = table.Column<double>(type: "REAL", nullable: false),
                    disco_tipo = table.Column<string>(type: "TEXT", nullable: false),
                    os_version = table.Column<string>(type: "TEXT", nullable: false),
                    antivirus_nombre = table.Column<string>(type: "TEXT", nullable: false),
                    tiempo_arranque = table.Column<string>(type: "TEXT", nullable: false),
                    tiempo_apagado = table.Column<string>(type: "TEXT", nullable: false),
                    hardware_detalles = table.Column<string>(type: "TEXT", nullable: false),
                    estado_seguridad = table.Column<string>(type: "TEXT", nullable: false),
                    ultima_actualizacion = table.Column<string>(type: "TEXT", nullable: false),
                    synced = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.id_equipo);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
