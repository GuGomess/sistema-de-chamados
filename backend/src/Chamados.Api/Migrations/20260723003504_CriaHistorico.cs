using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_chamado = table.Column<long>(type: "bigint", nullable: false),
                    id_autor = table.Column<long>(type: "bigint", nullable: false),
                    id_status_anterior = table.Column<long>(type: "bigint", nullable: true),
                    id_status_novo = table.Column<long>(type: "bigint", nullable: true),
                    acao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    detalhe = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico", x => x.id);
                    table.ForeignKey(
                        name: "FK_historico_chamado_id_chamado",
                        column: x => x.id_chamado,
                        principalTable: "chamado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historico_status_id_status_anterior",
                        column: x => x.id_status_anterior,
                        principalTable: "status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historico_status_id_status_novo",
                        column: x => x.id_status_novo,
                        principalTable: "status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historico_usuario_id_autor",
                        column: x => x.id_autor,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_autor",
                table: "historico",
                column: "id_autor");

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_chamado",
                table: "historico",
                column: "id_chamado");

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_status_anterior",
                table: "historico",
                column: "id_status_anterior");

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_status_novo",
                table: "historico",
                column: "id_status_novo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historico");
        }
    }
}
