import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ChamadoPorStatus, ProdutividadeTecnico } from '../models/metrica.model';

export interface MetricaFiltros {
  dataInicio?: string;
  dataFim?: string;
}

@Injectable({ providedIn: 'root' })
export class MetricaService {
  constructor(private readonly http: HttpClient) {}

  private paramsDe(filtros?: MetricaFiltros): HttpParams {
    let params = new HttpParams();
    if (filtros?.dataInicio) {
      params = params.set('dataInicio', filtros.dataInicio);
    }
    if (filtros?.dataFim) {
      params = params.set('dataFim', filtros.dataFim);
    }
    return params;
  }

  chamadosPorStatus(filtros?: MetricaFiltros): Observable<ChamadoPorStatus[]> {
    return this.http.get<ChamadoPorStatus[]>(
      `${environment.apiBaseUrl}/v1/metricas/chamados-por-status`,
      {
        params: this.paramsDe(filtros),
      },
    );
  }

  produtividadeTecnicos(filtros?: MetricaFiltros): Observable<ProdutividadeTecnico[]> {
    return this.http.get<ProdutividadeTecnico[]>(
      `${environment.apiBaseUrl}/v1/metricas/produtividade-tecnicos`,
      {
        params: this.paramsDe(filtros),
      },
    );
  }
}
