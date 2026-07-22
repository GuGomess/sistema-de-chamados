import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';

/**
 * Rotas da aplicação (lazy-loaded via loadComponent).
 * As telas ainda são placeholders — ver wireframes em docs/wireframes.html
 * e o contrato da API em docs/openapi.yaml.
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
  {
    path: 'login',
    title: 'Login — Sistema de Chamados',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'dashboard',
    title: 'Dashboard — Sistema de Chamados',
    canActivate: [authGuard],
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
    redirectTo: 'login',
  },
];
