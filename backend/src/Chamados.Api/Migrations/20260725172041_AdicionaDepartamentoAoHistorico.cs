using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaDepartamentoAoHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "id_departamento_anterior",
                table: "historico",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "id_departamento_novo",
                table: "historico",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_departamento_anterior",
                table: "historico",
                column: "id_departamento_anterior");

            migrationBuilder.CreateIndex(
                name: "IX_historico_id_departamento_novo",
                table: "historico",
                column: "id_departamento_novo");

            migrationBuilder.AddForeignKey(
                name: "FK_historico_departamento_id_departamento_anterior",
                table: "historico",
                column: "id_departamento_anterior",
                principalTable: "departamento",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_historico_departamento_id_departamento_novo",
                table: "historico",
                column: "id_departamento_novo",
                principalTable: "departamento",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_historico_departamento_id_departamento_anterior",
                table: "historico");

            migrationBuilder.DropForeignKey(
                name: "FK_historico_departamento_id_departamento_novo",
                table: "historico");

            migrationBuilder.DropIndex(
                name: "IX_historico_id_departamento_anterior",
                table: "historico");

            migrationBuilder.DropIndex(
                name: "IX_historico_id_departamento_novo",
                table: "historico");

            migrationBuilder.DropColumn(
                name: "id_departamento_anterior",
                table: "historico");

            migrationBuilder.DropColumn(
                name: "id_departamento_novo",
                table: "historico");
        }
    }
}
