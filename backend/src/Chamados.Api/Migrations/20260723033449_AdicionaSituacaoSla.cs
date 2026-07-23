using Chamados.Api.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaSituacaoSla : Migration
    {
        // Usuário técnico usado como autor das entradas de histórico geradas
        // automaticamente pelo SlaMonitorService. Ativo=false: nunca faz login.
        private const long PerfilAdministradorId = 1L;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "situacao_sla_resolucao",
                table: "chamado",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "EmDia");

            migrationBuilder.AddColumn<string>(
                name: "situacao_sla_resposta",
                table: "chamado",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "EmDia");

            migrationBuilder.InsertData(
                table: "usuario",
                columns: new[] { "id_perfil", "nome", "email", "senha_hash", "ativo" },
                values: new object[] { PerfilAdministradorId, "Sistema", UsuarioSistema.Email, "SISTEMA_LOGIN_DESABILITADO", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "usuario",
                keyColumn: "email",
                keyValue: UsuarioSistema.Email);

            migrationBuilder.DropColumn(
                name: "situacao_sla_resolucao",
                table: "chamado");

            migrationBuilder.DropColumn(
                name: "situacao_sla_resposta",
                table: "chamado");
        }
    }
}
