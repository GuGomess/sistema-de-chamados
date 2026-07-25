import { Injectable, effect, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Cliente do Hub SignalR "/hubs/chamados" — conecta enquanto houver um
 * usuário autenticado e desconecta no logout. Consumidores se inscrevem nos
 * eventos do servidor via on() e (para telas de detalhe) entram/saem do
 * grupo de um chamado específico via entrarChamado()/sairChamado().
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly authService = inject(AuthService);

  private connection: HubConnection | null = null;

  // Todos os listeners já pedidos via on(), para poderem ser reaplicados a
  // cada nova conexão criada (ex.: logout seguido de login gera uma nova
  // instância de HubConnection) — não só à primeira, ainda inexistente.
  private readonly listenersRegistrados: Array<{ evento: string; handler: (payload: unknown) => void }> = [];

  constructor() {
    // Mesmo padrão do NotificacaoService: reage à troca de usuário (login/logout
    // na mesma aba, sem recarregar a página) via effect() sobre o signal usuario().
    effect(() => {
      const usuario = this.authService.usuario();
      if (usuario) {
        this.conectar();
      } else {
        this.desconectar();
      }
    });
  }

  /** Registra um handler para um evento do hub (ex.: "ChamadoAtualizado", "NotificacaoRecebida"). */
  on<T>(evento: string, handler: (payload: T) => void): void {
    this.listenersRegistrados.push({ evento, handler: handler as (payload: unknown) => void });
    this.connection?.on(evento, handler);
  }

  /** Entra no grupo "chamado-{id}" — chamar ao abrir a tela de detalhe de um chamado. */
  entrarChamado(chamadoId: number): void {
    this.invocarComSeguranca('EntrarChamado', chamadoId);
  }

  /** Sai do grupo "chamado-{id}" — chamar ao fechar/destruir a tela de detalhe. */
  sairChamado(chamadoId: number): void {
    this.invocarComSeguranca('SairChamado', chamadoId);
  }

  private conectar(): void {
    if (this.connection) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.authService.getToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    for (const { evento, handler } of this.listenersRegistrados) {
      connection.on(evento, handler);
    }

    this.connection = connection;
    connection.start().catch((erro) => console.error('Falha ao conectar no hub de chamados.', erro));
  }

  private desconectar(): void {
    const connection = this.connection;
    if (!connection) {
      return;
    }
    this.connection = null;
    connection.stop().catch(() => undefined);
  }

  /** Invoca um método do hub sem lançar caso a conexão ainda não esteja pronta. */
  private invocarComSeguranca(metodo: string, ...args: unknown[]): void {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }
    this.connection.invoke(metodo, ...args).catch((erro) => console.error(`Falha ao invocar ${metodo} no hub.`, erro));
  }
}
