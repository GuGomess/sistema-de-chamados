import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { PerfilCodigo, Usuario } from '../../../core/models/auth.model';
import { UsuarioService } from '../../../core/services/usuario.service';

@Component({
  selector: 'app-usuarios-admin',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './usuarios-admin.html',
  styleUrl: './usuarios-admin.scss',
})
export class UsuariosAdmin implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly usuarioService = inject(UsuarioService);

  protected readonly usuarios = signal<Usuario[]>([]);
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly criando = signal(false);
  protected readonly erroCriacao = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    nome: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(6)]],
    perfil: ['TECNICO' as PerfilCodigo, [Validators.required]],
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

    this.usuarioService.criar(this.form.getRawValue()).subscribe({
      next: () => {
        this.criando.set(false);
        this.form.reset({ nome: '', email: '', senha: '', perfil: 'TECNICO' });
        this.carregar();
      },
      error: (error: HttpErrorResponse) => {
        this.criando.set(false);
        this.erroCriacao.set(
          error.status === 422
            ? 'Não foi possível criar o usuário. Verifique se o e-mail já está em uso e se a senha é válida.'
            : 'Não foi possível criar o usuário. Tente novamente em instantes.',
        );
      },
    });
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.usuarioService.listar().subscribe({
      next: (usuarios) => {
        this.usuarios.set(usuarios);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.erro.set('Não foi possível carregar os usuários. Tente novamente.');
      },
    });
  }
}
