import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest } from '../models/auth.model';

const AUTH_STORAGE_KEY = 'auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private readonly http: HttpClient) {}

  login(credenciais: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/v1/auth/login`, credenciais)
      .pipe(tap((response) => this.armazenarAuth(response)));
  }

  private armazenarAuth(auth: AuthResponse): void {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
  }
}
