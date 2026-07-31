import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { ChamadoPorStatus, ProdutividadeTecnico } from '../models/metrica.model';
import { MetricaService } from './metrica.service';

const CHAMADOS_POR_STATUS_URL = `${environment.apiBaseUrl}/v1/metricas/chamados-por-status`;
const PRODUTIVIDADE_URL = `${environment.apiBaseUrl}/v1/metricas/produtividade-tecnicos`;

describe('MetricaService', () => {
  let service: MetricaService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(MetricaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('chamadosPorStatus', () => {
    it('faz GET na URL correta, sem filtros, e repassa a resposta como veio da API', () => {
      const mock: ChamadoPorStatus[] = [
        { statusId: 1, statusNome: 'Aberto', quantidade: 10 },
        { statusId: 2, statusNome: 'Em andamento', quantidade: 4 },
      ];
      let respostaRecebida: ChamadoPorStatus[] | undefined;

      service.chamadosPorStatus().subscribe((resposta) => (respostaRecebida = resposta));

      const req = httpMock.expectOne((r) => r.url === CHAMADOS_POR_STATUS_URL);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys().length).toBe(0);
      req.flush(mock);

      expect(respostaRecebida).toEqual(mock);
    });

    it('inclui dataInicio/dataFim na query quando informados nos filtros', () => {
      service.chamadosPorStatus({ dataInicio: '2026-01-01', dataFim: '2026-01-31' }).subscribe();

      const req = httpMock.expectOne((r) => r.url === CHAMADOS_POR_STATUS_URL);
      expect(req.request.params.get('dataInicio')).toBe('2026-01-01');
      expect(req.request.params.get('dataFim')).toBe('2026-01-31');

      req.flush([]);
    });
  });

  describe('produtividadeTecnicos', () => {
    it('faz GET na URL correta e repassa a resposta como veio da API', () => {
      const mock: ProdutividadeTecnico[] = [
        {
          tecnicoId: 1,
          tecnicoNome: 'Ana',
          chamadosAtribuidos: 8,
          chamadosResolvidos: 6,
          tempoMedioResolucaoHoras: 3.5,
        },
        {
          tecnicoId: 2,
          tecnicoNome: 'Bruno',
          chamadosAtribuidos: 5,
          chamadosResolvidos: 0,
          tempoMedioResolucaoHoras: null,
        },
      ];
      let respostaRecebida: ProdutividadeTecnico[] | undefined;

      service.produtividadeTecnicos().subscribe((resposta) => (respostaRecebida = resposta));

      const req = httpMock.expectOne((r) => r.url === PRODUTIVIDADE_URL);
      expect(req.request.method).toBe('GET');
      req.flush(mock);

      expect(respostaRecebida).toEqual(mock);
    });
  });
});
