using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidosCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidosCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FornecedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecebidoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidosCompraItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoCompraId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoCodigo = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosCompraItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosCompraItens_PedidosCompra_PedidoCompraId",
                        column: x => x.PedidoCompraId,
                        principalTable: "PedidosCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidosCompraItens_Produtos_ProdutoCodigo",
                        column: x => x.ProdutoCodigo,
                        principalTable: "Produtos",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_CriadoEmUtc",
                table: "PedidosCompra",
                column: "CriadoEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_FornecedorId",
                table: "PedidosCompra",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompraItens_PedidoCompraId",
                table: "PedidosCompraItens",
                column: "PedidoCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompraItens_ProdutoCodigo",
                table: "PedidosCompraItens",
                column: "ProdutoCodigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidosCompraItens");

            migrationBuilder.DropTable(
                name: "PedidosCompra");
        }
    }
}
