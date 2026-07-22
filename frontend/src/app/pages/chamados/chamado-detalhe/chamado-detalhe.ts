import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { concatMap, from, Observable } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { ChamadoService } from '../../../core/services/chamado.service';
import { Chamado, Status, UsuarioResumo } from '../../../core/models/chamado.model';

interface ErrorResponse {
  errors?: Record<string, string[]>;
}

@Component({
  selector: 'app-chamado-detalhe',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './chamado-detalhe.html',
  styleUrl: './chamado-detalhe.scss',
})
export class ChamadoDetalhe implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly chamadoService = inject(ChamadoService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

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

  protected readonly form = this.formBuilder.nonNullable.group({
    idStatus: [null as number | null],
    idTecnico: [null as number | null],
  });

  ngOnInit(): void {
    this.carregarChamado();

    if (this.ehAdministrador() || this.ehTecnico()) {
      this.chamadoService.listarStatus().subscribe({ next: (status) => this.statusList.set(status) });
    }

    if (this.ehAdministrador()) {
      this.chamadoService.listarTecnicos().subscribe({ next: (tecnicos) => this.tecnicos.set(tecnicos) });
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

  protected podeEditar(): boolean {
    const chamado = this.chamado();
    if (!chamado || chamado.status.final) {
      return false;
    }
    return this.ehAdministrador() || (this.ehTecnico() && chamado.tecnico?.id === this.usuarioId);
  }

  protected assumir(): void {
    this.executarAcao(this.chamadoService.assumir(this.id));
  }

  protected liberar(): void {
    this.executarAcao(this.chamadoService.liberar(this.id));
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

    if (this.ehAdministrador() && idTecnico !== null && idTecnico !== (chamado.tecnico?.id ?? null)) {
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
