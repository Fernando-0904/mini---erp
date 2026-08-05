using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaEventos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EntidadeId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuarioEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Dados = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DataUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaEventos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEventos_DataUtc",
                table: "AuditoriaEventos",
                column: "DataUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaEventos_Entidade_EntidadeId",
                table: "AuditoriaEventos",
                columns: new[] { "Entidade", "EntidadeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaEventos");
        }
    }
}
