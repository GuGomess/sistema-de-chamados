import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, interval, of, startWith, switchMap, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Notificacao, NotificacaoContagem } from '../models/notificacao.model';
import { AuthService } from './auth.service';

const INTERVALO_POLLING_MS = 60_000;

@Injectable({ providedIn: 'root' })
export class NotificacaoService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  private readonly _naoLidas = signal(0);
  readonly naoLidas = this._naoLidas.asReadonly();

  constructor() {
    interval(INTERVALO_POLLING_MS)
      .pipe(
        startWith(0),
        switchMap(() => (this.authService.isAutenticado() ? this.contar() : of(null))),
      )
      .subscribe();
  }

  listar(apenasNaoLidas = false): Observable<Notificacao[]> {
    return this.http.get<Notificacao[]>(`${environment.apiBaseUrl}/v1/notificacoes`, {
      params: { apenasNaoLidas },
    });
  }

  marcarComoLida(id: number): Observable<void> {
    return this.http
      .patch<void>(`${environment.apiBaseUrl}/v1/notificacoes/${id}/lida`, {})
      .pipe(tap(() => this.contar().subscribe()));
  }

  marcarTodasComoLidas(): Observable<void> {
    return this.http
      .patch<void>(`${environment.apiBaseUrl}/v1/notificacoes/lidas`, {})
      .pipe(tap(() => this._naoLidas.set(0)));
  }

  private contar(): Observable<NotificacaoContagem | null> {
    return this.http
      .get<NotificacaoContagem>(`${environment.apiBaseUrl}/v1/notificacoes/nao-lidas/contagem`)
      .pipe(
        tap((resultado) => this._naoLidas.set(resultado.naoLidas)),
        catchError(() => of(null)),
      );
  }
}
