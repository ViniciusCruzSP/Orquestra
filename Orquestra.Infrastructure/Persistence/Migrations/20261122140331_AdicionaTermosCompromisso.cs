using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orquestra.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTermosCompromisso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TermosCompromisso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodiaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Pdf = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermosCompromisso", x => x.Id);
                    table.UniqueConstraint("AK_TermosCompromisso_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_TermosCompromisso_Custodies_OrganizationId_CustodiaId",
                        columns: x => new { x.OrganizationId, x.CustodiaId },
                        principalTable: "Custodies",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermosCompromisso_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TermosCompromisso_OrganizationId_CustodiaId",
                table: "TermosCompromisso",
                columns: new[] { "OrganizationId", "CustodiaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TermosCompromisso");
        }
    }
}
