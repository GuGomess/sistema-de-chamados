import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Departamento, DepartamentoCreateRequest, DepartamentoUpdateRequest } from '../models/departamento.model';

@Injectable({ providedIn: 'root' })
export class DepartamentoService {
  constructor(private readonly http: HttpClient) {}

  listar(apenasAtivos?: boolean): Observable<Departamento[]> {
    return this.http.get<Departamento[]>(`${environment.apiBaseUrl}/v1/departamentos`, {
      params: apenasAtivos !== undefined ? { apenasAtivos } : {},
    });
  }

  criar(request: DepartamentoCreateRequest): Observable<Departamento> {
    return this.http.post<Departamento>(`${environment.apiBaseUrl}/v1/departamentos`, request);
  }

  editar(id: number, request: DepartamentoUpdateRequest): Observable<Departamento> {
    return this.http.patch<Departamento>(`${environment.apiBaseUrl}/v1/departamentos/${id}`, request);
  }

  definirStatus(id: number, ativo: boolean): Observable<Departamento> {
    return this.http.patch<Departamento>(`${environment.apiBaseUrl}/v1/departamentos/${id}/status`, { ativo });
  }
}
