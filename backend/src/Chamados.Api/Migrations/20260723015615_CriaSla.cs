using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sla",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_prioridade = table.Column<long>(type: "bigint", nullable: false),
                    tempo_resposta_min = table.Column<int>(type: "integer", nullable: false),
                    tempo_resolucao_min = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla", x => x.id);
                    table.ForeignKey(
                        name: "FK_sla_prioridade_id_prioridade",
                        column: x => x.id_prioridade,
                        principalTable: "prioridade",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "sla",
                columns: new[] { "id", "ativo", "id_prioridade", "tempo_resolucao_min", "tempo_resposta_min" },
                values: new object[,]
                {
                    { 1L, true, 1L, 2880, 480 },
                    { 2L, true, 2L, 1440, 240 },
                    { 3L, true, 3L, 480, 60 },
                    { 4L, true, 4L, 240, 15 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_sla_id_prioridade",
                table: "sla",
                column: "id_prioridade",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sla");
        }
    }
}
