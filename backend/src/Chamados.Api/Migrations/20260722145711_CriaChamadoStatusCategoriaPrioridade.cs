using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaChamadoStatusCategoriaPrioridade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prioridade",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nivel = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prioridade", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ordem = table.Column<short>(type: "smallint", nullable: false),
                    final = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chamado",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    id_solicitante = table.Column<long>(type: "bigint", nullable: false),
                    id_tecnico = table.Column<long>(type: "bigint", nullable: true),
                    id_status = table.Column<long>(type: "bigint", nullable: false),
                    id_categoria = table.Column<long>(type: "bigint", nullable: false),
                    id_prioridade = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    prazo_resposta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    prazo_resolucao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolvido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fechado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamado", x => x.id);
                    table.ForeignKey(
                        name: "FK_chamado_categoria_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamado_prioridade_id_prioridade",
                        column: x => x.id_prioridade,
                        principalTable: "prioridade",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamado_status_id_status",
                        column: x => x.id_status,
                        principalTable: "status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamado_usuario_id_solicitante",
                        column: x => x.id_solicitante,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamado_usuario_id_tecnico",
                        column: x => x.id_tecnico,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "categoria",
                columns: new[] { "id", "ativa", "descricao", "nome" },
                values: new object[,]
                {
                    { 1L, true, null, "Hardware" },
                    { 2L, true, null, "Software" },
                    { 3L, true, null, "Rede" },
                    { 4L, true, null, "Acesso" }
                });

            migrationBuilder.InsertData(
                table: "prioridade",
                columns: new[] { "id", "nivel", "nome" },
                values: new object[,]
                {
                    { 1L, (short)1, "Baixa" },
                    { 2L, (short)2, "Média" },
                    { 3L, (short)3, "Alta" },
                    { 4L, (short)4, "Crítica" }
                });

            migrationBuilder.InsertData(
                table: "status",
                columns: new[] { "id", "final", "nome", "ordem" },
                values: new object[,]
                {
                    { 1L, false, "Aberto", (short)1 },
                    { 2L, false, "Em Atendimento", (short)2 },
                    { 3L, false, "Aguardando Cliente", (short)3 },
                    { 4L, true, "Resolvido", (short)4 },
                    { 5L, true, "Fechado", (short)5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_categoria_nome",
                table: "categoria",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamado_id_categoria",
                table: "chamado",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_id_prioridade",
                table: "chamado",
                column: "id_prioridade");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_id_solicitante",
                table: "chamado",
                column: "id_solicitante");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_id_status",
                table: "chamado",
                column: "id_status");

            migrationBuilder.CreateIndex(
                name: "IX_chamado_id_tecnico",
                table: "chamado",
                column: "id_tecnico");

            migrationBuilder.CreateIndex(
                name: "IX_prioridade_nome",
                table: "prioridade",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_nome",
                table: "status",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chamado");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "prioridade");

            migrationBuilder.DropTable(
                name: "status");
        }
    }
}
