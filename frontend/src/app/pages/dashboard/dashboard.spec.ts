import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, of } from 'rxjs';

import { ResumoSla } from '../../core/models/chamado.model';
import { ChamadoPorStatus, ProdutividadeTecnico } from '../../core/models/metrica.model';
import { ChamadoService } from '../../core/services/chamado.service';
import { MetricaService } from '../../core/services/metrica.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { Dashboard } from './dashboard';

const resumoMock: ResumoSla = { emRisco: 5, vencidos: 3 };

const chamadosPorStatusMock: ChamadoPorStatus[] = [
  { statusId: 1, statusNome: 'Aberto', quantidade: 10 },
  { statusId: 2, statusNome: 'Em andamento', quantidade: 4 },
];

const produtividadeMock: ProdutividadeTecnico[] = [
  {
    tecnicoId: 1,
    tecnicoNome: 'Ana',
    chamadosAtribuidos: 8,
    chamadosResolvidos: 6,
    tempoMedioResolucaoHoras: 3.456,
  },
  {
    tecnicoId: 2,
    tecnicoNome: 'Bruno',
    chamadosAtribuidos: 5,
    chamadosResolvidos: 5,
    tempoMedioResolucaoHoras: null,
  },
];

describe('Dashboard', () => {
  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;
  let router: Router;

  let chamadoServiceMock: { resumoSla: ReturnType<typeof vi.fn> };
  let metricaServiceMock: {
    chamadosPorStatus: ReturnType<typeof vi.fn>;
    produtividadeTecnicos: ReturnType<typeof vi.fn>;
  };
  let realtimeServiceMock: { on: ReturnType<typeof vi.fn> };

  async function criarComponente(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        provideRouter([]),
        { provide: ChamadoService, useValue: chamadoServiceMock },
        { provide: MetricaService, useValue: metricaServiceMock },
        { provide: RealtimeService, useValue: realtimeServiceMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Dashboard);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await fixture.whenStable();
  }

  beforeEach(() => {
    chamadoServiceMock = { resumoSla: vi.fn(() => of(resumoMock)) };
    metricaServiceMock = {
      chamadosPorStatus: vi.fn(() => of(chamadosPorStatusMock)),
      produtividadeTecnicos: vi.fn(() => of(produtividadeMock)),
    };
    realtimeServiceMock = { on: vi.fn() };
  });

  describe('com dados carregados com sucesso', () => {
    beforeEach(async () => {
      await criarComponente();
    });

    it('deve criar o componente', () => {
      expect(component).toBeTruthy();
    });

    it('busca o resumo de SLA e as métricas ao inicializar', () => {
      expect(chamadoServiceMock.resumoSla).toHaveBeenCalledTimes(1);
      expect(metricaServiceMock.chamadosPorStatus).toHaveBeenCalledTimes(1);
      expect(metricaServiceMock.produtividadeTecnicos).toHaveBeenCalledTimes(1);
    });

    it('exibe os cards de SLA com os valores vindos de ChamadoService.resumoSla()', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const valores = compiled.querySelectorAll('.card__valor');

      expect(valores.length).toBe(2);
      expect(valores[0].textContent?.trim()).toBe('3'); // vencidos (card--vencido)
      expect(valores[1].textContent?.trim()).toBe('5'); // emRisco (card--risco)
    });

    it('renderiza o gráfico de "chamados por status" com os dados do MetricaService', () => {
      expect(component['chamadosPorStatus']()).toEqual(chamadosPorStatusMock);

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('canvas')).toBeTruthy();
      expect(compiled.textContent).not.toContain('Nenhum chamado no período.');

      const dadosGrafico = component['statusChartData']();
      expect(dadosGrafico.labels).toEqual(['Aberto', 'Em andamento']);
      expect(dadosGrafico.datasets[0].data).toEqual([10, 4]);
    });

    it('renderiza a tabela de produtividade por técnico com os dados do MetricaService', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const linhas = compiled.querySelectorAll('.tabela-produtividade tbody tr');

      expect(linhas.length).toBe(2);

      expect(linhas[0].textContent).toContain('Ana');
      expect(linhas[0].textContent).toContain('8');
      expect(linhas[0].textContent).toContain('6');
      expect(linhas[0].textContent).toContain('3.5');

      expect(linhas[1].textContent).toContain('Bruno');
      expect(linhas[1].textContent).toContain('—');
    });

    it('verVencidos(): clicar no card de vencidos navega para /chamados com situacaoSla=Vencido', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const botao = compiled.querySelector<HTMLButtonElement>('.card--vencido');

      botao?.click();

      expect(router.navigate).toHaveBeenCalledWith(['/chamados'], {
        queryParams: { situacaoSla: 'Vencido' },
      });
    });

    it('verEmRisco(): clicar no card de em risco navega para /chamados com situacaoSla=EmRisco', () => {
      const compiled = fixture.nativeElement as HTMLElement;
      const botao = compiled.querySelector<HTMLButtonElement>('.card--risco');

      botao?.click();

      expect(router.navigate).toHaveBeenCalledWith(['/chamados'], {
        queryParams: { situacaoSla: 'EmRisco' },
      });
    });
  });

  describe('quando ChamadoService.resumoSla() falha', () => {
    beforeEach(async () => {
      chamadoServiceMock.resumoSla = vi.fn(
        () =>
          new Observable<ResumoSla>((subscriber) => {
            subscriber.error(new Error('erro de rede'));
          }),
      );
      await criarComponente();
    });

    it('exibe mensagem de erro e não mostra os cards de SLA', () => {
      const compiled = fixture.nativeElement as HTMLElement;

      expect(compiled.querySelector('[role="alert"]')?.textContent).toContain(
        'Não foi possível carregar os indicadores de SLA.',
      );
      expect(compiled.querySelector('.cards-sla')).toBeNull();
    });
  });

  describe('quando apenas uma métrica falha', () => {
    it('chamadosPorStatus falha e produtividadeTecnicos tem sucesso: mostra erro só no bloco de status e renderiza a tabela normalmente', async () => {
      metricaServiceMock.chamadosPorStatus = vi.fn(
        () =>
          new Observable<ChamadoPorStatus[]>((subscriber) => {
            subscriber.error(new Error('erro de rede'));
          }),
      );
      await criarComponente();

      const compiled = fixture.nativeElement as HTMLElement;

      expect(compiled.querySelector('[role="alert"]')?.textContent).toContain(
        'Não foi possível carregar os indicadores de chamados por status.',
      );
      expect(compiled.querySelector('canvas')).toBeNull();

      const linhas = compiled.querySelectorAll('.tabela-produtividade tbody tr');
      expect(linhas.length).toBe(2);
      expect(compiled.textContent).not.toContain(
        'Não foi possível carregar a produtividade dos técnicos.',
      );
    });

    it('produtividadeTecnicos falha e chamadosPorStatus tem sucesso: mostra erro só no bloco de produtividade e renderiza o gráfico normalmente', async () => {
      metricaServiceMock.produtividadeTecnicos = vi.fn(
        () =>
          new Observable<ProdutividadeTecnico[]>((subscriber) => {
            subscriber.error(new Error('erro de rede'));
          }),
      );
      await criarComponente();

      const compiled = fixture.nativeElement as HTMLElement;

      expect(compiled.querySelector('[role="alert"]')?.textContent).toContain(
        'Não foi possível carregar a produtividade dos técnicos.',
      );
      expect(compiled.querySelector('.tabela-produtividade')).toBeNull();

      expect(compiled.querySelector('canvas')).toBeTruthy();
      expect(compiled.textContent).not.toContain(
        'Não foi possível carregar os indicadores de chamados por status.',
      );
    });
  });

  describe('quando não há dados de métricas no período', () => {
    beforeEach(async () => {
      metricaServiceMock.chamadosPorStatus = vi.fn(() => of([]));
      metricaServiceMock.produtividadeTecnicos = vi.fn(() => of([]));
      await criarComponente();
    });

    it('mostra as mensagens de "nenhum dado" em vez do gráfico e da tabela', () => {
      const compiled = fixture.nativeElement as HTMLElement;

      expect(compiled.textContent).toContain('Nenhum chamado no período.');
      expect(compiled.textContent).toContain('Nenhum dado de produtividade disponível.');
      expect(compiled.querySelector('canvas')).toBeNull();
      expect(compiled.querySelector('.tabela-produtividade')).toBeNull();
    });
  });
});
