using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoEventoAlquilerYCuentaGanancias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CuentaGanancias",
                table: "SolicitudesAlquiler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoEventoAlquiler",
                table: "SolicitudesAlquiler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuentaGanancias",
                table: "SolicitudesAlquiler");

            migrationBuilder.DropColumn(
                name: "TipoEventoAlquiler",
                table: "SolicitudesAlquiler");
        }
    }
}
