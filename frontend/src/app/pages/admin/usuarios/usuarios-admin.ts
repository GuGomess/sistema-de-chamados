import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { PerfilCodigo, Usuario } from '../../../core/models/auth.model';
import { Departamento } from '../../../core/models/departamento.model';
import { AuthService } from '../../../core/services/auth.service';
import { DepartamentoService } from '../../../core/services/departamento.service';
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
  private readonly departamentoService = inject(DepartamentoService);
  private readonly authService = inject(AuthService);

  protected readonly usuarioAtualId = this.authService.getUsuario()?.id ?? null;

  protected readonly usuarios = signal<Usuario[]>([]);
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly criando = signal(false);
  protected readonly erroCriacao = signal<string | null>(null);

  protected readonly departamentosAtivos = signal<Departamento[]>([]);
  protected readonly departamentosSelecionadosCriacao = signal<number[]>([]);

  protected readonly gerenciandoDepartamentosId = signal<number | null>(null);
  protected readonly departamentosSelecionadosGerenciar = signal<number[]>([]);
  protected readonly salvandoDepartamentos = signal(false);
  protected readonly erroDepartamentos = signal<string | null>(null);

  protected readonly alterandoAtivoId = signal<number | null>(null);

  protected readonly alterandoSenhaId = signal<number | null>(null);
  protected readonly salvandoSenha = signal(false);
  protected readonly erroSenha = signal<string | null>(null);

  protected readonly senhaForm = this.formBuilder.nonNullable.group({
    novaSenha: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    nome: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(6)]],
    perfil: ['TECNICO' as PerfilCodigo, [Validators.required]],
  });

  ngOnInit(): void {
    this.carregar();
    this.carregarDepartamentosAtivos();
  }

  protected perfilCriacaoEhTecnico(): boolean {
    return this.form.controls.perfil.value === 'TECNICO';
  }

  protected departamentoSelecionadoNaCriacao(idDepartamento: number): boolean {
    return this.departamentosSelecionadosCriacao().includes(idDepartamento);
  }

  protected alternarDepartamentoCriacao(idDepartamento: number, event: Event): void {
    const selecionado = (event.target as HTMLInputElement).checked;
    this.departamentosSelecionadosCriacao.update((atual) =>
      selecionado ? [...atual, idDepartamento] : atual.filter((id) => id !== idDepartamento),
    );
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.criando()) {
      this.form.markAllAsTouched();
      return;
    }

    this.criando.set(true);
    this.erroCriacao.set(null);

    const idsDepartamentos = this.departamentosSelecionadosCriacao();

    this.usuarioService.criar(this.form.getRawValue()).subscribe({
      next: (novoUsuario) => {
        if (idsDepartamentos.length > 0) {
          this.usuarioService.atualizarDepartamentos(novoUsuario.id, idsDepartamentos).subscribe({
            next: () => {
              this.criando.set(false);
              this.finalizarCriacao();
            },
            error: () => {
              this.criando.set(false);
              this.finalizarCriacao();
            },
          });
        } else {
          this.criando.set(false);
          this.finalizarCriacao();
        }
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

  protected iniciarGerenciarDepartamentos(usuario: Usuario): void {
    this.erroDepartamentos.set(null);
    this.departamentosSelecionadosGerenciar.set(usuario.departamentos.map((d) => d.id));
    this.gerenciandoDepartamentosId.set(usuario.id);
  }

  protected cancelarGerenciarDepartamentos(): void {
    this.gerenciandoDepartamentosId.set(null);
  }

  protected departamentoSelecionadoNoGerenciamento(idDepartamento: number): boolean {
    return this.departamentosSelecionadosGerenciar().includes(idDepartamento);
  }

  protected alternarDepartamentoGerenciamento(idDepartamento: number, event: Event): void {
    const selecionado = (event.target as HTMLInputElement).checked;
    this.departamentosSelecionadosGerenciar.update((atual) =>
      selecionado ? [...atual, idDepartamento] : atual.filter((id) => id !== idDepartamento),
    );
  }

  protected salvarDepartamentos(usuario: Usuario): void {
    if (this.salvandoDepartamentos()) {
      return;
    }

    this.salvandoDepartamentos.set(true);
    this.erroDepartamentos.set(null);

    this.usuarioService.atualizarDepartamentos(usuario.id, this.departamentosSelecionadosGerenciar()).subscribe({
      next: () => {
        this.salvandoDepartamentos.set(false);
        this.gerenciandoDepartamentosId.set(null);
        this.carregar();
      },
      error: () => {
        this.salvandoDepartamentos.set(false);
        this.erroDepartamentos.set('Não foi possível salvar os departamentos. Tente novamente.');
      },
    });
  }

  protected alternarAtivo(usuario: Usuario): void {
    if (this.alterandoAtivoId()) {
      return;
    }

    const acao = usuario.ativo ? 'desativar' : 'ativar';
    if (!window.confirm(`Tem certeza que deseja ${acao} o usuário ${usuario.nome}?`)) {
      return;
    }

    this.alterandoAtivoId.set(usuario.id);
    this.usuarioService.alterarAtivo(usuario.id, !usuario.ativo).subscribe({
      next: () => {
        this.alterandoAtivoId.set(null);
        this.carregar();
      },
      error: () => {
        this.alterandoAtivoId.set(null);
        this.erro.set(`Não foi possível ${acao} o usuário. Tente novamente.`);
      },
    });
  }

  protected iniciarAlterarSenha(usuario: Usuario): void {
    this.erroSenha.set(null);
    this.senhaForm.reset({ novaSenha: '' });
    this.alterandoSenhaId.set(usuario.id);
  }

  protected cancelarAlterarSenha(): void {
    this.alterandoSenhaId.set(null);
  }

  protected salvarSenha(usuario: Usuario): void {
    if (this.senhaForm.invalid || this.salvandoSenha()) {
      this.senhaForm.markAllAsTouched();
      return;
    }

    this.salvandoSenha.set(true);
    this.erroSenha.set(null);

    const { novaSenha } = this.senhaForm.getRawValue();
    this.usuarioService.alterarSenha(usuario.id, novaSenha).subscribe({
      next: () => {
        this.salvandoSenha.set(false);
        this.alterandoSenhaId.set(null);
      },
      error: () => {
        this.salvandoSenha.set(false);
        this.erroSenha.set('Não foi possível alterar a senha. Tente novamente.');
      },
    });
  }

  private finalizarCriacao(): void {
    this.form.reset({ nome: '', email: '', senha: '', perfil: 'TECNICO' });
    this.departamentosSelecionadosCriacao.set([]);
    this.carregar();
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

  private carregarDepartamentosAtivos(): void {
    this.departamentoService.listar(true).subscribe({
      next: (departamentos) => this.departamentosAtivos.set(departamentos),
      error: () => {},
    });
  }
}
