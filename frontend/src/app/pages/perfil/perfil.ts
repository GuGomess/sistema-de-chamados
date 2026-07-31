import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AuthService } from '../../core/services/auth.service';
import { UsuarioService } from '../../core/services/usuario.service';
import { Icon } from '../../shared/icon/icon';

interface ErrorResponse {
  errors?: Record<string, string[]>;
}

@Component({
  selector: 'app-perfil',
  imports: [ReactiveFormsModule, Icon],
  templateUrl: './perfil.html',
  styleUrl: './perfil.scss',
})
export class Perfil {
  private readonly formBuilder = inject(FormBuilder);
  private readonly usuarioService = inject(UsuarioService);
  private readonly authService = inject(AuthService);

  protected readonly salvandoDados = signal(false);
  protected readonly erroDados = signal<string | null>(null);
  protected readonly sucessoDados = signal(false);

  protected readonly salvandoSenha = signal(false);
  protected readonly erroSenha = signal<string | null>(null);
  protected readonly sucessoSenha = signal(false);

  private readonly usuarioAtual = this.authService.getUsuario();

  protected readonly dadosForm = this.formBuilder.nonNullable.group({
    nome: [this.usuarioAtual?.nome ?? '', [Validators.required]],
    email: [this.usuarioAtual?.email ?? '', [Validators.required, Validators.email]],
  });

  protected readonly senhaForm = this.formBuilder.nonNullable.group({
    senhaAtual: ['', [Validators.required]],
    novaSenha: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected salvarDados(): void {
    if (this.dadosForm.invalid || this.salvandoDados()) {
      this.dadosForm.markAllAsTouched();
      return;
    }

    this.salvandoDados.set(true);
    this.erroDados.set(null);
    this.sucessoDados.set(false);

    const { nome, email } = this.dadosForm.getRawValue();
    this.usuarioService.atualizarMeuPerfil({ nome, email }).subscribe({
      next: (usuario) => {
        this.authService.atualizarUsuarioLocal(usuario);
        this.salvandoDados.set(false);
        this.sucessoDados.set(true);
      },
      error: (error: HttpErrorResponse) => {
        this.salvandoDados.set(false);
        this.erroDados.set(this.mensagemErro(error, 'Não foi possível salvar seus dados. Tente novamente.'));
      },
    });
  }

  protected salvarSenha(): void {
    if (this.senhaForm.invalid || this.salvandoSenha()) {
      this.senhaForm.markAllAsTouched();
      return;
    }

    this.salvandoSenha.set(true);
    this.erroSenha.set(null);
    this.sucessoSenha.set(false);

    const { senhaAtual, novaSenha } = this.senhaForm.getRawValue();
    this.usuarioService.alterarMinhaSenha({ senhaAtual, novaSenha }).subscribe({
      next: () => {
        this.salvandoSenha.set(false);
        this.sucessoSenha.set(true);
        this.senhaForm.reset({ senhaAtual: '', novaSenha: '' });
      },
      error: (error: HttpErrorResponse) => {
        this.salvandoSenha.set(false);
        this.erroSenha.set(this.mensagemErro(error, 'Não foi possível alterar sua senha. Tente novamente.'));
      },
    });
  }

  private mensagemErro(error: HttpErrorResponse, fallback: string): string {
    if (error.status === 422) {
      const body = error.error as ErrorResponse;
      const mensagens = Object.values(body?.errors ?? {}).flat();
      if (mensagens.length > 0) {
        return mensagens.join(' ');
      }
    }
    return fallback;
  }
}
