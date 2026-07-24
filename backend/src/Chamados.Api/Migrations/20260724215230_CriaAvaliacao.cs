using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaAvaliacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avaliacao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_chamado = table.Column<long>(type: "bigint", nullable: false),
                    id_autor = table.Column<long>(type: "bigint", nullable: false),
                    nota = table.Column<short>(type: "smallint", nullable: false),
                    comentario = table.Column<string>(type: "text", nullable: true),
                    publica = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avaliacao", x => x.id);
                    table.ForeignKey(
                        name: "FK_avaliacao_chamado_id_chamado",
                        column: x => x.id_chamado,
                        principalTable: "chamado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avaliacao_usuario_id_autor",
                        column: x => x.id_autor,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_avaliacao_id_autor",
                table: "avaliacao",
                column: "id_autor");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacao_id_chamado",
                table: "avaliacao",
                column: "id_chamado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avaliacao");
        }
    }
}
