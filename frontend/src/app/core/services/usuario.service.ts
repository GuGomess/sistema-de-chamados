import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Usuario, UsuarioCreateRequest } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  constructor(private readonly http: HttpClient) {}

  criar(request: UsuarioCreateRequest): Observable<Usuario> {
    return this.http.post<Usuario>(`${environment.apiBaseUrl}/v1/usuarios`, request);
  }

  listar(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(`${environment.apiBaseUrl}/v1/usuarios`);
  }

  atualizarDepartamentos(idUsuario: number, idsDepartamentos: number[]): Observable<Usuario> {
    return this.http.put<Usuario>(`${environment.apiBaseUrl}/v1/usuarios/${idUsuario}/departamentos`, {
      idsDepartamentos,
    });
  }

  alterarAtivo(idUsuario: number, ativo: boolean): Observable<Usuario> {
    return this.http.patch<Usuario>(`${environment.apiBaseUrl}/v1/usuarios/${idUsuario}/ativo`, { ativo });
  }

  alterarSenha(idUsuario: number, novaSenha: string): Observable<Usuario> {
    return this.http.patch<Usuario>(`${environment.apiBaseUrl}/v1/usuarios/${idUsuario}/senha`, { novaSenha });
  }

  atualizarMeuPerfil(request: { nome: string; email: string }): Observable<Usuario> {
    return this.http.patch<Usuario>(`${environment.apiBaseUrl}/v1/usuarios/me`, request);
  }

  alterarMinhaSenha(request: { senhaAtual: string; novaSenha: string }): Observable<void> {
    return this.http.patch<void>(`${environment.apiBaseUrl}/v1/usuarios/me/senha`, request);
  }
}
