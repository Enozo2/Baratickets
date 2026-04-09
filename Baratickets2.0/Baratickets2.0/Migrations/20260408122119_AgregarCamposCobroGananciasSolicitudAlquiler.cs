using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposCobroGananciasSolicitudAlquiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCobroGanancias",
                table: "SolicitudesAlquiler",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GananciasCobradas",
                table: "SolicitudesAlquiler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoGananciasCobradas",
                table: "SolicitudesAlquiler",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCobroGanancias",
                table: "SolicitudesAlquiler");

            migrationBuilder.DropColumn(
                name: "GananciasCobradas",
                table: "SolicitudesAlquiler");

            migrationBuilder.DropColumn(
                name: "MontoGananciasCobradas",
                table: "SolicitudesAlquiler");
        }
    }
}
