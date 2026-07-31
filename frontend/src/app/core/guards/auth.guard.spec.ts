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
import { authGuard } from './auth.guard';

function criarRoute(roles?: PerfilCodigo[]): ActivatedRouteSnapshot {
  return { data: roles ? { roles } : {} } as unknown as ActivatedRouteSnapshot;
}

function criarState(url: string): RouterStateSnapshot {
  return { url } as unknown as RouterStateSnapshot;
}

describe('authGuard', () => {
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

  function executarGuard(roles: PerfilCodigo[] | undefined, url: string) {
    return TestBed.runInInjectionContext(() => authGuard(criarRoute(roles), criarState(url)));
  }

  it('redireciona para /login com returnUrl quando o usuário não está autenticado', () => {
    authServiceMock.isAutenticado = () => false;

    const resultado = executarGuard(undefined, '/dashboard');

    const esperado = router.createUrlTree(['/login'], { queryParams: { returnUrl: '/dashboard' } });
    expect(resultado).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });

  it('preserva a URL de destino original no returnUrl', () => {
    authServiceMock.isAutenticado = () => false;

    const resultado = executarGuard(undefined, '/chamados/42');

    const esperado = router.createUrlTree(['/login'], {
      queryParams: { returnUrl: '/chamados/42' },
    });
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });

  it('redireciona para /chamados quando o usuário autenticado não tem o papel exigido pela rota', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => 'CLIENTE';

    const resultado = executarGuard(['ADMINISTRADOR', 'TECNICO'], '/dashboard');

    const esperado = router.createUrlTree(['/chamados']);
    expect(resultado).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(resultado as UrlTree)).toBe(router.serializeUrl(esperado));
  });

  it('permite o acesso (retorna true) quando o usuário autenticado tem o papel exigido', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => 'TECNICO';

    const resultado = executarGuard(['ADMINISTRADOR', 'TECNICO'], '/dashboard');

    expect(resultado).toBe(true);
  });

  it('permite o acesso quando o usuário está autenticado e a rota não exige papel específico', () => {
    authServiceMock.isAutenticado = () => true;
    authServiceMock.getPerfil = () => 'CLIENTE';

    const resultado = executarGuard(undefined, '/chamados');

    expect(resultado).toBe(true);
  });
});
