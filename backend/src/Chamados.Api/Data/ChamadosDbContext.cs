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

    public DbSet<Status> Status => Set<Status>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Prioridade> Prioridades => Set<Prioridade>();

    public DbSet<Sla> Slas => Set<Sla>();

    public DbSet<Chamado> Chamados => Set<Chamado>();

    public DbSet<Historico> Historicos => Set<Historico>();

    public DbSet<Comentario> Comentarios => Set<Comentario>();

    public DbSet<Anexo> Anexos => Set<Anexo>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

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

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("status");
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
            entity.Property(s => s.Ordem).HasColumnName("ordem");
            entity.Property(s => s.Final).HasColumnName("final");

            entity.HasIndex(s => s.Nome).IsUnique();

            entity.HasData(
                new Status { Id = 1, Nome = "Aberto", Ordem = 1, Final = false },
                new Status { Id = 2, Nome = "Em Atendimento", Ordem = 2, Final = false },
                new Status { Id = 3, Nome = "Aguardando Cliente", Ordem = 3, Final = false },
                new Status { Id = 4, Nome = "Resolvido", Ordem = 4, Final = true },
                new Status { Id = 5, Nome = "Fechado", Ordem = 5, Final = true }
            );
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categoria");
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
            entity.Property(c => c.Descricao).HasColumnName("descricao").HasMaxLength(255);
            entity.Property(c => c.Ativa).HasColumnName("ativa").HasDefaultValue(true);

            entity.HasIndex(c => c.Nome).IsUnique();

            entity.HasData(
                new Categoria { Id = 1, Nome = "Hardware", Ativa = true },
                new Categoria { Id = 2, Nome = "Software", Ativa = true },
                new Categoria { Id = 3, Nome = "Rede", Ativa = true },
                new Categoria { Id = 4, Nome = "Acesso", Ativa = true },
                // Atribuída automaticamente a chamados abertos por clientes (que não
                // escolhem categoria); técnico/administrador reclassificam na triagem.
                new Categoria { Id = 5, Nome = "A Triar", Descricao = "Categoria inicial de chamados abertos por clientes, pendente de triagem.", Ativa = true }
            );
        });

        modelBuilder.Entity<Prioridade>(entity =>
        {
            entity.ToTable("prioridade");
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
            entity.Property(p => p.Nivel).HasColumnName("nivel");

            entity.HasIndex(p => p.Nome).IsUnique();

            entity.HasData(
                new Prioridade { Id = 1, Nome = "Baixa", Nivel = 1 },
                new Prioridade { Id = 2, Nome = "Média", Nivel = 2 },
                new Prioridade { Id = 3, Nome = "Alta", Nivel = 3 },
                new Prioridade { Id = 4, Nome = "Crítica", Nivel = 4 }
            );
        });

        modelBuilder.Entity<Sla>(entity =>
        {
            entity.ToTable("sla");
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.PrioridadeId).HasColumnName("id_prioridade");
            entity.Property(s => s.TempoRespostaMin).HasColumnName("tempo_resposta_min");
            entity.Property(s => s.TempoResolucaoMin).HasColumnName("tempo_resolucao_min");
            entity.Property(s => s.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            entity.HasIndex(s => s.PrioridadeId).IsUnique();

            entity.HasOne(s => s.Prioridade)
                .WithMany()
                .HasForeignKey(s => s.PrioridadeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new Sla { Id = 1, PrioridadeId = 1, TempoRespostaMin = 480, TempoResolucaoMin = 2880, Ativo = true },
                new Sla { Id = 2, PrioridadeId = 2, TempoRespostaMin = 240, TempoResolucaoMin = 1440, Ativo = true },
                new Sla { Id = 3, PrioridadeId = 3, TempoRespostaMin = 60, TempoResolucaoMin = 480, Ativo = true },
                new Sla { Id = 4, PrioridadeId = 4, TempoRespostaMin = 15, TempoResolucaoMin = 240, Ativo = true }
            );
        });

        modelBuilder.Entity<Chamado>(entity =>
        {
            entity.ToTable("chamado");
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Titulo).HasColumnName("titulo").HasMaxLength(160).IsRequired();
            entity.Property(c => c.Descricao).HasColumnName("descricao").IsRequired();
            entity.Property(c => c.SolicitanteId).HasColumnName("id_solicitante");
            entity.Property(c => c.TecnicoId).HasColumnName("id_tecnico");
            entity.Property(c => c.StatusId).HasColumnName("id_status");
            entity.Property(c => c.CategoriaId).HasColumnName("id_categoria");
            entity.Property(c => c.PrioridadeId).HasColumnName("id_prioridade");
            entity.Property(c => c.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");
            entity.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em").HasDefaultValueSql("now()");
            entity.Property(c => c.PrazoResposta).HasColumnName("prazo_resposta");
            entity.Property(c => c.PrazoResolucao).HasColumnName("prazo_resolucao");
            entity.Property(c => c.PrimeiraRespostaEm).HasColumnName("primeira_resposta_em");
            entity.Property(c => c.ResolvidoEm).HasColumnName("resolvido_em");
            entity.Property(c => c.FechadoEm).HasColumnName("fechado_em");
            entity.Property(c => c.SituacaoSlaResposta)
                .HasColumnName("situacao_sla_resposta")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(SituacaoSla.EmDia);
            entity.Property(c => c.SituacaoSlaResolucao)
                .HasColumnName("situacao_sla_resolucao")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(SituacaoSla.EmDia);

            entity.HasOne(c => c.Solicitante)
                .WithMany(u => u.ChamadosSolicitados)
                .HasForeignKey(c => c.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Tecnico)
                .WithMany(u => u.ChamadosAtendidos)
                .HasForeignKey(c => c.TecnicoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Status)
                .WithMany(s => s.Chamados)
                .HasForeignKey(c => c.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Categoria)
                .WithMany(cat => cat.Chamados)
                .HasForeignKey(c => c.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Prioridade)
                .WithMany(p => p.Chamados)
                .HasForeignKey(c => c.PrioridadeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Historico>(entity =>
        {
            entity.ToTable("historico");
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.ChamadoId).HasColumnName("id_chamado");
            entity.Property(h => h.AutorId).HasColumnName("id_autor");
            entity.Property(h => h.StatusAnteriorId).HasColumnName("id_status_anterior");
            entity.Property(h => h.StatusNovoId).HasColumnName("id_status_novo");
            entity.Property(h => h.Acao).HasColumnName("acao").HasMaxLength(80).IsRequired();
            entity.Property(h => h.Detalhe).HasColumnName("detalhe");
            entity.Property(h => h.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(h => h.Chamado)
                .WithMany()
                .HasForeignKey(h => h.ChamadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Autor)
                .WithMany()
                .HasForeignKey(h => h.AutorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.StatusAnterior)
                .WithMany()
                .HasForeignKey(h => h.StatusAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.StatusNovo)
                .WithMany()
                .HasForeignKey(h => h.StatusNovoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comentario>(entity =>
        {
            entity.ToTable("comentario");
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.ChamadoId).HasColumnName("id_chamado");
            entity.Property(c => c.AutorId).HasColumnName("id_autor");
            entity.Property(c => c.Mensagem).HasColumnName("mensagem").IsRequired();
            entity.Property(c => c.Interno).HasColumnName("interno").HasDefaultValue(false);
            entity.Property(c => c.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(c => c.Chamado)
                .WithMany()
                .HasForeignKey(c => c.ChamadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Autor)
                .WithMany()
                .HasForeignKey(c => c.AutorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Anexo>(entity =>
        {
            entity.ToTable("anexo");
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.ChamadoId).HasColumnName("id_chamado");
            entity.Property(a => a.AutorId).HasColumnName("id_autor");
            entity.Property(a => a.NomeArquivo).HasColumnName("nome_arquivo").HasMaxLength(255).IsRequired();
            entity.Property(a => a.Caminho).HasColumnName("caminho").HasMaxLength(500).IsRequired();
            entity.Property(a => a.TipoMime).HasColumnName("tipo_mime").HasMaxLength(120).IsRequired();
            entity.Property(a => a.TamanhoBytes).HasColumnName("tamanho_bytes");
            entity.Property(a => a.ComentarioId).HasColumnName("id_comentario");
            entity.Property(a => a.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(a => a.Chamado)
                .WithMany()
                .HasForeignKey(a => a.ChamadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Autor)
                .WithMany()
                .HasForeignKey(a => a.AutorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Comentario)
                .WithMany(c => c.Anexos)
                .HasForeignKey(a => a.ComentarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.ToTable("notificacao");
            entity.Property(n => n.Id).HasColumnName("id");
            entity.Property(n => n.DestinatarioId).HasColumnName("id_destinatario");
            entity.Property(n => n.ChamadoId).HasColumnName("id_chamado");
            entity.Property(n => n.Tipo)
                .HasColumnName("tipo")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(n => n.Mensagem).HasColumnName("mensagem").HasMaxLength(255).IsRequired();
            entity.Property(n => n.Lida).HasColumnName("lida").HasDefaultValue(false);
            entity.Property(n => n.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasIndex(n => new { n.DestinatarioId, n.Lida });

            entity.HasOne(n => n.Destinatario)
                .WithMany()
                .HasForeignKey(n => n.DestinatarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Chamado)
                .WithMany()
                .HasForeignKey(n => n.ChamadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Avaliacao>(entity =>
        {
            entity.ToTable("avaliacao");
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.ChamadoId).HasColumnName("id_chamado");
            entity.Property(a => a.AutorId).HasColumnName("id_autor");
            entity.Property(a => a.Nota).HasColumnName("nota");
            entity.Property(a => a.Comentario).HasColumnName("comentario");
            entity.Property(a => a.Publica).HasColumnName("publica").HasDefaultValue(false);
            entity.Property(a => a.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasIndex(a => a.ChamadoId).IsUnique();

            entity.HasOne(a => a.Chamado)
                .WithMany()
                .HasForeignKey(a => a.ChamadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Autor)
                .WithMany()
                .HasForeignKey(a => a.AutorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
