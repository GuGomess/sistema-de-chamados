import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, Usuario } from '../models/auth.model';
import { AuthService } from './auth.service';

const AUTH_STORAGE_KEY = 'auth';
const LOGIN_URL = `${environment.apiBaseUrl}/v1/auth/login`;

const usuarioMock: Usuario = {
  id: 1,
  nome: 'Fulano de Tal',
  email: 'fulano@teste.com',
  perfil: 'TECNICO',
  ativo: true,
  criadoEm: '2026-01-01T00:00:00Z',
  departamentos: [],
};

const authResponseMock: AuthResponse = {
  accessToken: 'token-abc123',
  refreshToken: 'refresh-abc123',
  expiresIn: 3600,
  usuario: usuarioMock,
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('login', () => {
    it('com sucesso: chama a URL correta e armazena token e perfil do usuário retornado', () => {
      const credenciais: LoginRequest = { email: usuarioMock.email, senha: 'senha123' };
      let respostaRecebida: AuthResponse | undefined;

      service.login(credenciais).subscribe((resposta) => (respostaRecebida = resposta));

      const req = httpMock.expectOne(LOGIN_URL);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(credenciais);
      req.flush(authResponseMock);

      expect(respostaRecebida).toEqual(authResponseMock);
      expect(service.isAutenticado()).toBe(true);
      expect(service.getToken()).toBe('token-abc123');
      expect(service.getPerfil()).toBe('TECNICO');
      expect(service.getUsuario()).toEqual(usuarioMock);
      expect(JSON.parse(localStorage.getItem(AUTH_STORAGE_KEY)!)).toEqual(authResponseMock);
    });

    it('com falha: propaga o erro e não armazena sessão nenhuma', () => {
      const credenciais: LoginRequest = { email: 'errado@teste.com', senha: 'senha-errada' };
      let sucesso = false;
      let erroCapturado: unknown;

      service.login(credenciais).subscribe({
        next: () => (sucesso = true),
        error: (erro) => (erroCapturado = erro),
      });

      const req = httpMock.expectOne(LOGIN_URL);
      req.flush({ mensagem: 'Credenciais inválidas' }, { status: 401, statusText: 'Unauthorized' });

      expect(sucesso).toBe(false);
      expect(erroCapturado).toBeTruthy();
      expect(service.isAutenticado()).toBe(false);
      expect(service.getToken()).toBeNull();
      expect(service.getUsuario()).toBeNull();
      expect(localStorage.getItem(AUTH_STORAGE_KEY)).toBeNull();
    });
  });

  describe('logout', () => {
    it('limpa a sessão do localStorage e o signal de usuário', () => {
      service.login({ email: usuarioMock.email, senha: 'senha123' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(authResponseMock);
      expect(service.isAutenticado()).toBe(true);

      service.logout();

      expect(service.isAutenticado()).toBe(false);
      expect(service.getUsuario()).toBeNull();
      expect(service.getPerfil()).toBeNull();
      expect(localStorage.getItem(AUTH_STORAGE_KEY)).toBeNull();
    });
  });

  describe('extração de papel a partir do estado salvo', () => {
    it('uma nova instância lê o perfil da sessão já salva no localStorage (simula reload de página)', () => {
      localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(authResponseMock));

      // O signal interno de usuário é inicializado no construtor a partir do
      // localStorage — criar uma nova instância aqui simula o app sendo
      // recarregado com uma sessão previamente salva.
      const http = TestBed.inject(HttpClient);
      const novaInstancia = new AuthService(http);

      expect(novaInstancia.isAutenticado()).toBe(true);
      expect(novaInstancia.getToken()).toBe('token-abc123');
      expect(novaInstancia.getPerfil()).toBe('TECNICO');
      expect(novaInstancia.getUsuario()).toEqual(usuarioMock);
    });

    it('sem sessão salva, uma nova instância não está autenticada e não tem perfil', () => {
      const http = TestBed.inject(HttpClient);
      const novaInstancia = new AuthService(http);

      expect(novaInstancia.isAutenticado()).toBe(false);
      expect(novaInstancia.getPerfil()).toBeNull();
      expect(novaInstancia.getUsuario()).toBeNull();
    });

    it('ignora um estado salvo corrompido (JSON inválido) em vez de lançar erro', () => {
      localStorage.setItem(AUTH_STORAGE_KEY, '{json-invalido');

      const http = TestBed.inject(HttpClient);
      const novaInstancia = new AuthService(http);

      expect(novaInstancia.isAutenticado()).toBe(false);
      expect(novaInstancia.getPerfil()).toBeNull();
    });
  });
});
