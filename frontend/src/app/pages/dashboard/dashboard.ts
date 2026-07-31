import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { ResumoSla } from '../../core/models/chamado.model';
import { ChamadoPorStatus, ProdutividadeTecnico } from '../../core/models/metrica.model';
import { ChamadoService } from '../../core/services/chamado.service';
import { MetricaService } from '../../core/services/metrica.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { Icon } from '../../shared/icon/icon';

@Component({
  selector: 'app-dashboard',
  imports: [BaseChartDirective, Icon],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly chamadoService = inject(ChamadoService);
  private readonly metricaService = inject(MetricaService);
  private readonly realtimeService = inject(RealtimeService);
  private readonly router = inject(Router);

  protected readonly resumo = signal<ResumoSla | null>(null);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);

  protected readonly chamadosPorStatus = signal<ChamadoPorStatus[]>([]);
  protected readonly produtividadeTecnicos = signal<ProdutividadeTecnico[]>([]);
  protected readonly carregandoChamadosPorStatus = signal(true);
  protected readonly erroChamadosPorStatus = signal<string | null>(null);
  protected readonly carregandoProdutividade = signal(true);
  protected readonly erroProdutividade = signal<string | null>(null);

  protected readonly statusChartType = 'doughnut' as const;

  // Paleta categórica alinhada ao design system: acento + semânticas do
  // styles.scss, lidas em tempo de execução (com fallback para o hex atual
  // dos tokens) para acompanhar automaticamente o tema claro/escuro vigente
  // no momento em que o gráfico é montado.
  protected readonly statusChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '68%',
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          color: this.corTema('--text-secondary', '#565d78'),
          usePointStyle: true,
          pointStyle: 'circle',
          boxWidth: 8,
          boxHeight: 8,
          padding: 16,
          font: { family: 'Plus Jakarta Sans Variable, sans-serif', size: 12, weight: 600 },
        },
      },
      tooltip: {
        backgroundColor: this.corTema('--bg-surface', '#ffffff'),
        titleColor: this.corTema('--text-primary', '#161a2b'),
        bodyColor: this.corTema('--text-secondary', '#565d78'),
        borderColor: this.corTema('--border-subtle', '#e4e7f0'),
        borderWidth: 1,
        padding: 10,
        cornerRadius: 8,
        displayColors: true,
        boxPadding: 4,
      },
    },
  };

  protected readonly statusChartData = computed<ChartData<'doughnut'>>(() => {
    const dados = this.chamadosPorStatus();
    const paleta = this.paletaCategorica();
    const contorno = this.corTema('--bg-surface', '#ffffff');

    return {
      labels: dados.map((item) => item.statusNome),
      datasets: [
        {
          data: dados.map((item) => item.quantidade),
          backgroundColor: dados.map((_, i) => paleta[i % paleta.length]),
          borderColor: contorno,
          borderWidth: 2,
          hoverOffset: 8,
        },
      ],
    };
  });

  ngOnInit(): void {
    this.carregarResumo();
    this.carregarMetricas();
    this.realtimeService.on('ChamadoAtualizado', () => {
      this.carregarResumo();
      this.carregarMetricas();
    });
  }

  private carregarResumo(): void {
    this.chamadoService.resumoSla().subscribe({
      next: (resumo) => {
        this.resumo.set(resumo);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Não foi possível carregar os indicadores de SLA.');
        this.carregando.set(false);
      },
    });
  }

  private carregarMetricas(): void {
    this.carregandoChamadosPorStatus.set(true);
    this.erroChamadosPorStatus.set(null);
    this.carregandoProdutividade.set(true);
    this.erroProdutividade.set(null);

    this.metricaService.chamadosPorStatus().subscribe({
      next: (dados) => {
        this.chamadosPorStatus.set(dados);
        this.carregandoChamadosPorStatus.set(false);
      },
      error: () => {
        this.erroChamadosPorStatus.set(
          'Não foi possível carregar os indicadores de chamados por status.',
        );
        this.carregandoChamadosPorStatus.set(false);
      },
    });

    this.metricaService.produtividadeTecnicos().subscribe({
      next: (dados) => {
        this.produtividadeTecnicos.set(dados);
        this.carregandoProdutividade.set(false);
      },
      error: () => {
        this.erroProdutividade.set('Não foi possível carregar a produtividade dos técnicos.');
        this.carregandoProdutividade.set(false);
      },
    });
  }

  protected verVencidos(): void {
    this.router.navigate(['/chamados'], { queryParams: { situacaoSla: 'Vencido' } });
  }

  protected verEmRisco(): void {
    this.router.navigate(['/chamados'], { queryParams: { situacaoSla: 'EmRisco' } });
  }

  protected formatarHoras(horas: number | null): string {
    return horas === null ? '—' : horas.toFixed(1);
  }

  // Lê uma CSS custom property do sistema de design em tempo de execução
  // (acompanha o tema claro/escuro aplicado no momento), com fallback para o
  // valor padrão do token — necessário em ambientes sem `document` (SSR) ou
  // quando a folha global ainda não foi computada (ex.: testes).
  private corTema(variavel: string, fallback: string): string {
    if (typeof document === 'undefined') {
      return fallback;
    }
    const valor = getComputedStyle(document.documentElement).getPropertyValue(variavel).trim();
    return valor || fallback;
  }

  // Ordem fixa de cores categóricas para identidade de status — nunca por
  // hex "cru": índigo (acento do produto) seguido das semânticas do design
  // system, evitando as cores padrão do Chart.js.
  private paletaCategorica(): string[] {
    return [
      this.corTema('--accent-500', '#6366f1'),
      this.corTema('--info', '#0369a1'),
      this.corTema('--warning', '#b45309'),
      this.corTema('--success', '#15803d'),
      this.corTema('--danger', '#dc2626'),
      this.corTema('--accent-300', '#a5b4fc'),
    ];
  }
}
