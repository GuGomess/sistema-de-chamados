import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { PerfilCodigo } from '../../core/models/auth.model';

const ROTA_POR_PERFIL: Record<PerfilCodigo, string> = {
  CLIENTE: '/chamados',
  TECNICO: '/dashboard',
  ADMINISTRADOR: '/dashboard',
};

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected onSubmit(): void {
    if (this.form.invalid || this.carregando()) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando.set(true);
    this.erro.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: (auth) => {
        this.carregando.set(false);
        this.router.navigateByUrl(ROTA_POR_PERFIL[auth.usuario.perfil] ?? '/dashboard');
      },
      error: (error: HttpErrorResponse) => {
        this.carregando.set(false);
        this.erro.set(
          error.status === 401
            ? 'E-mail ou senha inválidos.'
            : 'Não foi possível entrar. Tente novamente em instantes.',
        );
      },
    });
  }
}
