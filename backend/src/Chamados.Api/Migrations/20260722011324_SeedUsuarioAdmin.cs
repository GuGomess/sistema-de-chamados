using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedUsuarioAdmin : Migration
    {
        // Usuário administrador de desenvolvimento — credenciais documentadas em backend/README.md.
        // Substituir/remover quando o CRUD de usuários (criação via API) existir.
        private const string AdminEmail = "admin@chamados.local";
        private const string AdminSenha = "Admin@123";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var senhaHash = new PasswordHasher<Usuario>().HashPassword(null, AdminSenha);

            migrationBuilder.InsertData(
                table: "usuario",
                columns: new[] { "id_perfil", "nome", "email", "senha_hash" },
                values: new object[] { 1L, "Administrador", AdminEmail, senhaHash });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "usuario",
                keyColumn: "email",
                keyValue: AdminEmail);
        }
    }
}
