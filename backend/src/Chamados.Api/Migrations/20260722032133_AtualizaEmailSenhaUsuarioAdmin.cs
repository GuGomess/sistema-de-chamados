using Chamados.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chamados.Api.Migrations
{
    /// <inheritdoc />
    public partial class AtualizaEmailSenhaUsuarioAdmin : Migration
    {
        private const string EmailAntigo = "admin@chamados.local";
        private const string SenhaAntiga = "Admin@123";
        private const string EmailNovo = "gus@admin.com";
        private const string SenhaNova = "admin@123";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var senhaHash = new PasswordHasher<Usuario>().HashPassword(null, SenhaNova);

            migrationBuilder.UpdateData(
                table: "usuario",
                keyColumn: "email",
                keyValue: EmailAntigo,
                columns: new[] { "email", "senha_hash" },
                values: new object[] { EmailNovo, senhaHash });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var senhaHash = new PasswordHasher<Usuario>().HashPassword(null, SenhaAntiga);

            migrationBuilder.UpdateData(
                table: "usuario",
                keyColumn: "email",
                keyValue: EmailNovo,
                columns: new[] { "email", "senha_hash" },
                values: new object[] { EmailAntigo, senhaHash });
        }
    }
}
