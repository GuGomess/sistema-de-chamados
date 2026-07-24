import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { ChamadoService } from '../../../core/services/chamado.service';
import { Categoria, Prioridade } from '../../../core/models/chamado.model';

interface ErrorResponse {
  errors?: Record<string, string[]>;
}

@Component({
  selector: 'app-chamado-novo',
  imports: [ReactiveFormsModule],
  templateUrl: './chamado-novo.html',
  styleUrl: './chamado-novo.scss',
})
export class ChamadoNovo implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly chamadoService = inject(ChamadoService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // Cliente não classifica o próprio chamado: categoria/prioridade ficam a
  // cargo do técnico/administrador na triagem (servidor aplica defaults).
  protected readonly ehCliente = this.authService.getPerfil() === 'CLIENTE';

  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly prioridades = signal<Prioridade[]>([]);
  protected readonly carregandoOpcoes = signal(!this.ehCliente);
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly arquivos = signal<File[]>([]);

  protected readonly form = this.formBuilder.nonNullable.group({
    titulo: ['', [Validators.required, Validators.maxLength(160)]],
    idCategoria: [null as number | null, this.ehCliente ? [] : [Validators.required]],
    idPrioridade: [null as number | null, this.ehCliente ? [] : [Validators.required]],
    descricao: ['', [Validators.required]],
  });

  ngOnInit(): void {
    if (this.ehCliente) {
      return;
    }

    forkJoin({
      categorias: this.chamadoService.listarCategorias(),
      prioridades: this.chamadoService.listarPrioridades(),
    }).subscribe({
      next: ({ categorias, prioridades }) => {
        this.categorias.set(categorias);
        this.prioridades.set(prioridades);
        this.carregandoOpcoes.set(false);
      },
      error: () => {
        this.erro.set('Não foi possível carregar categorias e prioridades. Tente novamente.');
        this.carregandoOpcoes.set(false);
      },
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.erro.set(null);

    const { titulo, idCategoria, idPrioridade, descricao } = this.form.getRawValue();

    this.chamadoService
      .criar({
        titulo,
        descricao,
        idCategoria: idCategoria ?? undefined,
        idPrioridade: idPrioridade ?? undefined,
      })
      .subscribe({
        next: (chamado) => this.enviarAnexosEIrParaDetalhe(chamado.id),
        error: (error: HttpErrorResponse) => {
          this.enviando.set(false);
          this.erro.set(this.mensagemErro(error));
        },
      });
  }

  protected selecionarArquivos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const novos = Array.from(input.files ?? []);
    this.arquivos.update((atual) => [...atual, ...novos]);
    input.value = '';
  }

  protected removerArquivo(arquivo: File): void {
    this.arquivos.update((atual) => atual.filter((item) => item !== arquivo));
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

  private enviarAnexosEIrParaDetalhe(idChamado: number): void {
    const arquivos = this.arquivos();
    if (arquivos.length === 0) {
      this.enviando.set(false);
      this.router.navigate(['/chamados', idChamado]);
      return;
    }

    const uploads = arquivos.map((arquivo) =>
      this.chamadoService.enviarAnexo(idChamado, arquivo).pipe(catchError(() => of(null))),
    );

    forkJoin(uploads).subscribe(() => {
      this.enviando.set(false);
      this.router.navigate(['/chamados', idChamado]);
    });
  }

  private mensagemErro(error: HttpErrorResponse): string {
    if (error.status === 422) {
      const body = error.error as ErrorResponse;
      const mensagens = Object.values(body?.errors ?? {}).flat();
      if (mensagens.length > 0) {
        return mensagens.join(' ');
      }
    }

    return 'Não foi possível abrir o chamado. Tente novamente em instantes.';
  }
}
