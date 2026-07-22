import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';

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
  private readonly router = inject(Router);

  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly prioridades = signal<Prioridade[]>([]);
  protected readonly carregandoOpcoes = signal(true);
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    titulo: ['', [Validators.required, Validators.maxLength(160)]],
    idCategoria: [null as number | null, [Validators.required]],
    idPrioridade: [null as number | null, [Validators.required]],
    descricao: ['', [Validators.required]],
  });

  ngOnInit(): void {
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
        idCategoria: idCategoria!,
        idPrioridade: idPrioridade!,
      })
      .subscribe({
        next: (chamado) => {
          this.enviando.set(false);
          this.router.navigate(['/chamados', chamado.id]);
        },
        error: (error: HttpErrorResponse) => {
          this.enviando.set(false);
          this.erro.set(this.mensagemErro(error));
        },
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
