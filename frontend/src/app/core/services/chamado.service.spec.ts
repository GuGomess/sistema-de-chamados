import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { ChamadoFiltros, ChamadoPage, ResumoSla } from '../models/chamado.model';
import { ChamadoService } from './chamado.service';

const CHAMADOS_URL = `${environment.apiBaseUrl}/v1/chamados`;

describe('ChamadoService', () => {
  let service: ChamadoService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ChamadoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('listar', () => {
    it('monta os query params a partir dos filtros informados', () => {
      const filtros: ChamadoFiltros = {
        page: 2,
        pageSize: 20,
        q: 'impressora',
        idStatus: 3,
        situacaoSla: 'Vencido',
        meus: true,
        ocultarFinalizados: false,
      };

      let respostaRecebida: ChamadoPage | undefined;
      service.listar(filtros).subscribe((resposta) => (respostaRecebida = resposta));

      const req = httpMock.expectOne((r) => r.url === CHAMADOS_URL);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('20');
      expect(req.request.params.get('q')).toBe('impressora');
      expect(req.request.params.get('idStatus')).toBe('3');
      expect(req.request.params.get('situacaoSla')).toBe('Vencido');
      // booleano "false" deve ir para a query (é um valor válido, não vazio)
      expect(req.request.params.get('meus')).toBe('true');
      expect(req.request.params.get('ocultarFinalizados')).toBe('false');

      const paginaMock: ChamadoPage = {
        items: [],
        meta: { page: 2, pageSize: 20, totalItems: 0, totalPages: 0 },
      };
      req.flush(paginaMock);

      expect(respostaRecebida).toEqual(paginaMock);
    });

    it('omite da query os filtros null, undefined ou string vazia', () => {
      const filtros: ChamadoFiltros = {
        page: 1,
        idCategoria: null,
        idTecnico: undefined,
        idDepartamento: null,
        solicitante: '',
        q: '',
      };

      service.listar(filtros).subscribe();

      const req = httpMock.expectOne((r) => r.url === CHAMADOS_URL);
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.has('idCategoria')).toBe(false);
      expect(req.request.params.has('idTecnico')).toBe(false);
      expect(req.request.params.has('idDepartamento')).toBe(false);
      expect(req.request.params.has('solicitante')).toBe(false);
      expect(req.request.params.has('q')).toBe(false);

      req.flush({ items: [], meta: { page: 1, pageSize: 10, totalItems: 0, totalPages: 0 } });
    });
  });

  describe('resumoSla', () => {
    it('faz GET em /v1/chamados/resumo-sla e repassa a resposta como veio da API', () => {
      const resumoMock: ResumoSla = { emRisco: 4, vencidos: 7 };
      let respostaRecebida: ResumoSla | undefined;

      service.resumoSla().subscribe((resposta) => (respostaRecebida = resposta));

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/chamados/resumo-sla`);
      expect(req.request.method).toBe('GET');
      req.flush(resumoMock);

      expect(respostaRecebida).toEqual(resumoMock);
    });
  });
});
