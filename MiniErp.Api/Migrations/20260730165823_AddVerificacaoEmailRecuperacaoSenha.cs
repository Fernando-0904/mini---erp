using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificacaoEmailRecuperacaoSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmado",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmadoEmUtc",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE Usuarios SET EmailConfirmado = 1, EmailConfirmadoEmUtc = CriadoEmUtc");

            migrationBuilder.CreateTable(
                name: "TokensUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TokenHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiraEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmailConfirmado", "EmailConfirmadoEmUtc" },
                values: new object[] { true, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_TokensUsuario_ExpiraEmUtc",
                table: "TokensUsuario",
                column: "ExpiraEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TokensUsuario_UsuarioId_Tipo",
                table: "TokensUsuario",
                columns: new[] { "UsuarioId", "Tipo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokensUsuario");

            migrationBuilder.DropColumn(
                name: "EmailConfirmado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailConfirmadoEmUtc",
                table: "Usuarios");
        }
    }
}
