import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { ROTA_POR_PERFIL } from '../constants/rota-inicial';
import { AuthService } from '../services/auth.service';

/**
 * Bloqueia rotas "de visitante" (ex.: /login) para quem já está autenticado.
 * Sem isso, acessar /login direto pela URL (digitando, ou com a sessão ainda
 * válida em outra aba) mostrava o formulário por cima da sessão — sem
 * deslogar de verdade, só escondia a rota atual atrás dele.
 */
export const guestGuard: CanActivateFn = (): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAutenticado()) {
    return true;
  }

  const perfil = authService.getPerfil();
  return router.createUrlTree([perfil ? ROTA_POR_PERFIL[perfil] : '/login']);
};
