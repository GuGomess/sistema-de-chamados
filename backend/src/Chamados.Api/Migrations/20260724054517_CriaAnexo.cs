using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriaAnexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anexo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_chamado = table.Column<long>(type: "bigint", nullable: false),
                    id_autor = table.Column<long>(type: "bigint", nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    caminho = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tipo_mime = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexo", x => x.id);
                    table.ForeignKey(
                        name: "FK_anexo_chamado_id_chamado",
                        column: x => x.id_chamado,
                        principalTable: "chamado",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_anexo_usuario_id_autor",
                        column: x => x.id_autor,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexo_id_autor",
                table: "anexo",
                column: "id_autor");

            migrationBuilder.CreateIndex(
                name: "IX_anexo_id_chamado",
                table: "anexo",
                column: "id_chamado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anexo");
        }
    }
}
