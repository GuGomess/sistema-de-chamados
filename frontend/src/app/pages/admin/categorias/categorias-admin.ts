import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Categoria } from '../../../core/models/chamado.model';
import { ChamadoService } from '../../../core/services/chamado.service';

@Component({
  selector: 'app-categorias-admin',
  imports: [ReactiveFormsModule],
  templateUrl: './categorias-admin.html',
  styleUrl: './categorias-admin.scss',
})
export class CategoriasAdmin implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly chamadoService = inject(ChamadoService);

  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly criando = signal(false);
  protected readonly erroCriacao = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    nome: ['', [Validators.required]],
    descricao: [''],
  });

  ngOnInit(): void {
    this.carregar();
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.criando()) {
      this.form.markAllAsTouched();
      return;
    }

    this.criando.set(true);
    this.erroCriacao.set(null);

    const { nome, descricao } = this.form.getRawValue();
    this.chamadoService.criarCategoria({ nome, descricao: descricao || undefined }).subscribe({
      next: () => {
        this.criando.set(false);
        this.form.reset({ nome: '', descricao: '' });
        this.carregar();
      },
      error: (error: HttpErrorResponse) => {
        this.criando.set(false);
        this.erroCriacao.set(
          error.status === 422
            ? 'Não foi possível criar a categoria. Verifique se o nome já está em uso.'
            : 'Não foi possível criar a categoria. Tente novamente em instantes.',
        );
      },
    });
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.chamadoService.listarCategorias().subscribe({
      next: (categorias) => {
        this.categorias.set(categorias);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.erro.set('Não foi possível carregar as categorias. Tente novamente.');
      },
    });
  }
}
