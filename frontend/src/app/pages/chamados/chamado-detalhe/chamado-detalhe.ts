import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl, SafeUrl } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { ChamadoService } from '../../../core/services/chamado.service';
import { DepartamentoService } from '../../../core/services/departamento.service';
import { RealtimeService } from '../../../core/services/realtime.service';
import {
  Anexo,
  Avaliacao,
  AvaliacaoCreateRequest,
  AvaliacaoUpdateRequest,
  Categoria,
  Chamado,
  ChamadoConteudoUpdateRequest,
  ChamadoUpdateRequest,
  Comentario,
  ComentarioCreateRequest,
  ComentarioUpdateRequest,
  Historico,
  PrazoResolucaoUpdateRequest,
  PrazoRespostaUpdateRequest,
  Prioridade,
  SituacaoSla,
  Status,
  UsuarioResumo,
} from '../../../core/models/chamado.model';
import { Departamento } from '../../../core/models/departamento.model';

interface ErrorResponse {
  errors?: Record<string, string[]>;
  detail?: string;
}

interface AnexoPreview {
  nomeArquivo: string;
  tipoMime: string;
  objectUrl: string;
  imagemUrl: SafeUrl;
  documentoUrl: SafeResourceUrl;
}

@Component({
  selector: 'app-chamado-detalhe',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './chamado-detalhe.html',
  styleUrl: './chamado-detalhe.scss',
})
export class ChamadoDetalhe implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly chamadoService = inject(ChamadoService);
  private readonly departamentoService = inject(DepartamentoService);
  private readonly authService = inject(AuthService);
  private readonly realtimeService = inject(RealtimeService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly sanitizer = inject(DomSanitizer);

  // HelpDesk (Departamento Id = 1, ver ChamadosController.DepartamentoHelpDeskId).
  private readonly HELPDESK_DEPARTAMENTO_ID = 1;

  // Status "Novo" (Id = 6, ver ChamadosController.StatusNovoId) só é atingido
  // via /devolver-helpdesk — fora do select manual de status.
  private readonly STATUS_NOVO_ID = 6;

  private readonly id = Number(this.route.snapshot.paramMap.get('id'));
  private readonly perfil = this.authService.getPerfil();
  protected readonly usuarioId = this.authService.getUsuario()?.id ?? null;
  private readonly usuarioDepartamentos = this.authService.getUsuario()?.departamentos ?? [];

  protected readonly chamado = signal<Chamado | null>(null);
  protected readonly statusList = signal<Status[]>([]);
  protected readonly statusEditavel = computed(() => this.statusList().filter((s) => s.id !== this.STATUS_NOVO_ID));
  protected readonly tecnicos = signal<UsuarioResumo[]>([]);
  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly prioridades = signal<Prioridade[]>([]);
  protected readonly departamentos = signal<Departamento[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly salvando = signal(false);
  protected readonly acaoErro = signal<string | null>(null);

  protected readonly comentarios = signal<Comentario[]>([]);
  protected readonly carregandoComentarios = signal(true);
  protected readonly enviandoComentario = signal(false);
  protected readonly comentarioErro = signal<string | null>(null);
  protected readonly comentarioArquivos = signal<File[]>([]);
  protected readonly editandoComentarioId = signal<number | null>(null);

  private readonly extensoesPermitidas = [
    '.jpg', '.jpeg', '.png', '.gif', '.webp',
    '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx', '.txt', '.csv',
    '.mp4', '.mov', '.avi', '.webm', '.mkv',
  ];
  private readonly tamanhoMaximoArquivoBytes = 100 * 1024 * 1024;

  protected readonly anexos = signal<Anexo[]>([]);
  protected readonly carregandoAnexos = signal(true);
  protected readonly enviandoAnexo = signal(false);
  protected readonly anexoErro = signal<string | null>(null);
  protected readonly anexoPreview = signal<AnexoPreview | null>(null);
  protected readonly substituindoAnexoId = signal<number | null>(null);

  // Miniaturas de anexos de imagem carregadas sob demanda (o download exige
  // Authorization, então um <img src> direto não funciona — ver carregarThumbsImagens).
  protected readonly anexoThumbs = signal<Map<number, SafeUrl>>(new Map());
  private readonly anexoThumbObjectUrls: string[] = [];

  protected readonly avaliacoes = signal<Avaliacao[]>([]);
  protected readonly carregandoAvaliacao = signal(true);
  protected readonly enviandoAvaliacao = signal(false);
  protected readonly avaliacaoErro = signal<string | null>(null);
  protected readonly editandoAvaliacaoId = signal<number | null>(null);

  protected readonly historico = signal<Historico[]>([]);
  protected readonly carregandoHistorico = signal(true);

  // Novo fluxo de alteração de prazo: fica oculto por padrão (só a label com a
  // data) e só revela o calendário + motivo quando o usuário clica em "Alterar
  // prazo" — evita o antigo comportamento de salvar sozinho a cada campo tocado.
  protected readonly editandoPrazoResposta = signal(false);
  protected readonly editandoPrazoResolucao = signal(false);

  // Edição inline de título/descrição do chamado — mesmo padrão de "fica oculto
  // até clicar em Editar" usado para o prazo, ver toggleEdicaoPrazoResolucao.
  protected readonly editandoConteudo = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    idStatus: [null as number | null],
    idCategoria: [null as number | null],
    idPrioridade: [null as number | null],
  });

  protected readonly atribuirForm = this.formBuilder.nonNullable.group({
    idTecnico: [null as number | null],
  });

  protected readonly encaminharForm = this.formBuilder.nonNullable.group({
    idDepartamento: [null as number | null],
  });

  protected readonly fecharClienteForm = this.formBuilder.nonNullable.group({
    motivo: [''],
  });

  protected readonly avaliacaoForm = this.formBuilder.nonNullable.group({
    nota: [null as number | null],
    comentario: [''],
    publica: [true],
  });

  protected readonly avaliacaoEdicaoForm = this.formBuilder.nonNullable.group({
    nota: [null as number | null],
    comentario: [''],
    publica: [true],
  });

  protected readonly comentarioForm = this.formBuilder.nonNullable.group({
    mensagem: [''],
    interno: [false],
  });

  protected readonly comentarioEdicaoForm = this.formBuilder.nonNullable.group({
    mensagem: [''],
  });

  protected readonly conteudoForm = this.formBuilder.nonNullable.group({
    titulo: [''],
    descricao: [''],
  });

  protected readonly prazoForm = this.formBuilder.nonNullable.group({
    prazoResolucao: [''],
    justificativa: [''],
  });

  protected readonly prazoRespostaForm = this.formBuilder.nonNullable.group({
    prazoResposta: [''],
  });

  ngOnInit(): void {
    this.carregarChamado();
    this.carregarComentarios();
    this.carregarAnexos();
    this.carregarAvaliacoes();
    this.carregarHistorico();

    if (this.ehAdministrador() || this.ehTecnico()) {
      this.chamadoService.listarStatus().subscribe({ next: (status) => this.statusList.set(status) });
      // Cliente não escolhe categoria/prioridade ao abrir o chamado (fica "A
      // Triar"/"Média" por padrão) — precisa que técnico/admin reclassifiquem.
      this.chamadoService.listarCategorias().subscribe({ next: (categorias) => this.categorias.set(categorias) });
      this.chamadoService.listarPrioridades().subscribe({ next: (prioridades) => this.prioridades.set(prioridades) });

      this.departamentoService.listar(true).subscribe({ next: (departamentos) => this.departamentos.set(departamentos) });
    }

    this.realtimeService.entrarChamado(this.id);
    this.realtimeService.on<number>('ChamadoAtualizado', (chamadoId) => {
      if (chamadoId === this.id) {
        this.carregarChamado();
        this.carregarComentarios();
        this.carregarAnexos();
        this.carregarAvaliacoes();
        this.carregarHistorico();
      }
    });
  }

  ngOnDestroy(): void {
    this.realtimeService.sairChamado(this.id);

    const preview = this.anexoPreview();
    if (preview) {
      URL.revokeObjectURL(preview.objectUrl);
    }
    for (const objectUrl of this.anexoThumbObjectUrls) {
      URL.revokeObjectURL(objectUrl);
    }
  }

  protected situacaoSlaLabel(situacao: SituacaoSla): string {
    switch (situacao) {
      case 'EmRisco':
        return 'Em risco';
      case 'Vencido':
        return 'Vencido';
      default:
        return 'Em dia';
    }
  }

  protected ehAdministrador(): boolean {
    return this.perfil === 'ADMINISTRADOR';
  }

  protected ehTecnico(): boolean {
    return this.perfil === 'TECNICO';
  }

  // Cliente não vê categoria/prioridade/SLA (conceitos de triagem interna).
  protected ehCliente(): boolean {
    return this.perfil === 'CLIENTE';
  }

  protected podeAssumir(): boolean {
    const chamado = this.chamado();
    return (
      this.ehTecnico() &&
      !!chamado &&
      !chamado.tecnico &&
      !chamado.status.final &&
      this.pertenceAoDepartamento(chamado.departamento.id)
    );
  }

  protected podeReabrir(): boolean {
    const chamado = this.chamado();
    return !!chamado && chamado.status.final;
  }

  protected podeAlterarStatus(): boolean {
    const chamado = this.chamado();
    if (!chamado) {
      return false;
    }
    return this.ehAdministrador() || (this.ehTecnico() && chamado.tecnico?.id === this.usuarioId);
  }

  protected podeAtribuirTecnico(): boolean {
    const chamado = this.chamado();
    if (!chamado || chamado.status.final) {
      return false;
    }
    if (this.ehAdministrador()) {
      return true;
    }
    return this.ehTecnico() && (!chamado.tecnico || chamado.tecnico.id === this.usuarioId);
  }

  private pertenceAoDepartamento(idDepartamento: number): boolean {
    return this.usuarioDepartamentos.some((departamento) => departamento.id === idDepartamento);
  }

  protected podeEncaminhar(): boolean {
    const chamado = this.chamado();
    if (!chamado || chamado.status.final) {
      return false;
    }
    return this.ehAdministrador() || (this.ehTecnico() && this.pertenceAoDepartamento(this.HELPDESK_DEPARTAMENTO_ID));
  }

  protected podeDevolverHelpDesk(): boolean {
    const chamado = this.chamado();
    if (!chamado || chamado.status.final || chamado.departamento.id === this.HELPDESK_DEPARTAMENTO_ID) {
      return false;
    }
    return this.ehAdministrador() || (this.ehTecnico() && this.pertenceAoDepartamento(chamado.departamento.id));
  }

  // Cliente pode fechar o próprio chamado enquanto ainda estiver em andamento
  // (mesmo sem nenhuma atividade). Uma vez "Resolvido", "Fechar chamado" some
  // — as ações que fazem sentido nesse ponto são "Reabrir" ou avaliar; fechar
  // diretamente também abriria mão da avaliação (só chamado "Resolvido" pode
  // ser avaliado, ver podeAvaliarAgora).
  protected podeFecharComoCliente(): boolean {
    const chamado = this.chamado();
    return this.ehCliente() && !!chamado && chamado.status.nome !== 'Fechado' && chamado.status.nome !== 'Resolvido';
  }

  protected podeAjustarPrazo(): boolean {
    const chamado = this.chamado();
    if (!chamado || chamado.status.final) {
      return false;
    }
    return this.ehAdministrador() || (this.ehTecnico() && chamado.tecnico?.id === this.usuarioId);
  }

  protected podeMarcarComentarioInterno(): boolean {
    return this.ehAdministrador() || this.ehTecnico();
  }

  // Resolver exige que tenha havido ao menos um retorno (comentário) desde a
  // última vez que o chamado foi aberto (reabertura mais recente, ou a
  // abertura original se nunca foi reaberto) — usado pra desabilitar a opção
  // "Resolvido" no select de status antes mesmo de tentar salvar (o backend
  // também recusa, ver salvarAlteracoes). Um comentário de um ciclo anterior
  // (antes da reabertura) não conta.
  protected statusResolvidoBloqueado(): boolean {
    if (this.carregandoComentarios() || this.carregandoHistorico()) {
      return false;
    }

    const abertoDesde = this.reabertoEm() ?? this.chamado()?.criadoEm;
    if (!abertoDesde) {
      return this.comentarios().length === 0;
    }

    const abertoDesdeMs = new Date(abertoDesde).getTime();
    return !this.comentarios().some((c) => new Date(c.criadoEm).getTime() >= abertoDesdeMs);
  }

  // Não há coluna própria pra "reaberto em" — deriva da entrada de histórico
  // mais recente com acao "Reabertura" (a API já devolve o histórico do mais
  // novo pro mais antigo, então a primeira ocorrência já é a mais recente).
  protected reabertoEm(): string | null {
    return this.historico().find((evento) => evento.acao === 'Reabertura')?.criadoEm ?? null;
  }

  protected enviarComentario(): void {
    if (this.enviandoComentario()) {
      return;
    }

    const { mensagem, interno } = this.comentarioForm.getRawValue();
    if (!mensagem.trim()) {
      this.comentarioErro.set('Escreva uma mensagem para comentar.');
      return;
    }

    const request: ComentarioCreateRequest = {
      mensagem: mensagem.trim(),
      interno: this.podeMarcarComentarioInterno() && interno,
      arquivos: this.comentarioArquivos(),
    };

    this.enviandoComentario.set(true);
    this.comentarioErro.set(null);

    this.chamadoService.criarComentario(this.id, request).subscribe({
      next: (comentario) => {
        this.comentarios.update((atual) => [...atual, comentario]);
        this.comentarioForm.reset({ mensagem: '', interno: false });
        this.comentarioArquivos.set([]);
        this.enviandoComentario.set(false);
        this.carregarThumbsImagens(comentario.anexos);
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoComentario.set(false);
        this.comentarioErro.set(this.mensagemErroComentario(error));
      },
    });
  }

  protected selecionarArquivosComentario(event: Event): void {
    const input = event.target as HTMLInputElement;
    const arquivos = Array.from(input.files ?? []);
    input.value = '';

    for (const arquivo of arquivos) {
      const erro = this.validarArquivoComentario(arquivo);
      if (erro) {
        this.comentarioErro.set(erro);
        return;
      }
    }

    this.comentarioErro.set(null);
    this.comentarioArquivos.update((atual) => [...atual, ...arquivos]);
  }

  protected removerArquivoComentario(index: number): void {
    this.comentarioArquivos.update((atual) => atual.filter((_, i) => i !== index));
  }

  protected podeEditarComentario(comentario: Comentario): boolean {
    return comentario.autor.id === this.usuarioId;
  }

  protected iniciarEdicaoComentario(comentario: Comentario): void {
    this.comentarioErro.set(null);
    this.editandoComentarioId.set(comentario.id);
    this.comentarioEdicaoForm.reset({ mensagem: comentario.mensagem });
  }

  protected cancelarEdicaoComentario(): void {
    this.editandoComentarioId.set(null);
  }

  protected salvarEdicaoComentario(comentario: Comentario): void {
    if (this.enviandoComentario()) {
      return;
    }

    const { mensagem } = this.comentarioEdicaoForm.getRawValue();
    if (!mensagem.trim()) {
      this.comentarioErro.set('Escreva uma mensagem para comentar.');
      return;
    }

    const request: ComentarioUpdateRequest = { mensagem: mensagem.trim() };

    this.enviandoComentario.set(true);
    this.comentarioErro.set(null);

    this.chamadoService.editarComentario(this.id, comentario.id, request).subscribe({
      next: (atualizado) => {
        this.atualizarComentarioNaLista(atualizado);
        this.editandoComentarioId.set(null);
        this.enviandoComentario.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoComentario.set(false);
        this.comentarioErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  protected ocultarComentario(comentario: Comentario): void {
    this.chamadoService.ocultarComentario(this.id, comentario.id, !comentario.oculta).subscribe({
      next: (atualizado) => this.atualizarComentarioNaLista(atualizado),
      error: (error: HttpErrorResponse) => {
        this.comentarioErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  private atualizarComentarioNaLista(atualizado: Comentario): void {
    this.comentarios.update((atual) => atual.map((item) => (item.id === atualizado.id ? atualizado : item)));
  }

  private validarArquivoComentario(arquivo: File): string | null {
    const extensao = `.${arquivo.name.split('.').pop()?.toLowerCase() ?? ''}`;
    if (!this.extensoesPermitidas.includes(extensao)) {
      return `Tipo de arquivo não permitido: ${arquivo.name}.`;
    }
    if (arquivo.size > this.tamanhoMaximoArquivoBytes) {
      return `Arquivo muito grande: ${arquivo.name}.`;
    }
    return null;
  }

  private mensagemErroComentario(error: HttpErrorResponse): string {
    if (error.status === 422) {
      const body = error.error as ErrorResponse;
      if (body?.detail) {
        return body.detail;
      }
    }
    if (error.status === 403) {
      return 'Você não tem permissão para comentar neste chamado.';
    }
    return 'Não foi possível enviar o comentário. Tente novamente.';
  }

  private carregarComentarios(): void {
    this.carregandoComentarios.set(true);

    this.chamadoService.listarComentarios(this.id).subscribe({
      next: (comentarios) => {
        this.comentarios.set(comentarios);
        this.carregandoComentarios.set(false);
        this.carregarThumbsImagens(comentarios.flatMap((c) => c.anexos));
      },
      error: () => {
        this.carregandoComentarios.set(false);
      },
    });
  }

  private carregarAnexos(): void {
    this.carregandoAnexos.set(true);

    this.chamadoService.listarAnexos(this.id).subscribe({
      next: (anexos) => {
        this.anexos.set(anexos);
        this.carregandoAnexos.set(false);
        this.carregarThumbsImagens(anexos);
      },
      error: () => {
        this.carregandoAnexos.set(false);
      },
    });
  }

  private carregarAvaliacoes(): void {
    this.carregandoAvaliacao.set(true);

    this.chamadoService.listarAvaliacoes(this.id).subscribe({
      next: (avaliacoes) => {
        this.avaliacoes.set(avaliacoes);
        this.carregandoAvaliacao.set(false);
      },
      error: () => {
        this.carregandoAvaliacao.set(false);
      },
    });
  }

  private carregarHistorico(): void {
    this.carregandoHistorico.set(true);

    this.chamadoService.listarHistorico(this.id).subscribe({
      next: (historico) => {
        this.historico.set(historico);
        this.carregandoHistorico.set(false);
      },
      error: () => {
        this.carregandoHistorico.set(false);
      },
    });
  }

  // Só chamados "Resolvido" (não "Fechado") podem receber avaliação — reabrir e
  // resolver de novo libera um novo ciclo, empilhando sobre avaliações anteriores.
  protected podeAvaliarAgora(): boolean {
    const chamado = this.chamado();
    if (!chamado || !this.ehCliente() || chamado.status.nome !== 'Resolvido') {
      return false;
    }
    return !this.cicloAtualJaAvaliado();
  }

  private cicloAtualJaAvaliado(): boolean {
    const chamado = this.chamado();
    if (!chamado?.resolvidoEm) {
      return false;
    }
    const resolvidoEm = new Date(chamado.resolvidoEm).getTime();
    return this.avaliacoes().some((av) => new Date(av.criadoEm).getTime() >= resolvidoEm);
  }

  protected mostrarBlocoAvaliacao(): boolean {
    const chamado = this.chamado();
    if (!chamado || !chamado.status.final) {
      return false;
    }
    if (this.carregandoAvaliacao()) {
      return true;
    }
    return this.avaliacoes().length > 0 || this.podeAvaliarAgora();
  }

  protected enviarAvaliacao(): void {
    if (this.enviandoAvaliacao()) {
      return;
    }

    const { nota, comentario, publica } = this.avaliacaoForm.getRawValue();
    if (nota === null || nota < 0 || nota > 5) {
      this.avaliacaoErro.set('Selecione uma nota entre 0 e 5.');
      return;
    }

    const request: AvaliacaoCreateRequest = {
      nota,
      comentario: comentario.trim() ? comentario.trim() : undefined,
      publica,
    };

    this.enviandoAvaliacao.set(true);
    this.avaliacaoErro.set(null);

    this.chamadoService.enviarAvaliacao(this.id, request).subscribe({
      next: (avaliacao) => {
        this.avaliacoes.update((atual) => [avaliacao, ...atual]);
        this.avaliacaoForm.reset({ nota: null, comentario: '', publica: true });
        this.enviandoAvaliacao.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoAvaliacao.set(false);
        this.avaliacaoErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  protected iniciarEdicaoAvaliacao(av: Avaliacao): void {
    this.avaliacaoErro.set(null);
    this.editandoAvaliacaoId.set(av.id);
    this.avaliacaoEdicaoForm.reset({
      nota: av.nota,
      comentario: av.comentario ?? '',
      publica: av.publica,
    });
  }

  protected cancelarEdicaoAvaliacao(): void {
    this.editandoAvaliacaoId.set(null);
  }

  protected salvarEdicaoAvaliacao(av: Avaliacao): void {
    if (this.enviandoAvaliacao()) {
      return;
    }

    const { nota, comentario, publica } = this.avaliacaoEdicaoForm.getRawValue();
    if (nota === null || nota < 0 || nota > 5) {
      this.avaliacaoErro.set('Selecione uma nota entre 0 e 5.');
      return;
    }

    const request: AvaliacaoUpdateRequest = {
      nota,
      comentario: comentario.trim() ? comentario.trim() : undefined,
      publica,
    };

    this.enviandoAvaliacao.set(true);
    this.avaliacaoErro.set(null);

    this.chamadoService.atualizarAvaliacao(this.id, av.id, request).subscribe({
      next: (atualizada) => {
        this.avaliacoes.update((atual) => atual.map((item) => (item.id === atualizada.id ? atualizada : item)));
        this.editandoAvaliacaoId.set(null);
        this.enviandoAvaliacao.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoAvaliacao.set(false);
        this.avaliacaoErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  protected ocultarAvaliacao(av: Avaliacao): void {
    this.chamadoService.ocultarAvaliacao(this.id, av.id, !av.oculta).subscribe({
      next: (atualizada) => {
        this.avaliacoes.update((atual) => atual.map((item) => (item.id === atualizada.id ? atualizada : item)));
      },
      error: (error: HttpErrorResponse) => {
        this.avaliacaoErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  protected apagarAvaliacao(av: Avaliacao): void {
    if (!window.confirm('Tem certeza que deseja apagar esta avaliação? Essa ação não pode ser desfeita.')) {
      return;
    }

    this.chamadoService.apagarAvaliacao(this.id, av.id).subscribe({
      next: () => {
        this.avaliacoes.update((atual) => atual.filter((item) => item.id !== av.id));
        if (this.editandoAvaliacaoId() === av.id) {
          this.editandoAvaliacaoId.set(null);
        }
      },
      error: (error: HttpErrorResponse) => {
        this.avaliacaoErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  // Busca e mantém em memória a miniatura (blob local) de cada anexo de imagem
  // ainda não carregado — o download exige o header Authorization, então não dá
  // para usar a URL do anexo direto num <img src>.
  private carregarThumbsImagens(anexos: Anexo[]): void {
    for (const anexo of anexos) {
      if (!anexo.tipoMime.startsWith('image/') || this.anexoThumbs().has(anexo.id)) {
        continue;
      }

      this.chamadoService.baixarAnexo(this.id, anexo.id).subscribe({
        next: (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          this.anexoThumbObjectUrls.push(objectUrl);
          const safeUrl = this.sanitizer.bypassSecurityTrustUrl(objectUrl);
          this.anexoThumbs.update((atual) => {
            const novo = new Map(atual);
            novo.set(anexo.id, safeUrl);
            return novo;
          });
        },
        error: () => undefined,
      });
    }
  }

  protected anexoThumbUrl(anexoId: number): SafeUrl | null {
    return this.anexoThumbs().get(anexoId) ?? null;
  }

  protected enviarAnexo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    if (!arquivo) {
      return;
    }

    this.enviandoAnexo.set(true);
    this.anexoErro.set(null);

    this.chamadoService.enviarAnexo(this.id, arquivo).subscribe({
      next: (anexo) => {
        this.anexos.update((atual) => [...atual, anexo]);
        this.enviandoAnexo.set(false);
        input.value = '';
        this.carregarThumbsImagens([anexo]);
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoAnexo.set(false);
        this.anexoErro.set(this.mensagemErroAnexo(error));
        input.value = '';
      },
    });
  }

  protected podeSubstituirAnexo(anexo: Anexo): boolean {
    return anexo.autor.id === this.usuarioId || this.ehAdministrador();
  }

  protected substituirAnexo(anexo: Anexo, event: Event): void {
    const input = event.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    if (!arquivo) {
      return;
    }

    this.substituindoAnexoId.set(anexo.id);
    this.anexoErro.set(null);

    this.chamadoService.substituirAnexo(this.id, anexo.id, arquivo).subscribe({
      next: (atualizado) => {
        this.removerThumb(atualizado.id);
        this.atualizarAnexoNaLista(atualizado);
        this.substituindoAnexoId.set(null);
        input.value = '';
        this.carregarThumbsImagens([atualizado]);
      },
      error: (error: HttpErrorResponse) => {
        this.substituindoAnexoId.set(null);
        this.anexoErro.set(this.mensagemErroAcao(error));
        input.value = '';
      },
    });
  }

  private atualizarAnexoNaLista(atualizado: Anexo): void {
    this.anexos.update((atual) => atual.map((item) => (item.id === atualizado.id ? atualizado : item)));
    this.comentarios.update((atual) =>
      atual.map((comentario) => ({
        ...comentario,
        anexos: comentario.anexos.map((item) => (item.id === atualizado.id ? atualizado : item)),
      })),
    );
  }

  private removerThumb(anexoId: number): void {
    if (!this.anexoThumbs().has(anexoId)) {
      return;
    }
    this.anexoThumbs.update((atual) => {
      const novo = new Map(atual);
      novo.delete(anexoId);
      return novo;
    });
  }

  protected baixarAnexo(anexo: Anexo): void {
    this.chamadoService.baixarAnexo(this.id, anexo.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = anexo.nomeArquivo;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.anexoErro.set('Não foi possível baixar o anexo.'),
    });
  }

  protected visualizarAnexo(anexo: Anexo): void {
    this.chamadoService.baixarAnexo(this.id, anexo.id).subscribe({
      next: (blob) => {
        const objectUrl = URL.createObjectURL(blob);
        this.anexoPreview.set({
          nomeArquivo: anexo.nomeArquivo,
          tipoMime: anexo.tipoMime,
          objectUrl,
          imagemUrl: this.sanitizer.bypassSecurityTrustUrl(objectUrl),
          documentoUrl: this.sanitizer.bypassSecurityTrustResourceUrl(objectUrl),
        });
      },
      error: () => this.anexoErro.set('Não foi possível visualizar o anexo.'),
    });
  }

  protected fecharPreview(): void {
    const preview = this.anexoPreview();
    if (preview) {
      URL.revokeObjectURL(preview.objectUrl);
    }
    this.anexoPreview.set(null);
  }

  protected formatarTamanho(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }
    const kb = bytes / 1024;
    if (kb < 1024) {
      return `${kb.toFixed(1)} KB`;
    }
    return `${(kb / 1024).toFixed(1)} MB`;
  }

  private mensagemErroAnexo(error: HttpErrorResponse): string {
    if (error.status === 422) {
      const body = error.error as ErrorResponse;
      if (body?.detail) {
        return body.detail;
      }
    }
    if (error.status === 403) {
      return 'Você não tem permissão para enviar anexos neste chamado.';
    }
    return 'Não foi possível enviar o anexo. Tente novamente.';
  }

  protected assumir(): void {
    this.executarAcao(this.chamadoService.assumir(this.id));
  }

  protected reabrir(): void {
    this.executarAcao(this.chamadoService.reabrir(this.id));
  }

  protected atribuirTecnico(): void {
    const chamado = this.chamado();
    if (!chamado || this.salvando()) {
      return;
    }

    const { idTecnico } = this.atribuirForm.getRawValue();
    if (idTecnico === null || idTecnico === (chamado.tecnico?.id ?? null)) {
      return;
    }

    this.executarAcao(this.chamadoService.atribuir(chamado.id, idTecnico));
  }

  protected encaminharDepartamento(): void {
    const chamado = this.chamado();
    if (!chamado || this.salvando()) {
      return;
    }

    const { idDepartamento } = this.encaminharForm.getRawValue();
    if (idDepartamento === null || idDepartamento === chamado.departamento.id) {
      return;
    }

    this.executarAcao(this.chamadoService.encaminharDepartamento(chamado.id, { idDepartamento }), () => {
      this.encaminharForm.reset({ idDepartamento: null });
    });
  }

  protected devolverAoHelpDesk(): void {
    if (this.salvando()) {
      return;
    }
    if (!window.confirm('Tem certeza que deseja devolver este chamado ao HelpDesk para nova triagem?')) {
      return;
    }

    this.executarAcao(this.chamadoService.devolverAoHelpDesk(this.id));
  }

  protected fecharComoCliente(): void {
    if (this.salvando()) {
      return;
    }
    if (!window.confirm('Tem certeza que deseja fechar este chamado?')) {
      return;
    }

    const { motivo } = this.fecharClienteForm.getRawValue();
    const motivoTratado = motivo.trim() || undefined;

    this.executarAcao(this.chamadoService.fecharComoCliente(this.id, motivoTratado));
  }

  // Abre/fecha o editor de prazo de resolução. Ao abrir, repopula o form com o
  // prazo atual do chamado (nunca fica com um valor "preso" de uma edição
  // anterior cancelada).
  protected toggleEdicaoPrazoResolucao(): void {
    if (this.editandoPrazoResolucao()) {
      this.cancelarEdicaoPrazoResolucao();
      return;
    }

    const chamado = this.chamado();
    if (!chamado) {
      return;
    }

    this.acaoErro.set(null);
    this.prazoForm.reset(
      {
        prazoResolucao: chamado.prazoResolucao ? this.paraDatetimeLocal(chamado.prazoResolucao) : '',
        justificativa: '',
      },
      { emitEvent: false },
    );
    this.editandoPrazoResolucao.set(true);
  }

  protected cancelarEdicaoPrazoResolucao(): void {
    this.editandoPrazoResolucao.set(false);
  }

  protected ajustarPrazo(): void {
    if (this.salvando()) {
      return;
    }

    const chamado = this.chamado();
    if (!chamado) {
      return;
    }

    const { prazoResolucao, justificativa } = this.prazoForm.getRawValue();
    if (!prazoResolucao) {
      this.acaoErro.set('Selecione o novo prazo de resolução.');
      return;
    }
    if (!justificativa.trim()) {
      this.acaoErro.set('Informe a justificativa da alteração de prazo.');
      return;
    }

    const prazoOriginal = chamado.prazoResolucao ? this.paraDatetimeLocal(chamado.prazoResolucao) : '';
    if (prazoResolucao === prazoOriginal) {
      // Justificativa preenchida, mas o prazo em si não mudou — não há o que
      // aplicar, só fecha o editor.
      this.editandoPrazoResolucao.set(false);
      return;
    }

    const request: PrazoResolucaoUpdateRequest = {
      prazoResolucao: new Date(prazoResolucao).toISOString(),
      justificativa: justificativa.trim(),
    };

    this.executarAcao(this.chamadoService.ajustarPrazoResolucao(this.id, request), (atualizado) => {
      this.inicializarFormsDePrazo(atualizado);
      this.editandoPrazoResolucao.set(false);
    });
  }

  protected toggleEdicaoPrazoResposta(): void {
    if (this.editandoPrazoResposta()) {
      this.cancelarEdicaoPrazoResposta();
      return;
    }

    const chamado = this.chamado();
    if (!chamado) {
      return;
    }

    this.acaoErro.set(null);
    this.prazoRespostaForm.reset(
      { prazoResposta: chamado.prazoResposta ? this.paraDatetimeLocal(chamado.prazoResposta) : '' },
      { emitEvent: false },
    );
    this.editandoPrazoResposta.set(true);
  }

  protected cancelarEdicaoPrazoResposta(): void {
    this.editandoPrazoResposta.set(false);
  }

  protected ajustarPrazoResposta(): void {
    if (this.salvando()) {
      return;
    }

    const chamado = this.chamado();
    if (!chamado) {
      return;
    }

    const { prazoResposta } = this.prazoRespostaForm.getRawValue();
    if (!prazoResposta) {
      this.acaoErro.set('Selecione o novo prazo de resposta.');
      return;
    }

    const prazoOriginal = chamado.prazoResposta ? this.paraDatetimeLocal(chamado.prazoResposta) : '';
    if (prazoResposta === prazoOriginal) {
      this.editandoPrazoResposta.set(false);
      return;
    }

    const request: PrazoRespostaUpdateRequest = {
      prazoResposta: new Date(prazoResposta).toISOString(),
    };

    this.executarAcao(this.chamadoService.ajustarPrazoResposta(this.id, request), (atualizado) => {
      this.inicializarFormsDePrazo(atualizado);
      this.editandoPrazoResposta.set(false);
    });
  }

  protected salvarAlteracoes(): void {
    const chamado = this.chamado();
    if (!chamado || this.salvando()) {
      return;
    }

    const { idStatus, idCategoria, idPrioridade } = this.form.getRawValue();

    const atualizacao: ChamadoUpdateRequest = {};
    if (idStatus !== null && idStatus !== chamado.status.id) {
      atualizacao.idStatus = idStatus;
    }
    if (idCategoria !== null && idCategoria !== chamado.categoria.id) {
      atualizacao.idCategoria = idCategoria;
    }
    if (idPrioridade !== null && idPrioridade !== chamado.prioridade.id) {
      atualizacao.idPrioridade = idPrioridade;
    }

    if (Object.keys(atualizacao).length === 0) {
      return;
    }

    this.executarAcao(this.chamadoService.atualizar(chamado.id, atualizacao));
  }

  // Cliente só pode editar o próprio chamado (título/descrição) enquanto ele não
  // estiver num status final — depois disso só staff (admin/técnico) edita.
  protected podeEditarConteudo(): boolean {
    const chamado = this.chamado();
    if (!chamado) {
      return false;
    }
    if (this.ehCliente()) {
      return chamado.solicitante.id === this.usuarioId && !chamado.status.final;
    }
    return this.ehAdministrador() || this.ehTecnico();
  }

  protected podeOcultarDescricao(): boolean {
    return this.ehAdministrador();
  }

  protected iniciarEdicaoConteudo(): void {
    const chamado = this.chamado();
    if (!chamado) {
      return;
    }

    this.acaoErro.set(null);
    this.conteudoForm.reset({ titulo: chamado.titulo, descricao: chamado.descricao });
    this.editandoConteudo.set(true);
  }

  protected cancelarEdicaoConteudo(): void {
    this.editandoConteudo.set(false);
  }

  protected salvarEdicaoConteudo(): void {
    const chamado = this.chamado();
    if (!chamado || this.salvando()) {
      return;
    }

    const { titulo, descricao } = this.conteudoForm.getRawValue();
    if (!titulo.trim() || !descricao.trim()) {
      this.acaoErro.set('Preencha título e descrição.');
      return;
    }

    const request: ChamadoConteudoUpdateRequest = {};
    if (titulo.trim() !== chamado.titulo) {
      request.titulo = titulo.trim();
    }
    if (descricao.trim() !== chamado.descricao) {
      request.descricao = descricao.trim();
    }

    if (Object.keys(request).length === 0) {
      this.editandoConteudo.set(false);
      return;
    }

    this.executarAcao(this.chamadoService.editarConteudoChamado(chamado.id, request), () => {
      this.editandoConteudo.set(false);
    });
  }

  protected ocultarDescricaoChamado(): void {
    const chamado = this.chamado();
    if (!chamado) {
      return;
    }
    this.executarAcao(this.chamadoService.ocultarDescricaoChamado(chamado.id, !chamado.descricaoOculta));
  }

  private executarAcao(acao: Observable<Chamado>, aposSucesso?: (atualizado: Chamado) => void): void {
    if (this.salvando()) {
      return;
    }

    this.salvando.set(true);
    this.acaoErro.set(null);

    acao.subscribe({
      next: (atualizado) => {
        this.aplicarChamado(atualizado);
        aposSucesso?.(atualizado);
        this.salvando.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.salvando.set(false);
        this.acaoErro.set(this.mensagemErroAcao(error));
        // Reaplica o chamado atual (sem a mudança que falhou) para ressincronizar
        // os selects/forms com o estado real — sem isso, o select de status, por
        // exemplo, fica mostrando "Resolvido" mesmo que o servidor tenha recusado.
        const chamadoAtual = this.chamado();
        if (chamadoAtual) {
          this.aplicarChamado(chamadoAtual);
        }
      },
    });
  }

  private carregarChamado(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.chamadoService.detalhar(this.id).subscribe({
      next: (chamado) => {
        this.aplicarChamado(chamado);
        this.inicializarFormsDePrazo(chamado);
        this.carregando.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.erro.set(this.mensagemErroCarregar(error));
        this.carregando.set(false);
      },
    });
  }

  private aplicarChamado(chamado: Chamado): void {
    this.chamado.set(chamado);
    this.form.patchValue(
      {
        idStatus: chamado.status.id,
        idCategoria: chamado.categoria.id,
        idPrioridade: chamado.prioridade.id,
      },
      { emitEvent: false },
    );
    this.atribuirForm.patchValue(
      { idTecnico: chamado.tecnico?.id ?? null },
      { emitEvent: false },
    );

    if (this.ehAdministrador() || this.ehTecnico()) {
      // Reatribuição: só técnicos do departamento responsável (ou administradores)
      // são elegíveis — reconsultado a cada carregamento pra acompanhar o
      // departamento atual do chamado (ex.: após um encaminhamento).
      this.chamadoService
        .listarAtribuiveis(chamado.departamento.id)
        .subscribe({ next: (atribuiveis) => this.tecnicos.set(atribuiveis) });
    }
  }

  // Só chamado no carregamento inicial e logo após um ajuste de prazo bem-
  // sucedido (nunca a cada ação genérica) — outras ações (mudar categoria,
  // atribuir técnico, etc.) nunca alteram prazoResolucao/prazoResposta, então
  // resincronizar esses forms a cada uma delas só apagaria, sem necessidade,
  // uma justificativa que o usuário ainda estivesse digitando.
  private inicializarFormsDePrazo(chamado: Chamado): void {
    this.prazoForm.patchValue(
      {
        prazoResolucao: chamado.prazoResolucao ? this.paraDatetimeLocal(chamado.prazoResolucao) : '',
        justificativa: '',
      },
      { emitEvent: false },
    );
    this.prazoRespostaForm.patchValue(
      { prazoResposta: chamado.prazoResposta ? this.paraDatetimeLocal(chamado.prazoResposta) : '' },
      { emitEvent: false },
    );
  }

  private paraDatetimeLocal(iso: string): string {
    const data = new Date(iso);
    const pad = (valor: number) => String(valor).padStart(2, '0');
    return `${data.getFullYear()}-${pad(data.getMonth() + 1)}-${pad(data.getDate())}T${pad(data.getHours())}:${pad(data.getMinutes())}`;
  }

  private mensagemErroCarregar(error: HttpErrorResponse): string {
    if (error.status === 404) {
      return 'Chamado não encontrado.';
    }
    if (error.status === 403) {
      return 'Você não tem permissão para acessar este chamado.';
    }
    return 'Não foi possível carregar o chamado. Tente novamente em instantes.';
  }

  private mensagemErroAcao(error: HttpErrorResponse): string {
    if (error.status === 422) {
      const body = error.error as ErrorResponse;
      const mensagens = Object.values(body?.errors ?? {}).flat();
      if (mensagens.length > 0) {
        return mensagens.join(' ');
      }
    }
    if (error.status === 409) {
      return 'O chamado já foi alterado por outra ação. Recarregue a página.';
    }
    if (error.status === 403) {
      return 'Você não tem permissão para executar esta ação.';
    }
    return 'Não foi possível concluir a ação. Tente novamente.';
  }
}
