import { Injectable, computed, effect, signal } from '@angular/core';

export type Tema = 'light' | 'dark';

const TEMA_STORAGE_KEY = 'tema';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  // Preferência explícita do usuário (persistida) — null enquanto ele nunca
  // alternou o tema manualmente, caso em que seguimos o SO (prefers-color-scheme).
  private readonly _preferencia = signal<Tema | null>(this.getPreferenciaSalva());

  // Tema do SO, com o listener de matchMedia mantendo-o vivo — assim, se o
  // usuário nunca escolheu um tema manualmente, o app troca junto se ele
  // mudar o tema do Windows/macOS com a aba aberta.
  private readonly _temaSistema = signal<Tema>(this.getTemaSistema());

  readonly tema = computed<Tema>(() => this._preferencia() ?? this._temaSistema());
  readonly ehEscuro = computed(() => this.tema() === 'dark');

  constructor() {
    // Efeito (não setter direto no construtor): precisa reagir tanto à troca
    // manual de preferência quanto à mudança ao vivo do tema do SO.
    effect(() => {
      document.documentElement.setAttribute('data-theme', this.tema());
    });

    if (typeof window !== 'undefined' && window.matchMedia) {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (evento) => {
        this._temaSistema.set(evento.matches ? 'dark' : 'light');
      });
    }
  }

  alternar(): void {
    const novoTema: Tema = this.tema() === 'dark' ? 'light' : 'dark';
    this._preferencia.set(novoTema);
    localStorage.setItem(TEMA_STORAGE_KEY, novoTema);
  }

  private getPreferenciaSalva(): Tema | null {
    const salvo = localStorage.getItem(TEMA_STORAGE_KEY);
    return salvo === 'light' || salvo === 'dark' ? salvo : null;
  }

  private getTemaSistema(): Tema {
    if (typeof window === 'undefined' || !window.matchMedia) {
      return 'light';
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
