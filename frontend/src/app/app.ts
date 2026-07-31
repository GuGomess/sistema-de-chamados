import { Component, ElementRef, HostListener, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { Notificacao } from './core/models/notificacao.model';
import { AuthService } from './core/services/auth.service';
import { NotificacaoService } from './core/services/notificacao.service';
import { RealtimeService } from './core/services/realtime.service';
import { ThemeService } from './core/services/theme.service';
import { ToastService } from './core/services/toast.service';
import { Icon } from './shared/icon/icon';
import { ToastHost } from './shared/toast/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon, ToastHost],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly authService = inject(AuthService);
  protected readonly notificacaoService = inject(NotificacaoService);
  protected readonly themeService = inject(ThemeService);
  private readonly toastService = inject(ToastService);
  private readonly realtimeService = inject(RealtimeService);
  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  protected readonly title = signal('Sistema de Chamados');
  protected readonly naoLidas = this.notificacaoService.naoLidas;
  protected readonly notificacoesAbertas = signal(false);
  protected readonly notificacoes = signal<Notificacao[]>([]);
  protected readonly carregandoNotificacoes = signal(false);
  // Sidebar em telas estreitas começa fechada (overlay) — em telas largas ela
  // é sempre visível via CSS, esse estado só importa abaixo do breakpoint.
  protected readonly menuAberto = signal(false);

  constructor() {
    // App é o componente raiz (sobrevive a login/logout na mesma aba) — ponto
    // certo pra reagir a um evento que pode chegar em qualquer tela, não só
    // dentro de uma página específica.
    this.realtimeService.on('ContaDesativada', () => this.aoContaDesativada());
  }

  // Métodos (não campos fixados no construtor): o componente raiz não é
  // recriado entre login/logout na mesma sessão de SPA, então precisam ler o
  // usuário atual (via signal do AuthService) a cada checagem, não uma vez só.
  protected nomeUsuario(): string | null {
    return this.authService.usuario()?.nome ?? null;
  }

  protected ehCliente(): boolean {
    return this.authService.usuario()?.perfil === 'CLIENTE';
  }

  protected ehAdministrador(): boolean {
    return this.authService.usuario()?.perfil === 'ADMINISTRADOR';
  }

  protected labelChamados(): string {
    return this.ehCliente() ? 'Meus chamados' : 'Chamados';
  }

  protected alternarNotificacoes(): void {
    const abrindo = !this.notificacoesAbertas();
    this.notificacoesAbertas.set(abrindo);
    if (abrindo) {
      // Gesto do usuário (clique) — ponto certo para pedir a permissão do SO.
      this.notificacaoService.solicitarPermissaoNotificacao();
      this.carregarNotificacoes();
    }
  }

  // Clicar fora do sino/dropdown também fecha — o próprio clique no sino
  // (que já alterna o estado em alternarNotificacoes) fica dentro do elemento,
  // então não é fechado e reaberto pelo mesmo clique.
  @HostListener('document:click', ['$event'])
  protected aoClicarFora(event: MouseEvent): void {
    if (!this.notificacoesAbertas()) {
      return;
    }
    const alvo = event.target as Node;
    if (!this.elementRef.nativeElement.querySelector('.app-header__notificacoes')?.contains(alvo)) {
      this.notificacoesAbertas.set(false);
    }
  }

  protected abrirNotificacao(notificacao: Notificacao): void {
    this.notificacoesAbertas.set(false);
    if (!notificacao.lida) {
      this.notificacaoService.marcarComoLida(notificacao.id).subscribe();
      this.notificacoes.update((atual) =>
        atual.map((n) => (n.id === notificacao.id ? { ...n, lida: true } : n)),
      );
    }
    this.router.navigate(['/chamados', notificacao.idChamado]);
  }

  protected marcarTodasComoLidas(): void {
    this.notificacaoService.marcarTodasComoLidas().subscribe(() => {
      this.notificacoes.update((atual) => atual.map((n) => ({ ...n, lida: true })));
    });
  }

  protected sair(): void {
    this.authService.logout();
    this.notificacoesAbertas.set(false);
    this.router.navigateByUrl('/login');
  }

  private aoContaDesativada(): void {
    this.authService.logout();
    this.notificacoesAbertas.set(false);
    this.router.navigateByUrl('/login');
    this.toastService.mostrar('Sua conta foi desativada por um administrador.', 'erro');
  }

  protected alternarMenu(): void {
    this.menuAberto.update((atual) => !atual);
  }

  protected fecharMenu(): void {
    this.menuAberto.set(false);
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
