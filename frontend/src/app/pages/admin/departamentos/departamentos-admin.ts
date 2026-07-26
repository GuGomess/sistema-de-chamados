import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Departamento } from '../../../core/models/departamento.model';
import { DepartamentoService } from '../../../core/services/departamento.service';

@Component({
  selector: 'app-departamentos-admin',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './departamentos-admin.html',
  styleUrl: './departamentos-admin.scss',
})
export class DepartamentosAdmin implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly departamentoService = inject(DepartamentoService);

  protected readonly departamentos = signal<Departamento[]>([]);
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly criando = signal(false);
  protected readonly erroCriacao = signal<string | null>(null);

  protected readonly editandoId = signal<number | null>(null);
  protected readonly salvandoEdicao = signal(false);
  protected readonly erroEdicao = signal<string | null>(null);

  protected readonly alterandoStatusId = signal<number | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    nome: ['', [Validators.required]],
    descricao: [''],
  });

  protected readonly editForm = this.formBuilder.nonNullable.group({
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
    this.departamentoService.criar({ nome, descricao: descricao || undefined }).subscribe({
      next: () => {
        this.criando.set(false);
        this.form.reset({ nome: '', descricao: '' });
        this.carregar();
      },
      error: (error: HttpErrorResponse) => {
        this.criando.set(false);
        this.erroCriacao.set(
          error.status === 422
            ? 'Não foi possível criar o departamento. Verifique se o nome já está em uso.'
            : 'Não foi possível criar o departamento. Tente novamente em instantes.',
        );
      },
    });
  }

  protected iniciarEdicao(departamento: Departamento): void {
    this.erroEdicao.set(null);
    this.editForm.reset({ nome: departamento.nome, descricao: departamento.descricao ?? '' });
    this.editandoId.set(departamento.id);
  }

  protected cancelarEdicao(): void {
    this.editandoId.set(null);
  }

  protected salvarEdicao(departamento: Departamento): void {
    if (this.editForm.invalid || this.salvandoEdicao()) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.salvandoEdicao.set(true);
    this.erroEdicao.set(null);

    const { nome, descricao } = this.editForm.getRawValue();
    // Diferente da criação: aqui precisa distinguir "não alterar" de "limpar"
    // — envia sempre a string (mesmo vazia), já que o backend só ignora
    // descricao quando o campo vem null/ausente.
    this.departamentoService.editar(departamento.id, { nome, descricao: descricao.trim() }).subscribe({
      next: () => {
        this.salvandoEdicao.set(false);
        this.editandoId.set(null);
        this.carregar();
      },
      error: (error: HttpErrorResponse) => {
        this.salvandoEdicao.set(false);
        this.erroEdicao.set(
          error.status === 422
            ? 'Não foi possível salvar. Verifique se o nome já está em uso.'
            : 'Não foi possível salvar as alterações. Tente novamente em instantes.',
        );
      },
    });
  }

  protected alternarStatus(departamento: Departamento): void {
    if (this.alterandoStatusId()) {
      return;
    }

    this.alterandoStatusId.set(departamento.id);
    this.departamentoService.definirStatus(departamento.id, !departamento.ativo).subscribe({
      next: () => {
        this.alterandoStatusId.set(null);
        this.carregar();
      },
      error: () => {
        this.alterandoStatusId.set(null);
        this.erro.set('Não foi possível alterar o status do departamento. Tente novamente.');
      },
    });
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.departamentoService.listar().subscribe({
      next: (departamentos) => {
        this.departamentos.set(departamentos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.erro.set('Não foi possível carregar os departamentos. Tente novamente.');
      },
    });
  }
}
