import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { PerfilCodigo } from '../models/auth.model';
import { AuthService } from '../services/auth.service';
import { guestGuard } from './guest.guard';

// guestGuard ignora seus argumentos, mas o tipo CanActivateFn exige que a
// chamada informe route/state — valores vazios bastam.
const routeMock = {} as ActivatedRouteSnapshot;
const stateMock = { url: '/login' } as RouterStateSnapshot;

describe('guestGuard', () => {
  let authServiceMock: { isAutenticado: () => boolean; getPerfil: () => PerfilCodigo | null };
  let router: Router;

  beforeEach(() => {
    authServiceMock = {
      isAutenticado: () => false,
      getPerfil: () => null,
    };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceMock }],
    });

    router = TestBed.inject(Router);
  });

  function executarGuard() {
    return TestBed.runInInjectionContext(() => guestGuard(routeMock, stateMock));
  }

  it('permite o acesso (retorna true) quando o usuário não está autenticado', () => {
    authServiceMock.isAutenticado = () => false;

    const resultado = executarGuard();

    expect(resultado).toBe(true);
  });

  it('redireciona técnico já autenticado para /dashboard (rota inicial do papel dele)', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => 'TECNICO';

    const resultado = executarGuard();

    const esperado = router.createUrlTree(['/dashboard']);
    expect(resultado).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });

  it('redireciona administrador já autenticado para /dashboard', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => 'ADMINISTRADOR';

    const resultado = executarGuard();

    const esperado = router.createUrlTree(['/dashboard']);
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });

  it('redireciona cliente já autenticado para /chamados (rota inicial do papel dele)', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => 'CLIENTE';

    const resultado = executarGuard();

    const esperado = router.createUrlTree(['/chamados']);
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });

  it('redireciona para /login se autenticado mas sem perfil resolvido (estado inconsistente)', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => null;

    const resultado = executarGuard();

    const esperado = router.createUrlTree(['/login']);
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });
});
