using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class mefaltouno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Lugar_LugarId",
                table: "Eventos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lugar",
                table: "Lugar");

            migrationBuilder.RenameTable(
                name: "Lugar",
                newName: "Lugares");

            migrationBuilder.AddColumn<bool>(
                name: "EstaActivo",
                table: "Lugares",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lugares",
                table: "Lugares",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Lugares_LugarId",
                table: "Eventos",
                column: "LugarId",
                principalTable: "Lugares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Lugares_LugarId",
                table: "Eventos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lugares",
                table: "Lugares");

            migrationBuilder.DropColumn(
                name: "EstaActivo",
                table: "Lugares");

            migrationBuilder.RenameTable(
                name: "Lugares",
                newName: "Lugar");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lugar",
                table: "Lugar",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Lugar_LugarId",
                table: "Eventos",
                column: "LugarId",
                principalTable: "Lugar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
