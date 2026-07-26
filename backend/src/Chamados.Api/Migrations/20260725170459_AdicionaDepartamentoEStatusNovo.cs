using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaDepartamentoEStatusNovo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departamento",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario_departamento",
                columns: table => new
                {
                    DepartamentosId = table.Column<long>(type: "bigint", nullable: false),
                    TecnicosId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_departamento", x => new { x.DepartamentosId, x.TecnicosId });
                    table.ForeignKey(
                        name: "FK_usuario_departamento_departamento_DepartamentosId",
                        column: x => x.DepartamentosId,
                        principalTable: "departamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_departamento_usuario_TecnicosId",
                        column: x => x.TecnicosId,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "departamento",
                columns: new[] { "id", "ativo", "descricao", "nome" },
                values: new object[,]
                {
                    { 1L, true, null, "HelpDesk" },
                    { 2L, true, null, "Desenvolvimento" },
                    { 3L, true, null, "Infraestrutura" }
                });

            migrationBuilder.InsertData(
                table: "status",
                columns: new[] { "id", "final", "nome", "ordem" },
                values: new object[] { 6L, false, "Novo", (short)0 });

            migrationBuilder.CreateIndex(
                name: "IX_departamento_nome",
                table: "departamento",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_departamento_TecnicosId",
                table: "usuario_departamento",
                column: "TecnicosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario_departamento");

            migrationBuilder.DropTable(
                name: "departamento");

            migrationBuilder.DeleteData(
                table: "status",
                keyColumn: "id",
                keyValue: 6L);
        }
    }
}
