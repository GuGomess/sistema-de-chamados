import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { Notificacao } from './core/models/notificacao.model';
import { AuthService } from './core/services/auth.service';
import { NotificacaoService } from './core/services/notificacao.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly authService = inject(AuthService);
  protected readonly notificacaoService = inject(NotificacaoService);
  private readonly router = inject(Router);

  protected readonly title = signal('Sistema de Chamados');
  protected readonly naoLidas = this.notificacaoService.naoLidas;
  protected readonly notificacoesAbertas = signal(false);
  protected readonly notificacoes = signal<Notificacao[]>([]);
  protected readonly carregandoNotificacoes = signal(false);

  protected alternarNotificacoes(): void {
    const abrindo = !this.notificacoesAbertas();
    this.notificacoesAbertas.set(abrindo);
    if (abrindo) {
      this.carregarNotificacoes();
    }
  }

  protected abrirNotificacao(notificacao: Notificacao): void {
    this.notificacoesAbertas.set(false);
    if (!notificacao.lida) {
      this.notificacaoService.marcarComoLida(notificacao.id).subscribe();
    }
    this.router.navigate(['/chamados', notificacao.idChamado]);
  }

  protected sair(): void {
    this.authService.logout();
    this.notificacoesAbertas.set(false);
    this.router.navigateByUrl('/login');
  }

  private carregarNotificacoes(): void {
    this.carregandoNotificacoes.set(true);
    this.notificacaoService.listar().subscribe({
      next: (notificacoes) => {
        this.notificacoes.set(notificacoes);
        this.carregandoNotificacoes.set(false);
      },
      error: () => this.carregandoNotificacoes.set(false),
    });
  }
}
