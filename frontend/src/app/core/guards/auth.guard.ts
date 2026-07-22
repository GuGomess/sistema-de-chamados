import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { PerfilCodigo } from '../models/auth.model';
import { AuthService } from '../services/auth.service';

/**
 * Exige autenticação para acessar a rota. Se a rota declarar `data: { roles: [...] }`,
 * também exige que o perfil do usuário logado esteja nessa lista.
 */
export const authGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAutenticado()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  const rolesPermitidos = route.data['roles'] as PerfilCodigo[] | undefined;
  const perfil = authService.getPerfil();

  if (rolesPermitidos && (!perfil || !rolesPermitidos.includes(perfil))) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
