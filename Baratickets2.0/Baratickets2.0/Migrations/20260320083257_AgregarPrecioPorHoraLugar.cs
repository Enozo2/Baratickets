using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPrecioPorHoraLugar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioPorHora",
                table: "Lugares",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecioPorHora",
                table: "Lugares");
        }
    }
}
