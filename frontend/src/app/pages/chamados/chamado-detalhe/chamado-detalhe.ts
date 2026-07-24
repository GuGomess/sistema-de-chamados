import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl, SafeUrl } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { concatMap, from, Observable } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { ChamadoService } from '../../../core/services/chamado.service';
import {
  Anexo,
  Chamado,
  Comentario,
  ComentarioCreateRequest,
  PrazoResolucaoUpdateRequest,
  PrazoRespostaUpdateRequest,
  SituacaoSla,
  Status,
  UsuarioResumo,
} from '../../../core/models/chamado.model';

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
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly sanitizer = inject(DomSanitizer);

  private readonly id = Number(this.route.snapshot.paramMap.get('id'));
  private readonly perfil = this.authService.getPerfil();
  private readonly usuarioId = this.authService.getUsuario()?.id ?? null;

  protected readonly chamado = signal<Chamado | null>(null);
  protected readonly statusList = signal<Status[]>([]);
  protected readonly tecnicos = signal<UsuarioResumo[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly salvando = signal(false);
  protected readonly acaoErro = signal<string | null>(null);

  protected readonly comentarios = signal<Comentario[]>([]);
  protected readonly carregandoComentarios = signal(true);
  protected readonly enviandoComentario = signal(false);
  protected readonly comentarioErro = signal<string | null>(null);

  protected readonly anexos = signal<Anexo[]>([]);
  protected readonly carregandoAnexos = signal(true);
  protected readonly enviandoAnexo = signal(false);
  protected readonly anexoErro = signal<string | null>(null);
  protected readonly anexoPreview = signal<AnexoPreview | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    idStatus: [null as number | null],
    idTecnico: [null as number | null],
  });

  protected readonly comentarioForm = this.formBuilder.nonNullable.group({
    mensagem: [''],
    interno: [false],
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

    if (this.ehAdministrador() || this.ehTecnico()) {
      this.chamadoService.listarStatus().subscribe({ next: (status) => this.statusList.set(status) });
    }

    if (this.ehAdministrador()) {
      this.chamadoService.listarTecnicos().subscribe({ next: (tecnicos) => this.tecnicos.set(tecnicos) });
    }
  }

  ngOnDestroy(): void {
    const preview = this.anexoPreview();
    if (preview) {
      URL.revokeObjectURL(preview.objectUrl);
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

  protected podeAssumir(): boolean {
    const chamado = this.chamado();
    return this.ehTecnico() && !!chamado && !chamado.tecnico && !chamado.status.final;
  }

  protected podeLiberar(): boolean {
    const chamado = this.chamado();
    if (!chamado || !chamado.tecnico) {
      return false;
    }
    return this.ehAdministrador() || (this.ehTecnico() && chamado.tecnico.id === this.usuarioId);
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
    return this.ehAdministrador() && !!chamado && !chamado.status.final;
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
    };

    this.enviandoComentario.set(true);
    this.comentarioErro.set(null);

    this.chamadoService.criarComentario(this.id, request).subscribe({
      next: (comentario) => {
        this.comentarios.update((atual) => [...atual, comentario]);
        this.comentarioForm.reset({ mensagem: '', interno: false });
        this.enviandoComentario.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoComentario.set(false);
        this.comentarioErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  private carregarComentarios(): void {
    this.carregandoComentarios.set(true);

    this.chamadoService.listarComentarios(this.id).subscribe({
      next: (comentarios) => {
        this.comentarios.set(comentarios);
        this.carregandoComentarios.set(false);
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
      },
      error: () => {
        this.carregandoAnexos.set(false);
      },
    });
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
      },
      error: (error: HttpErrorResponse) => {
        this.enviandoAnexo.set(false);
        this.anexoErro.set(this.mensagemErroAnexo(error));
        input.value = '';
      },
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

  protected podeVisualizar(anexo: Anexo): boolean {
    return anexo.tipoMime.startsWith('image/') || anexo.tipoMime === 'application/pdf';
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

  protected liberar(): void {
    this.executarAcao(this.chamadoService.liberar(this.id));
  }

  protected ajustarPrazo(): void {
    if (this.salvando()) {
      return;
    }

    const { prazoResolucao, justificativa } = this.prazoForm.getRawValue();
    if (!prazoResolucao || !justificativa.trim()) {
      this.acaoErro.set('Informe o novo prazo e uma justificativa.');
      return;
    }

    const request: PrazoResolucaoUpdateRequest = {
      prazoResolucao: new Date(prazoResolucao).toISOString(),
      justificativa: justificativa.trim(),
    };

    this.executarAcao(this.chamadoService.ajustarPrazoResolucao(this.id, request));
  }

  protected ajustarPrazoResposta(): void {
    if (this.salvando()) {
      return;
    }

    const { prazoResposta } = this.prazoRespostaForm.getRawValue();
    if (!prazoResposta) {
      this.acaoErro.set('Informe o novo prazo de resposta.');
      return;
    }

    const request: PrazoRespostaUpdateRequest = {
      prazoResposta: new Date(prazoResposta).toISOString(),
    };

    this.executarAcao(this.chamadoService.ajustarPrazoResposta(this.id, request));
  }

  protected salvarAlteracoes(): void {
    const chamado = this.chamado();
    if (!chamado || this.salvando()) {
      return;
    }

    const { idStatus, idTecnico } = this.form.getRawValue();
    const acoes: Observable<Chamado>[] = [];

    if (idStatus !== null && idStatus !== chamado.status.id) {
      acoes.push(this.chamadoService.atualizar(chamado.id, { idStatus }));
    }

    if (this.podeAtribuirTecnico() && idTecnico !== null && idTecnico !== (chamado.tecnico?.id ?? null)) {
      acoes.push(this.chamadoService.atribuir(chamado.id, idTecnico));
    }

    if (acoes.length === 0) {
      return;
    }

    this.salvando.set(true);
    this.acaoErro.set(null);

    from(acoes)
      .pipe(concatMap((acao) => acao))
      .subscribe({
        next: (atualizado) => this.aplicarChamado(atualizado),
        error: (error: HttpErrorResponse) => {
          this.salvando.set(false);
          this.acaoErro.set(this.mensagemErroAcao(error));
        },
        complete: () => this.salvando.set(false),
      });
  }

  private executarAcao(acao: Observable<Chamado>): void {
    if (this.salvando()) {
      return;
    }

    this.salvando.set(true);
    this.acaoErro.set(null);

    acao.subscribe({
      next: (atualizado) => {
        this.aplicarChamado(atualizado);
        this.salvando.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.salvando.set(false);
        this.acaoErro.set(this.mensagemErroAcao(error));
      },
    });
  }

  private carregarChamado(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.chamadoService.detalhar(this.id).subscribe({
      next: (chamado) => {
        this.aplicarChamado(chamado);
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
      { idStatus: chamado.status.id, idTecnico: chamado.tecnico?.id ?? null },
      { emitEvent: false },
    );
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
