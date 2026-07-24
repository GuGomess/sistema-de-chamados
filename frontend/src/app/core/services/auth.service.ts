import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, PerfilCodigo, Usuario } from '../models/auth.model';

const AUTH_STORAGE_KEY = 'auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Signal (não só leitura pontual do localStorage) porque o componente raiz
  // (App) não é recriado entre login/logout na mesma sessão de SPA — sem
  // isso, trocar de usuário sem recarregar a página deixava o header (nome,
  // perfil, abas) preso na sessão anterior.
  private readonly _usuario = signal<Usuario | null>(this.getSessao()?.usuario ?? null);
  readonly usuario = this._usuario.asReadonly();

  constructor(private readonly http: HttpClient) {}

  login(credenciais: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/v1/auth/login`, credenciais)
      .pipe(tap((response) => this.armazenarAuth(response)));
  }

  logout(): void {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    this._usuario.set(null);
  }

  isAutenticado(): boolean {
    return this.getToken() !== null;
  }

  getToken(): string | null {
    return this.getSessao()?.accessToken ?? null;
  }

  getUsuario(): Usuario | null {
    return this._usuario();
  }

  getPerfil(): PerfilCodigo | null {
    return this.getUsuario()?.perfil ?? null;
  }

  private getSessao(): AuthResponse | null {
    const bruto = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!bruto) {
      return null;
    }

    try {
      return JSON.parse(bruto) as AuthResponse;
    } catch {
      return null;
    }
  }

  private armazenarAuth(auth: AuthResponse): void {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
    this._usuario.set(auth.usuario);
  }
}
