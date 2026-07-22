using Chamados.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chamados.Api.Data;

public class ChamadosDbContext : DbContext
{
    public ChamadosDbContext(DbContextOptions<ChamadosDbContext> options) : base(options)
    {
    }

    public DbSet<Perfil> Perfis => Set<Perfil>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.ToTable("perfil");
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
            entity.Property(p => p.Descricao).HasColumnName("descricao").HasMaxLength(255);
            entity.HasIndex(p => p.Nome).IsUnique();

            entity.HasData(
                new Perfil { Id = 1, Nome = "Administrador", Descricao = "Acesso completo: usuários, configurações de SLA, categorias e prioridades." },
                new Perfil { Id = 2, Nome = "Técnico", Descricao = "Atende chamados atribuídos, atualiza status e registra comentários." },
                new Perfil { Id = 3, Nome = "Cliente", Descricao = "Abre chamados e acompanha o andamento dos próprios atendimentos." }
            );
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuario");
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.PerfilId).HasColumnName("id_perfil");
            entity.Property(u => u.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(160).IsRequired();
            entity.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255).IsRequired();
            entity.Property(u => u.Ativo).HasColumnName("ativo").HasDefaultValue(true);
            entity.Property(u => u.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Perfil)
                .WithMany(p => p.Usuarios)
                .HasForeignKey(u => u.PerfilId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
