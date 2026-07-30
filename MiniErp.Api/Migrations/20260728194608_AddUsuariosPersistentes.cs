using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuariosPersistentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false, collation: "NOCASE"),
                    Perfil = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SenhaHash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SenhaSalt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "CriadoEmUtc", "Email", "Nome", "Perfil", "SenhaHash", "SenhaSalt" },
                values: new object[] { 1, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), "admin@mini-erp.com", "Administrador", "Admin", new byte[] { 173, 88, 172, 146, 69, 143, 166, 143, 121, 125, 117, 118, 134, 12, 55, 108, 66, 149, 81, 187, 235, 211, 174, 112, 55, 32, 171, 70, 138, 107, 77, 54 }, new byte[] { 158, 129, 49, 162, 145, 108, 144, 71, 114, 182, 126, 103, 145, 24, 150, 12 } });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
