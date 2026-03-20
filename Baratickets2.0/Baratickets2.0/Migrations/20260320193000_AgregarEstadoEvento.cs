using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoEvento",
                table: "Eventos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MotivoRechazo",
                table: "Eventos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoEvento",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "MotivoRechazo",
                table: "Eventos");
        }
    }
}
