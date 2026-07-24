import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { Observable, Subject, catchError, interval, merge, of, switchMap, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Notificacao } from '../models/notificacao.model';
import { AuthService } from './auth.service';

const INTERVALO_POLLING_MS = 60_000;

@Injectable({ providedIn: 'root' })
export class NotificacaoService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  private readonly _naoLidas = signal(0);
  readonly naoLidas = this._naoLidas.asReadonly();

  // Todo refresh do contador (polling periódico OU disparado por uma ação do
  // usuário, ex.: marcar como lida) passa por este Subject e por switchMap,
  // para que uma resposta antiga e "atrasada" nunca sobrescreva uma mais
  // recente (era a causa do contador às vezes não "limpar" ao clicar).
  private readonly atualizar$ = new Subject<void>();

  // IDs de notificações não lidas já vistos nesta sessão do navegador —
  // usado só para saber quais são "novas" (e merecem toast + som), não para
  // decidir o que está lido/não lido (isso é sempre responsabilidade do servidor).
  private idsConhecidos = new Set<number>();
  private primeiraCarga = true;

  constructor() {
    merge(interval(INTERVALO_POLLING_MS), this.atualizar$)
      .pipe(switchMap(() => (this.authService.isAutenticado() ? this.buscarNaoLidasEAtualizar() : of(null))))
      .subscribe();

    // NotificacaoService é um singleton (providedIn: 'root') que sobrevive a
    // login/logout dentro da mesma aba — sem isto, trocar de usuário sem
    // recarregar a página deixava o contador preso no valor da sessão
    // anterior até o próximo tick do polling (até 60s depois).
    effect(() => {
      const usuario = this.authService.usuario();
      this.idsConhecidos = new Set();
      this.primeiraCarga = true;
      this._naoLidas.set(0);
      if (usuario) {
        this.atualizar$.next();
      }
    });
  }

  listar(apenasNaoLidas = false): Observable<Notificacao[]> {
    return this.http.get<Notificacao[]>(`${environment.apiBaseUrl}/v1/notificacoes`, {
      params: { apenasNaoLidas },
    });
  }

  marcarComoLida(id: number): Observable<void> {
    return this.http
      .patch<void>(`${environment.apiBaseUrl}/v1/notificacoes/${id}/lida`, {})
      .pipe(tap(() => this.atualizar$.next()));
  }

  marcarTodasComoLidas(): Observable<void> {
    return this.http.patch<void>(`${environment.apiBaseUrl}/v1/notificacoes/lidas`, {}).pipe(
      tap(() => {
        this.idsConhecidos = new Set();
        this._naoLidas.set(0);
      }),
    );
  }

  /** Pede a permissão de notificação do navegador/SO. Só chamar a partir de um gesto do usuário (ex.: clique no sino). */
  solicitarPermissaoNotificacao(): void {
    if (typeof Notification === 'undefined' || Notification.permission !== 'default') {
      return;
    }
    Notification.requestPermission().catch(() => undefined);
  }

  private buscarNaoLidasEAtualizar(): Observable<Notificacao[] | null> {
    return this.listar(true).pipe(
      tap((naoLidas) => {
        this._naoLidas.set(naoLidas.length);

        if (this.primeiraCarga) {
          // Não notifica retroativamente o que já estava pendente antes de abrir a página.
          this.primeiraCarga = false;
          this.idsConhecidos = new Set(naoLidas.map((n) => n.id));
          return;
        }

        const novas = naoLidas.filter((n) => !this.idsConhecidos.has(n.id));
        this.idsConhecidos = new Set(naoLidas.map((n) => n.id));
        novas.forEach((notificacao) => this.notificarNovaMensagem(notificacao));
      }),
      catchError(() => of(null)),
    );
  }

  private notificarNovaMensagem(notificacao: Notificacao): void {
    this.tocarSom();

    if (typeof Notification === 'undefined' || Notification.permission !== 'granted') {
      return;
    }

    // silent: true suprime o som/vibração nativos do SO — o único som tocado
    // é o nosso (tocarSom, acima). Sem isso o usuário ouve os dois sons juntos.
    const toast = new Notification('Sistema de Chamados', {
      body: notificacao.mensagem,
      tag: `chamado-${notificacao.idChamado}`,
      silent: true,
    });
    toast.onclick = () => {
      window.focus();
      toast.close();
    };
  }

  /** Toca um "ding" curto sintetizado via Web Audio — sem depender de um arquivo de áudio. */
  private tocarSom(): void {
    try {
      const AudioContextCtor =
        window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      if (!AudioContextCtor) {
        return;
      }

      const contexto = new AudioContextCtor();
      const agora = contexto.currentTime;

      const tocarTom = (frequencia: number, inicio: number, duracao: number) => {
        const oscilador = contexto.createOscillator();
        const ganho = contexto.createGain();
        oscilador.type = 'sine';
        oscilador.frequency.value = frequencia;
        ganho.gain.setValueAtTime(0.0001, agora + inicio);
        ganho.gain.linearRampToValueAtTime(0.18, agora + inicio + 0.02);
        ganho.gain.exponentialRampToValueAtTime(0.0001, agora + inicio + duracao);
        oscilador.connect(ganho);
        ganho.connect(contexto.destination);
        oscilador.start(agora + inicio);
        oscilador.stop(agora + inicio + duracao);
      };

      tocarTom(880, 0, 0.16);
      tocarTom(1318.5, 0.1, 0.22);

      setTimeout(() => void contexto.close().catch(() => undefined), 500);
    } catch {
      // Web Audio indisponível/bloqueado — notificação segue só visual, sem quebrar o app.
    }
  }
}
