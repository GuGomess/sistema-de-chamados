import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Categoria,
  Chamado,
  ChamadoCreateRequest,
  ChamadoFiltros,
  ChamadoPage,
  Prioridade,
  Status,
  UsuarioResumo,
} from '../models/chamado.model';

@Injectable({ providedIn: 'root' })
export class ChamadoService {
  constructor(private readonly http: HttpClient) {}

  criar(chamado: ChamadoCreateRequest): Observable<Chamado> {
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados`, chamado);
  }

  listar(filtros: ChamadoFiltros): Observable<ChamadoPage> {
    let params = new HttpParams();
    for (const [chave, valor] of Object.entries(filtros)) {
      if (valor !== null && valor !== undefined && valor !== '') {
        params = params.set(chave, String(valor));
      }
    }

    return this.http.get<ChamadoPage>(`${environment.apiBaseUrl}/v1/chamados`, { params });
  }

  listarCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${environment.apiBaseUrl}/v1/categorias`);
  }

  listarPrioridades(): Observable<Prioridade[]> {
    return this.http.get<Prioridade[]>(`${environment.apiBaseUrl}/v1/prioridades`);
  }

  listarStatus(): Observable<Status[]> {
    return this.http.get<Status[]>(`${environment.apiBaseUrl}/v1/status`);
  }

  listarTecnicos(): Observable<UsuarioResumo[]> {
    return this.http.get<UsuarioResumo[]>(`${environment.apiBaseUrl}/v1/usuarios/tecnicos`);
  }
}
