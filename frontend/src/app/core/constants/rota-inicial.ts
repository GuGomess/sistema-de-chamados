import { PerfilCodigo } from '../models/auth.model';

/**
 * Rota para onde cada perfil vai por padrão: logo após o login, ao clicar na
 * marca do header já autenticado, ou ao tentar acessar /login com uma sessão
 * ainda válida. Fonte única — usada em login.ts, app.routes.ts e guest.guard.ts.
 */
export const ROTA_POR_PERFIL: Record<PerfilCodigo, string> = {
  CLIENTE: '/chamados',
  TECNICO: '/dashboard',
  ADMINISTRADOR: '/dashboard',
};
