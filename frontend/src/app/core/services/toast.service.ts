import { Injectable, signal } from '@angular/core';

export type ToastTipo = 'info' | 'erro' | 'sucesso';

export interface Toast {
  id: number;
  mensagem: string;
  tipo: ToastTipo;
}

const DURACAO_MS = 6000;

/**
 * Substitui window.alert() por uma notificação in-app não bloqueante. Hoje
 * usado só pelo aviso de conta desativada (ver app.ts), mas fica disponível
 * pra qualquer tela que precise avisar o usuário sem travar a UI com um
 * alert() nativo do navegador.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();
  private proximoId = 1;

  mostrar(mensagem: string, tipo: ToastTipo = 'info'): void {
    const id = this.proximoId++;
    this._toasts.update((atual) => [...atual, { id, mensagem, tipo }]);
    setTimeout(() => this.remover(id), DURACAO_MS);
  }

  remover(id: number): void {
    this._toasts.update((atual) => atual.filter((t) => t.id !== id));
  }
}
