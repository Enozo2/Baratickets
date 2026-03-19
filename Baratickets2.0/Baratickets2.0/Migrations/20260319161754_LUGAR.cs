using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class LUGAR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LugarId",
                table: "Eventos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Lugar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lugar", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_LugarId",
                table: "Eventos",
                column: "LugarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Lugar_LugarId",
                table: "Eventos",
                column: "LugarId",
                principalTable: "Lugar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Lugar_LugarId",
                table: "Eventos");

            migrationBuilder.DropTable(
                name: "Lugar");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_LugarId",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "LugarId",
                table: "Eventos");
        }
    }
}
