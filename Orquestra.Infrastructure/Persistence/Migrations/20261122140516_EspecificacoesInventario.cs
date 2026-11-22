using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orquestra.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EspecificacoesInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AdquiridoEm",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Armazenamento",
                table: "Assets",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemoriaRam",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processador",
                table: "Assets",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SistemaOperacional",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Assets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoPeriferico",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdquiridoEm",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Armazenamento",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "MemoriaRam",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Processador",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "SistemaOperacional",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "TipoPeriferico",
                table: "Assets");
        }
    }
}
