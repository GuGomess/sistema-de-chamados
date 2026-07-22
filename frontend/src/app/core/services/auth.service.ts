import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, PerfilCodigo, Usuario } from '../models/auth.model';

const AUTH_STORAGE_KEY = 'auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private readonly http: HttpClient) {}

  login(credenciais: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/v1/auth/login`, credenciais)
      .pipe(tap((response) => this.armazenarAuth(response)));
  }

  logout(): void {
    localStorage.removeItem(AUTH_STORAGE_KEY);
  }

  isAutenticado(): boolean {
    return this.getToken() !== null;
  }

  getToken(): string | null {
    return this.getSessao()?.accessToken ?? null;
  }

  getUsuario(): Usuario | null {
    return this.getSessao()?.usuario ?? null;
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
  }
}
