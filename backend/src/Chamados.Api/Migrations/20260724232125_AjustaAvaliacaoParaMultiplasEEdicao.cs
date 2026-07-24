using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjustaAvaliacaoParaMultiplasEEdicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_avaliacao_id_chamado",
                table: "avaliacao");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "editado_em",
                table: "avaliacao",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "oculta",
                table: "avaliacao",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_avaliacao_id_chamado",
                table: "avaliacao",
                column: "id_chamado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_avaliacao_id_chamado",
                table: "avaliacao");

            migrationBuilder.DropColumn(
                name: "editado_em",
                table: "avaliacao");

            migrationBuilder.DropColumn(
                name: "oculta",
                table: "avaliacao");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacao_id_chamado",
                table: "avaliacao",
                column: "id_chamado",
                unique: true);
        }
    }
}
