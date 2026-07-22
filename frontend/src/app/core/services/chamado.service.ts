import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Categoria, Chamado, ChamadoCreateRequest, Prioridade } from '../models/chamado.model';

@Injectable({ providedIn: 'root' })
export class ChamadoService {
  constructor(private readonly http: HttpClient) {}

  criar(chamado: ChamadoCreateRequest): Observable<Chamado> {
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados`, chamado);
  }

  listarCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${environment.apiBaseUrl}/v1/categorias`);
  }

  listarPrioridades(): Observable<Prioridade[]> {
    return this.http.get<Prioridade[]>(`${environment.apiBaseUrl}/v1/prioridades`);
  }
}
