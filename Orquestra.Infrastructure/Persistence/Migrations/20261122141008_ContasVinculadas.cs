using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orquestra.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContasVinculadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Employees",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ColaboradorId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrganizationId_Cpf",
                table: "Employees",
                columns: new[] { "OrganizationId", "Cpf" },
                unique: true,
                filter: "[Cpf] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId_ColaboradorId",
                table: "AspNetUsers",
                columns: new[] { "OrganizationId", "ColaboradorId" },
                unique: true,
                filter: "[ColaboradorId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Employees_OrganizationId_ColaboradorId",
                table: "AspNetUsers",
                columns: new[] { "OrganizationId", "ColaboradorId" },
                principalTable: "Employees",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Employees_OrganizationId_ColaboradorId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Employees_OrganizationId_Cpf",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId_ColaboradorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ColaboradorId",
                table: "AspNetUsers");
        }
    }
}
