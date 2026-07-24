import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, of } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Anexo,
  AtribuirTecnicoRequest,
  Avaliacao,
  AvaliacaoCreateRequest,
  AvaliacaoUpdateRequest,
  Categoria,
  Chamado,
  ChamadoCreateRequest,
  ChamadoFiltros,
  ChamadoPage,
  ChamadoUpdateRequest,
  Comentario,
  ComentarioCreateRequest,
  FecharClienteRequest,
  Historico,
  PrazoResolucaoUpdateRequest,
  PrazoRespostaUpdateRequest,
  Prioridade,
  ResumoSla,
  Status,
  UsuarioResumo,
} from '../models/chamado.model';

@Injectable({ providedIn: 'root' })
export class ChamadoService {
  constructor(private readonly http: HttpClient) {}

  criar(chamado: ChamadoCreateRequest): Observable<Chamado> {
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados`, chamado);
  }

  detalhar(id: number): Observable<Chamado> {
    return this.http.get<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}`);
  }

  atualizar(id: number, request: ChamadoUpdateRequest): Observable<Chamado> {
    return this.http.patch<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}`, request);
  }

  atribuir(id: number, idTecnico: number): Observable<Chamado> {
    const request: AtribuirTecnicoRequest = { idTecnico };
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}/atribuir`, request);
  }

  assumir(id: number): Observable<Chamado> {
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}/assumir`, {});
  }

  reabrir(id: number): Observable<Chamado> {
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}/reabrir`, {});
  }

  fecharComoCliente(id: number, motivo?: string): Observable<Chamado> {
    const request: FecharClienteRequest = { motivo };
    return this.http.post<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}/fechar-cliente`, request);
  }

  ajustarPrazoResolucao(id: number, request: PrazoResolucaoUpdateRequest): Observable<Chamado> {
    return this.http.patch<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}/prazo-resolucao`, request);
  }

  ajustarPrazoResposta(id: number, request: PrazoRespostaUpdateRequest): Observable<Chamado> {
    return this.http.patch<Chamado>(`${environment.apiBaseUrl}/v1/chamados/${id}/prazo-resposta`, request);
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

  listarAtribuiveis(): Observable<UsuarioResumo[]> {
    return this.http.get<UsuarioResumo[]>(`${environment.apiBaseUrl}/v1/usuarios/atribuiveis`);
  }

  resumoSla(): Observable<ResumoSla> {
    return this.http.get<ResumoSla>(`${environment.apiBaseUrl}/v1/chamados/resumo-sla`);
  }

  listarComentarios(id: number): Observable<Comentario[]> {
    return this.http.get<Comentario[]>(`${environment.apiBaseUrl}/v1/chamados/${id}/comentarios`);
  }

  criarComentario(id: number, request: ComentarioCreateRequest): Observable<Comentario> {
    const formData = new FormData();
    formData.append('mensagem', request.mensagem);
    formData.append('interno', String(request.interno));
    for (const arquivo of request.arquivos ?? []) {
      formData.append('arquivos', arquivo);
    }
    return this.http.post<Comentario>(`${environment.apiBaseUrl}/v1/chamados/${id}/comentarios`, formData);
  }

  listarAnexos(id: number): Observable<Anexo[]> {
    return this.http.get<Anexo[]>(`${environment.apiBaseUrl}/v1/chamados/${id}/anexos`);
  }

  enviarAnexo(id: number, arquivo: File): Observable<Anexo> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    return this.http.post<Anexo>(`${environment.apiBaseUrl}/v1/chamados/${id}/anexos`, formData);
  }

  baixarAnexo(id: number, anexoId: number): Observable<Blob> {
    return this.http.get(`${environment.apiBaseUrl}/v1/chamados/${id}/anexos/${anexoId}/download`, {
      responseType: 'blob',
    });
  }

  listarAvaliacoes(id: number): Observable<Avaliacao[]> {
    return this.http
      .get<Avaliacao[]>(`${environment.apiBaseUrl}/v1/chamados/${id}/avaliacoes`)
      .pipe(catchError(() => of([])));
  }

  enviarAvaliacao(id: number, request: AvaliacaoCreateRequest): Observable<Avaliacao> {
    return this.http.post<Avaliacao>(`${environment.apiBaseUrl}/v1/chamados/${id}/avaliacao`, request);
  }

  atualizarAvaliacao(id: number, avaliacaoId: number, request: AvaliacaoUpdateRequest): Observable<Avaliacao> {
    return this.http.patch<Avaliacao>(`${environment.apiBaseUrl}/v1/chamados/${id}/avaliacoes/${avaliacaoId}`, request);
  }

  ocultarAvaliacao(id: number, avaliacaoId: number, oculta: boolean): Observable<Avaliacao> {
    return this.http.patch<Avaliacao>(`${environment.apiBaseUrl}/v1/chamados/${id}/avaliacoes/${avaliacaoId}/ocultar`, { oculta });
  }

  listarHistorico(id: number): Observable<Historico[]> {
    return this.http.get<Historico[]>(`${environment.apiBaseUrl}/v1/chamados/${id}/historico`);
  }
}
