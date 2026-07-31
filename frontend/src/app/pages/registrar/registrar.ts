import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ROTA_POR_PERFIL } from '../../core/constants/rota-inicial';
import { AuthService } from '../../core/services/auth.service';
import { Icon } from '../../shared/icon/icon';

function senhasIguaisValidator(control: AbstractControl): ValidationErrors | null {
  const senha = control.get('senha')?.value;
  const confirmarSenha = control.get('confirmarSenha')?.value;
  return senha === confirmarSenha ? null : { senhasDiferentes: true };
}

@Component({
  selector: 'app-registrar',
  imports: [ReactiveFormsModule, RouterLink, Icon],
  templateUrl: './registrar.html',
  styleUrl: './registrar.scss',
})
export class Registrar {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      nome: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      senha: ['', [Validators.required, Validators.minLength(6)]],
      confirmarSenha: ['', [Validators.required]],
    },
    { validators: senhasIguaisValidator },
  );

  protected onSubmit(): void {
    if (this.form.invalid || this.carregando()) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando.set(true);
    this.erro.set(null);

    const { nome, email, senha } = this.form.getRawValue();

    this.authService.registrar({ nome, email, senha }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.router.navigateByUrl(ROTA_POR_PERFIL['CLIENTE']);
      },
      error: (error: HttpErrorResponse) => {
        this.carregando.set(false);
        this.erro.set(
          error.status === 422
            ? 'Não foi possível cadastrar. Verifique se o e-mail já está em uso e se a senha é válida.'
            : 'Não foi possível concluir o cadastro. Tente novamente em instantes.',
        );
      },
    });
  }
}
