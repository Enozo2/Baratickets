using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baratickets2._0.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConceptoOrdenYRelacionAlquiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrdenId",
                table: "SolicitudesAlquiler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Concepto",
                table: "Ordenes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaReembolso",
                table: "Ordenes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Reembolsado",
                table: "Ordenes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAlquiler_OrdenId",
                table: "SolicitudesAlquiler",
                column: "OrdenId");

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesAlquiler_Ordenes_OrdenId",
                table: "SolicitudesAlquiler",
                column: "OrdenId",
                principalTable: "Ordenes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesAlquiler_Ordenes_OrdenId",
                table: "SolicitudesAlquiler");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesAlquiler_OrdenId",
                table: "SolicitudesAlquiler");

            migrationBuilder.DropColumn(
                name: "OrdenId",
                table: "SolicitudesAlquiler");

            migrationBuilder.DropColumn(
                name: "Concepto",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "FechaReembolso",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "Reembolsado",
                table: "Ordenes");
        }
    }
}
