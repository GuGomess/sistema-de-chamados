import { inject } from '@angular/core';
import { Routes } from '@angular/router';

import { ROTA_POR_PERFIL } from './core/constants/rota-inicial';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { AuthService } from './core/services/auth.service';

/**
 * Rota "atual" pro estado de sessão: home do perfil se autenticado, /login
 * caso contrário. Usada nos redirects incondicionais (rota vazia e coringa),
 * que antes mandavam pra /login mesmo já autenticado — sem deslogar de
 * verdade, só escondiam a rota atual atrás do formulário de login.
 */
function rotaAtual(): string {
  const perfil = inject(AuthService).getPerfil();
  return perfil ? ROTA_POR_PERFIL[perfil] : '/login';
}

/**
 * Rotas da aplicação (lazy-loaded via loadComponent).
 * As telas ainda são placeholders — ver wireframes em docs/wireframes.html
 * e o contrato da API em docs/openapi.yaml.
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: rotaAtual,
  },
  {
    path: 'login',
    title: 'Login — Sistema de Chamados',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'dashboard',
    title: 'Dashboard — Sistema de Chamados',
    canActivate: [authGuard],
    // Mostra contadores de SLA (vencidos/em risco) — conceito de triagem
    // interna que o Cliente não deve ver (ver core/models/auth.model.ts).
    data: { roles: ['ADMINISTRADOR', 'TECNICO'] },
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'chamados',
    title: 'Chamados — Sistema de Chamados',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/chamados/chamados-lista/chamados-lista').then((m) => m.ChamadosLista),
  },
  {
    path: 'chamados/novo',
    title: 'Novo chamado — Sistema de Chamados',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/chamados/chamado-novo/chamado-novo').then((m) => m.ChamadoNovo),
  },
  {
    path: 'chamados/:id',
    title: 'Detalhe do chamado — Sistema de Chamados',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/chamados/chamado-detalhe/chamado-detalhe').then((m) => m.ChamadoDetalhe),
  },
  {
    path: '**',
    redirectTo: rotaAtual,
  },
];
