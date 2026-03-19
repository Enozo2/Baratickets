using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class cuponusado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_TicketId",
                table: "Devoluciones");

            migrationBuilder.AddColumn<bool>(
                name: "CuponUsado",
                table: "Devoluciones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_TicketId",
                table: "Devoluciones",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devoluciones_TicketId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "CuponUsado",
                table: "Devoluciones");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_TicketId",
                table: "Devoluciones",
                column: "TicketId");
        }
    }
}
