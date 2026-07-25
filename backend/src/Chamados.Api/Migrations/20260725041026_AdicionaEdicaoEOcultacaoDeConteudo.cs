using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaEdicaoEOcultacaoDeConteudo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "editado_em",
                table: "comentario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "oculta",
                table: "comentario",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "conteudo_editado_em",
                table: "chamado",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "descricao_oculta",
                table: "chamado",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "editado_em",
                table: "anexo",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "editado_em",
                table: "comentario");

            migrationBuilder.DropColumn(
                name: "oculta",
                table: "comentario");

            migrationBuilder.DropColumn(
                name: "conteudo_editado_em",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "descricao_oculta",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "editado_em",
                table: "anexo");
        }
    }
}
