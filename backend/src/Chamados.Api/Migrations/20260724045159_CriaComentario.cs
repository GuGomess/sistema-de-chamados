using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaComentario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comentario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_chamado = table.Column<long>(type: "bigint", nullable: false),
                    id_autor = table.Column<long>(type: "bigint", nullable: false),
                    mensagem = table.Column<string>(type: "text", nullable: false),
                    interno = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comentario", x => x.id);
                    table.ForeignKey(
                        name: "FK_comentario_chamado_id_chamado",
                        column: x => x.id_chamado,
                        principalTable: "chamado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comentario_usuario_id_autor",
                        column: x => x.id_autor,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comentario_id_autor",
                table: "comentario",
                column: "id_autor");

            migrationBuilder.CreateIndex(
                name: "IX_comentario_id_chamado",
                table: "comentario",
                column: "id_chamado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comentario");
        }
    }
}
