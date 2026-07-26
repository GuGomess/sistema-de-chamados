using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaDepartamentoAoChamado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "id_departamento",
                table: "chamado",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateIndex(
                name: "IX_chamado_id_departamento",
                table: "chamado",
                column: "id_departamento");

            migrationBuilder.AddForeignKey(
                name: "FK_chamado_departamento_id_departamento",
                table: "chamado",
                column: "id_departamento",
                principalTable: "departamento",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chamado_departamento_id_departamento",
                table: "chamado");

            migrationBuilder.DropIndex(
                name: "IX_chamado_id_departamento",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "id_departamento",
                table: "chamado");
        }
    }
}
