using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaComentarioAoAnexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "id_comentario",
                table: "anexo",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_anexo_id_comentario",
                table: "anexo",
                column: "id_comentario");

            migrationBuilder.AddForeignKey(
                name: "FK_anexo_comentario_id_comentario",
                table: "anexo",
                column: "id_comentario",
                principalTable: "comentario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_anexo_comentario_id_comentario",
                table: "anexo");

            migrationBuilder.DropIndex(
                name: "IX_anexo_id_comentario",
                table: "anexo");

            migrationBuilder.DropColumn(
                name: "id_comentario",
                table: "anexo");
        }
    }
}
