import { Component, inject } from '@angular/core';

import { ToastService } from '../../core/services/toast.service';
import { Icon } from '../icon/icon';

@Component({
  selector: 'app-toast-host',
  imports: [Icon],
  template: `
    @if (toastService.toasts().length > 0) {
      <div class="toast-host" role="region" aria-label="Notificações">
        @for (toast of toastService.toasts(); track toast.id) {
          <div class="toast toast--{{ toast.tipo }}" role="alert">
            <app-icon [name]="iconePorTipo(toast.tipo)" />
            <span>{{ toast.mensagem }}</span>
            <button
              type="button"
              class="icon-btn"
              (click)="toastService.remover(toast.id)"
              aria-label="Fechar notificação"
            >
              <app-icon name="x" />
            </button>
          </div>
        }
      </div>
    }
  `,
})
export class ToastHost {
  protected readonly toastService = inject(ToastService);

  protected iconePorTipo(tipo: string): 'info' | 'alert-triangle' | 'check' {
    if (tipo === 'erro') return 'alert-triangle';
    if (tipo === 'sucesso') return 'check';
    return 'info';
  }
}
