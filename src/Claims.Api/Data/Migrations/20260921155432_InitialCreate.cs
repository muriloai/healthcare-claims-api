using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Claims.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beneficiarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeFicticio = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Carteirinha = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    InicioVigencia = table.Column<string>(type: "TEXT", nullable: false),
                    FimCarencia = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prestadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeFicticio = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CredenciadoAtivo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuiasConsulta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BeneficiarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrestadorId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataAtendimento = table.Column<string>(type: "TEXT", nullable: false),
                    LoteId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuiasConsulta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuiasConsulta_Beneficiarios_BeneficiarioId",
                        column: x => x.BeneficiarioId,
                        principalTable: "Beneficiarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuiasConsulta_Lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "Lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GuiasConsulta_Prestadores_PrestadorId",
                        column: x => x.PrestadorId,
                        principalTable: "Prestadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiarios_Carteirinha",
                table: "Beneficiarios",
                column: "Carteirinha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuiasConsulta_BeneficiarioId",
                table: "GuiasConsulta",
                column: "BeneficiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasConsulta_LoteId",
                table: "GuiasConsulta",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasConsulta_PrestadorId",
                table: "GuiasConsulta",
                column: "PrestadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuiasConsulta");

            migrationBuilder.DropTable(
                name: "Beneficiarios");

            migrationBuilder.DropTable(
                name: "Lotes");

            migrationBuilder.DropTable(
                name: "Prestadores");
        }
    }
}
